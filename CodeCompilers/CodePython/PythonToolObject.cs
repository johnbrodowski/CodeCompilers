using Newtonsoft.Json;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace AnthropicApp.Python
{
    public class PythonToolObject
    {
        [JsonProperty("editor_id", NullValueHandling = NullValueHandling.Ignore)]
        public string? editor_id { get; set; } 

        [JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
        public string? code { get; set; }

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string? version { get; set; } 

        [JsonProperty("pip_commands", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? pip_commands { get; set; }
 
        [JsonProperty("file_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? file_name { get; set; }

        [JsonProperty("project_path", NullValueHandling = NullValueHandling.Ignore)]
        public string? project_path { get; set; }

        [JsonProperty("file_path", NullValueHandling = NullValueHandling.Ignore)]
        public string? file_path { get; set; }

        //[JsonProperty("venv_name", NullValueHandling = NullValueHandling.Ignore)]
        //public string? venv_name { get; set; }  

        //[JsonProperty("overwrite", NullValueHandling = NullValueHandling.Ignore)]
        //public bool? overwrite { get; set; }  

        [JsonProperty("project_description", NullValueHandling = NullValueHandling.Ignore)]
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