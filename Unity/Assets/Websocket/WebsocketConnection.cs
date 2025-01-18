﻿#nullable enable

using System;
using System.Text;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using NativeWebSocket;
using NeuroSdk.Messages.API;
using NeuroSdk.Utilities;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace NeuroSdk.Websocket
{
    [PublicAPI]
    public sealed class WebsocketConnection : MonoBehaviour
    {
        private const float RECONNECT_INTERVAL = 3;

        private static WebsocketConnection? _instance;
        public static WebsocketConnection? Instance
        {
            get
            {
                if (!_instance) Debug.LogWarning("Accessed WebsocketConnection.Instance without an instance being present");
                return _instance;
            }
            private set => _instance = value;
        }

        private static WebSocket? _socket;

        public string game = null!;
        public MessageQueue messageQueue = null!;
        public CommandHandler commandHandler = null!;

        public UnityEvent? onConnected;
        public UnityEvent<string>? onError;
        public UnityEvent<WebSocketCloseCode>? onDisconnected;

        private void Awake()
        {
            if (Instance)
            {
                Debug.Log("Destroying duplicate WebsocketConnection instance");
                Destroy(this);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        private void Start() => StartWs().Forget();

        private async UniTask Reconnect()
        {
            await UniTask.SwitchToMainThread();
            await UniTask.Delay(TimeSpan.FromSeconds(RECONNECT_INTERVAL));
            await StartWs();
        }

        private async UniTask StartWs()
        {
            try
            {
                if (_socket?.State is WebSocketState.Open or WebSocketState.Connecting) await _socket.Close();
            }
            catch
            {
                // ignored
            }

            string? websocketUrl = null;

            if (Application.absoluteURL.IndexOf("?", StringComparison.Ordinal) != -1)
            {
                string[] urlSplits = Application.absoluteURL.Split('?');
                if (urlSplits.Length > 1)
                {
                    string[] urlParamSplits = urlSplits[1].Split(new[] { "WebSocketURL=" }, StringSplitOptions.None);
                    if (urlParamSplits.Length > 1)
                    {
                        string? param = urlParamSplits[1].Split('&')[0];
                        if (!string.IsNullOrEmpty(param))
                        {
                            websocketUrl = param;
                        }
                    }
                }
            }

            if (websocketUrl is null or "")
            {
                try
                {
                    Uri uri = new(Application.absoluteURL);
                    string requestUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}/$env/NEURO_SDK_WS_URL";
                    UnityWebRequest request = UnityWebRequest.Get(requestUrl);

                    await request.SendWebRequest();
                    if (TryGetResult(request, out string result))
                    {
                        websocketUrl = result;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            if (websocketUrl is null or "")
            {
                websocketUrl = Environment.GetEnvironmentVariable("NEURO_SDK_WS_URL", EnvironmentVariableTarget.Process) ??
                               Environment.GetEnvironmentVariable("NEURO_SDK_WS_URL", EnvironmentVariableTarget.User) ??
                               Environment.GetEnvironmentVariable("NEURO_SDK_WS_URL", EnvironmentVariableTarget.Machine);
            }

            if (websocketUrl is null or "")
            {
                string errMessage = "Could not retrieve websocket URL.";
#if UNITY_EDITOR || !UNITY_WEBGL
                errMessage += " You should set the NEURO_SDK_WS_URL environment variable.";
#endif
#if UNITY_WEBGL
                errMessage += " You need to specify a WebSocketURL query parameter in the URL or open a local server that serves the NEURO_SDK_WS_URL environment variable. See the documentation for more information.";
#endif
                Debug.LogError(errMessage);
                return;
            }

            // Websocket callbacks get run on separate threads! Watch out
            _socket = new WebSocket(websocketUrl);
            _socket.OnOpen += () => onConnected?.Invoke();
            _socket.OnMessage += bytes =>
            {
                string message = Encoding.UTF8.GetString(bytes);
                ReceiveMessage(message).Forget();
            };
            _socket.OnError += error =>
            {
                onError?.Invoke(error);
                if (error != "Unable to connect to the remote server")
                {
                    Debug.LogError("Websocket connection has encountered an error!");
                    Debug.LogError(error);
                }
            };
            _socket.OnClose += code =>
            {
                onDisconnected?.Invoke(code);
                if (code != WebSocketCloseCode.Abnormal) Debug.LogWarning($"Websocket connection has been closed with code {code}!");
                Reconnect().Forget();
            };
            await _socket.Connect();
        }

        private void Update()
        {
            if (_socket?.State is not WebSocketState.Open) return;

            while (messageQueue.Count > 0)
            {
                OutgoingMessageBuilder builder = messageQueue.Dequeue()!;
                SendTask(builder).Forget();
            }

#if !UNITY_WEBGL || UNITY_EDITOR
            _socket.DispatchMessageQueue();
#endif
        }

        private async UniTask SendTask(OutgoingMessageBuilder builder)
        {
            string message = Jason.Serialize(builder.GetWsMessage());

            Debug.Log($"Sending ws message {message}");

            try
            {
                await _socket!.SendText(message);
            }
            catch
            {
                Debug.LogError($"Failed to send ws message {message}");
                messageQueue.Enqueue(builder);
            }
        }

        public void Send(OutgoingMessageBuilder messageBuilder) => messageQueue.Enqueue(messageBuilder);

        public void SendImmediate(OutgoingMessageBuilder messageBuilder)
        {
            string message = Jason.Serialize(messageBuilder.GetWsMessage());

            if (_socket?.State is not WebSocketState.Open)
            {
                Debug.LogError($"WS not open - failed to send immediate ws message {message}");
                return;
            }

            Debug.Log($"Sending immediate ws message {message}");

            _socket.SendText(message);
        }

        [Obsolete("Use WebsocketConnection.Instance.Send instead")]
        public static void TrySend(OutgoingMessageBuilder messageBuilder)
        {
            if (Instance == null)
            {
                Debug.LogError("Cannot send message - WebsocketConnection instance is null");
                return;
            }

            Instance.Send(messageBuilder);
        }

        [Obsolete("Use WebsocketConnection.Instance.SendImmediate instead")]
        public static void TrySendImmediate(OutgoingMessageBuilder messageBuilder)
        {
            if (Instance == null)
            {
                Debug.LogError("Cannot send immediate message - WebsocketConnection instance is null");
                return;
            }

            Instance.SendImmediate(messageBuilder);
        }

        private async UniTask ReceiveMessage(string msgData)
        {
            try
            {
                await UniTask.SwitchToMainThread();

                Debug.Log("Received ws message " + msgData);

                JObject message = JObject.Parse(msgData);
                string? command = message["command"]?.Value<string>();
                MessageJData data = new(message["data"]);

                if (command == null)
                {
                    Debug.LogError("Received command that could not be deserialized. What the fuck are you doing?");
                    return;
                }

                commandHandler.Handle(command, data);
            }
            catch (Exception e)
            {
                Debug.LogError("Received invalid message");
                Debug.LogError(e);
            }
        }

        private bool TryGetResult(UnityWebRequest request, out string result)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (request is { isDone: true, isHttpError: false, isNetworkError: false })
#pragma warning restore CS0618 // Type or member is obsolete
            {
                result = request.downloadHandler.text;
                return true;
            }

            result = "";
            return false;
        }
    }
}
