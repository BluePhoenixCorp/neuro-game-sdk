﻿#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NeuroSdk.Actions
{
    /// <summary>
    /// A wrapper class for the data of an <see cref="NeuroSdk.Messages.Incoming.Action"/> message.
    /// </summary>
    [PublicAPI]
    public sealed class ActionJData
    {
        public JToken? Data { get; private set; }

        private ActionJData()
        {
        }

        private void DeserializeFromJson(string? stringifiedData)
        {
            if (string.IsNullOrEmpty(stringifiedData))
            {
                Data = null;
                return;
            }

            Data = JToken.Parse(stringifiedData);
        }

        internal static bool TryParse(string? stringifiedData, [NotNullWhen(true)] out ActionJData? actionJData)
        {
            try
            {
                actionJData = new ActionJData();
                actionJData.DeserializeFromJson(stringifiedData);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to deserialize ActionJData from string.");
                Debug.LogError(e);
                actionJData = null;
                return false;
            }
        }
    }
}
