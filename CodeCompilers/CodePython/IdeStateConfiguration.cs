using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CodeCompilers.CodePython
{
    public class IdeStateConfiguration
    {
        [JsonPropertyName("project_name")]
        public string? ProjectName { get; set; }

        [JsonPropertyName("saved_at")]
        public DateTime? SavedAt { get; set; }

        [JsonPropertyName("states")]
        public List<DynamicIdeObject>? States { get; set; }

        [JsonPropertyName("active_id")]
        public string? ActiveIdeId { get; set; }

        [JsonPropertyName("project_metadata")]
        public Dictionary<string, string>? ProjectMetadata { get; set; }


        //[JsonProperty("project_metadata", NullValueHandling = NullValueHandling.Ignore)]
        //public ProjectMetadata ProjectMetadata { get; set; } = new();
    }

    public class ProjectMetadata
    {
        [JsonPropertyName("last_edited_by")]
        public string? LastEditedBy { get; set; } = null;

        [JsonPropertyName("project_type")]
        public string? ProjectType { get; set; } = null;

        [JsonPropertyName("project_description")]
        public string? ProjectDescription { get; set; }
    }


}
