using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnthropicApp
{
    public class DynamicIdeState // Changed from public nested to internal standalone
    {
        // --- Identifier ---
        public string editor_id { get; set; }

        // --- Copied Configuration/Association Data (useful at runtime) ---
        public string description { get; set; }
        public string syntax_type { get; set; }
        public bool word_wrap { get; set; }
        public string project_path { get; set; }
        public string file_name { get; set; }
        // public string FileExtension { get; set; } // Optional, can derive from FilePath
        public string file_path { get; set; }

        // --- Core Runtime State ---
        public int line_count { get; set; }
        public bool? has_unsaved_changes { get; set; } = false;
        // public bool IsFocused { get; set; } // Optional: Manager's _activeId is likely sufficient
        public string content_preview { get; set; } // Generated preview ContentPreview
        public DateTime last_modified { get; set; } // LastModified
      
        [JsonProperty("is_focused", NullValueHandling = NullValueHandling.Ignore)]
        public bool is_focused { get; set; } = false;
        public static DynamicIdeState CreateDefault() => new()
        {
            line_count = 0,
            has_unsaved_changes = false,
            //IsFocused = false,
            last_modified = DateTime.Now,
            content_preview = string.Empty,
            syntax_type = "none"
        };

        // Add other relevant runtime state fields from DynamicIdeObject if needed
    }
}
