using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCompilers.CodePython
{
    public class IdeStateConfiguration
    {
        [JsonProperty("project_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? ProjectName { get; set; }



        [JsonProperty("saved_at", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? SavedAt { get; set; }


        [JsonProperty("states", NullValueHandling = NullValueHandling.Ignore)]
        public List<DynamicIdeObject>? States { get; set; }  


        [JsonProperty("active_id", NullValueHandling = NullValueHandling.Ignore)]
        public string? ActiveIdeId { get; set; }


        [JsonProperty("project_metadata", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string>? ProjectMetadata { get; set; }


        //[JsonProperty("project_metadata", NullValueHandling = NullValueHandling.Ignore)]
        //public ProjectMetadata ProjectMetadata { get; set; } = new();
    }

    public class ProjectMetadata
    {

        [JsonProperty("last_edited_by", NullValueHandling = NullValueHandling.Ignore)]
        public string? LastEditedBy { get; set; } = null;

        [JsonProperty("project_type", NullValueHandling = NullValueHandling.Ignore)]
        public string? ProjectType { get; set; } = null;

        [JsonProperty("project_description", NullValueHandling = NullValueHandling.Ignore)]
        public string? ProjectDescription { get; set; }
 
    }


}
