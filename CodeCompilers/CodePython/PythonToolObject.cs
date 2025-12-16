using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;

namespace AnthropicApp.Python
{
    public class PythonToolObject
    {
        [JsonPropertyName("editor_id")]
        public string? editor_id { get; set; }

        [JsonPropertyName("code")]
        public string? code { get; set; }

        [JsonPropertyName("version")]
        public string? version { get; set; }

        [JsonPropertyName("pip_commands")]
        public List<string>? pip_commands { get; set; }

        [JsonPropertyName("file_name")]
        public string? file_name { get; set; }

        [JsonPropertyName("project_path")]
        public string? project_path { get; set; }

        [JsonPropertyName("file_path")]
        public string? file_path { get; set; }

        //[JsonProperty("venv_name", NullValueHandling = NullValueHandling.Ignore)]
        //public string? venv_name { get; set; }  

        //[JsonProperty("overwrite", NullValueHandling = NullValueHandling.Ignore)]
        //public bool? overwrite { get; set; }  

        [JsonPropertyName("project_description")]
        public string? project_description { get; set; }
 
        //[JsonProperty("file_structure", NullValueHandling = NullValueHandling.Ignore)]
        //public string? file_structure { get; set; }
 
        //[JsonProperty("file_to_generate", NullValueHandling = NullValueHandling.Ignore)]
        //public string? file_to_generate { get; set; }

        //[JsonProperty("current_request", NullValueHandling = NullValueHandling.Ignore)]
        //public string? current_request { get; set; }
 
        //[JsonProperty("tool_use_log", NullValueHandling = NullValueHandling.Ignore)]
        //public string? tool_use_log { get; set; }

        //[JsonProperty("chat_message", NullValueHandling = NullValueHandling.Ignore)]
        //public string? chat_message { get; set; }

    }
}