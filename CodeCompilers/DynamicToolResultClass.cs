using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnthropicApp
{
    public class DynamicToolResultClass
    {
        [JsonProperty("editor_id", NullValueHandling = NullValueHandling.Ignore)]
        public string? editor_id { get; set; }

        [JsonProperty("success", NullValueHandling = NullValueHandling.Ignore)]
        public bool success { get; set; }

        [JsonProperty("is_error", NullValueHandling = NullValueHandling.Ignore)]
        public bool is_error { get; set; }

        [JsonProperty("syntax_type", NullValueHandling = NullValueHandling.Ignore)]
        public string? syntax_type { get; set; }

        [JsonProperty("output_content", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> output_content { get; set; } = new();

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string? description { get; set; }

        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string? message { get; set; }

        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public object? result { get; set; }


        [JsonProperty("file_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? file_name { get; set; }


        [JsonProperty("chat_message", NullValueHandling = NullValueHandling.Ignore)]
        public string? chat_message { get; set; }

        [JsonProperty("tool_use_log", NullValueHandling = NullValueHandling.Ignore)]
        public string? tool_use_log { get; set; }


        [JsonProperty("image_data_base64", NullValueHandling = NullValueHandling.Ignore)]
        public byte[]? image_data_base64 { get; set; }


        [JsonProperty("image_data_string", NullValueHandling = NullValueHandling.Ignore)]
        public string? image_data_string { get; set; }




        [JsonProperty("exception", NullValueHandling = NullValueHandling.Ignore)]
        public Exception? exception { get; set; }

        public static DynamicToolResultClass CreateSuccess(string ideId, string description = null) => new()
        {
            editor_id = ideId,
            success = true,
            is_error = false,
            description = description,
            output_content = new List<string>()
        };

        public static DynamicToolResultClass CreateError(string message) => new()
        {
            success = false,
            is_error = true,
            output_content = new List<string> { message }
        };
    }

}
