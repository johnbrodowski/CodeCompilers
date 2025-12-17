using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;

namespace CodeCompilers.Python
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

        [JsonPropertyName("project_description")]
        public string? project_description { get; set; }
    }
}