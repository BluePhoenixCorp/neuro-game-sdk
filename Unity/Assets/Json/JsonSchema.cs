﻿#nullable enable

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace NeuroSdk.Json
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public sealed class JsonSchema
    {
        [JsonIgnore]
        public Dictionary<string, JsonSchema> Defs
        {
            get => _defs ??= new Dictionary<string, JsonSchema>();
            set => _defs = value;
        }

        [JsonIgnore]
        public List<JsonSchema> AnyOf
        {
            get => _anyOf ??= new();
            set => _anyOf = value;
        }

        [JsonIgnore]
        public List<JsonSchema> OneOf
        {
            get => _oneOf ??= new();
            set => _oneOf = value;
        }

        [JsonIgnore]
        public List<JsonSchema> AllOf
        {
            get => _allOf ??= new();
            set => _allOf = value;
        }

        [JsonIgnore]
        public Dictionary<string, JsonSchema> Properties
        {
            get => _properties ??= new();
            set => _properties = value;
        }

        [JsonIgnore]
        public JsonSchemaType Type
        {
            get
            {
                return _type switch
                {
                    "string" => JsonSchemaType.String,
                    "number" => JsonSchemaType.Float,
                    "integer" => JsonSchemaType.Integer,
                    "boolean" => JsonSchemaType.Boolean,
                    "object" => JsonSchemaType.Object,
                    "array" => JsonSchemaType.Array,
                    "null" => JsonSchemaType.Null,
                    _ => JsonSchemaType.None
                };
            }
            set
            {
                _type = value switch
                {
                    JsonSchemaType.String => "string",
                    JsonSchemaType.Float => "number",
                    JsonSchemaType.Integer => "integer",
                    JsonSchemaType.Boolean => "boolean",
                    JsonSchemaType.Object => "object",
                    JsonSchemaType.Array => "array",
                    JsonSchemaType.Null => "null",
                    _ => null
                };
            }
        }

        [JsonIgnore]
        public List<object> Enum
        {
            get => _enum ??= new();
            set => _enum = value;
        }

        [JsonIgnore]
        public Dictionary<string, List<string>> DependentRequired
        {
            get => _dependentRequired ??= new();
            set => _dependentRequired = value;
        }

        [JsonIgnore]
        public List<string> Required
        {
            get => _required ??= new();
            set => _required = value;
        }

        #region Keywords

        [JsonProperty("$ref")]
        public string? Ref { get; set; }

        [JsonProperty("$defs")]
        private Dictionary<string, JsonSchema>? _defs;

        [JsonProperty("$anchor")]
        public string? Anchor { get; set; }

        [JsonProperty("anyOf")]
        private List<JsonSchema>? _anyOf;

        [JsonProperty("oneOf")]
        private List<JsonSchema>? _oneOf;

        [JsonProperty("allOf")]
        private List<JsonSchema>? _allOf;

        [JsonProperty("not")]
        public JsonSchema? Not { get; set; }

        [JsonProperty("properties")]
        private Dictionary<string, JsonSchema>? _properties;

        [JsonProperty("additionalProperties")]
        public bool? AdditionalProperties { get; set; }

        [JsonProperty("items")]
        public JsonSchema? Items { get; set; }

        [JsonProperty("type")]
        private string? _type;

        [JsonProperty("enum")]
        private List<object>? _enum;

        [JsonProperty("const")]
        public object? Const { get; set; }

        [JsonProperty("minLength")]
        public int? MinLength { get; set; }

        [JsonProperty("pattern")]
        public string? Pattern { get; set; }

        [JsonProperty("maxLength")]
        public int? MaxLength { get; set; }

        [JsonProperty("maximum")]
        public int? Maximum { get; set; }

        [JsonProperty("exclusiveMinimum")]
        public int? ExclusiveMinimum { get; set; }

        [JsonProperty("exclusiveMaximum")]
        public int? ExclusiveMaximum { get; set; }

        [JsonProperty("minimum")]
        public int? Minimum { get; set; }

        [JsonProperty("dependentRequired")]
        private Dictionary<string, List<string>>? _dependentRequired;

        [JsonProperty("required")]
        private List<string>? _required;

        [JsonProperty("minItems")]
        public int? MinItems { get; set; }

        [JsonProperty("maxItems")]
        public int? MaxItems { get; set; }

        [JsonProperty("uniqueItems")]
        public bool? UniqueItems { get; set; }

        [JsonProperty("format")]
        public string? Format { get; set; }

        #endregion
    }
}
