﻿#nullable enable

using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using NeuroSdk.Websocket;

namespace NeuroSdk.Messages.API
{
    [PublicAPI]
    public interface IIncomingMessageHandler
    {
        bool CanHandle(string command);
        ExecutionResult Validate(string command, MessageJData messageData, out object? parsedData);
        void ReportResult(object? parsedData, ExecutionResult result);
        UniTask ExecuteAsync(object? parsedData);
    }

    [PublicAPI]
    public abstract class IncomingMessageHandler : IIncomingMessageHandler
    {
        public abstract bool CanHandle(string command);
        protected abstract ExecutionResult Validate(string command, MessageJData messageData);
        protected abstract void ReportResult(ExecutionResult result);
        protected abstract UniTask ExecuteAsync();

        ExecutionResult IIncomingMessageHandler.Validate(string command, MessageJData messageData, out object? parsedData)
        {
            ExecutionResult result = Validate(command, messageData);
            parsedData = null;
            return result;
        }

        void IIncomingMessageHandler.ReportResult(object? parsedData, ExecutionResult result) => ReportResult(result);

        UniTask IIncomingMessageHandler.ExecuteAsync(object? parsedData) => ExecuteAsync();
    }

    [PublicAPI]
    public abstract class IncomingMessageHandler<T> : IIncomingMessageHandler
    {
        public abstract bool CanHandle(string command);
        protected abstract ExecutionResult Validate(string command, MessageJData messageData, out T? parsedData);
        protected abstract void ReportResult(T? parsedData, ExecutionResult result);
        protected abstract UniTask ExecuteAsync(T? parsedData);

        ExecutionResult IIncomingMessageHandler.Validate(string command, MessageJData messageData, out object? parsedData)
        {
            ExecutionResult result = Validate(command, messageData, out T? tParsedData);
            parsedData = tParsedData;
            return result;
        }

        void IIncomingMessageHandler.ReportResult(object? parsedData, ExecutionResult result) => ReportResult((T?) parsedData, result);

        UniTask IIncomingMessageHandler.ExecuteAsync(object? parsedData) => ExecuteAsync((T?) parsedData);
    }
}
