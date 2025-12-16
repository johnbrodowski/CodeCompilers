 
using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.Drawing;


namespace AnthropicApp
{
    /// <summary>
    /// Represents a dynamic Editor object that manages Editor instances and their states
    /// </summary>
    public class DynamicIdeObject
    {
        #region Basic Properties

        [JsonProperty("editor_id", NullValueHandling = NullValueHandling.Ignore)]
        public string? editor_id { get; set; } = "none";

        [JsonProperty("project_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? project_name { get; set; }  

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string? description { get; set; }  

        [JsonProperty("syntax_type", NullValueHandling = NullValueHandling.Ignore)] // e.g., "python", "csharp", "none"
        public string? syntax_type { get; set; } = "python";

        [JsonProperty("word_wrap", NullValueHandling = NullValueHandling.Ignore)]
        public bool word_wrap { get; set; } = false;

        #endregion Basic Properties

 
        #region File Properties

        [JsonProperty("file_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? file_name { get; set; }


        [JsonProperty("chat_message", NullValueHandling = NullValueHandling.Ignore)]
        public string? chat_message { get; set; }

        [JsonProperty("tool_use_log", NullValueHandling = NullValueHandling.Ignore)]
        public string? tool_use_log { get; set; }







        [JsonProperty("project_path", NullValueHandling = NullValueHandling.Ignore)]
        public string? project_path { get; set; }  

        [JsonProperty("file_extension", NullValueHandling = NullValueHandling.Ignore)]
        public string? file_extension { get; set; }

        [JsonProperty("file_path", NullValueHandling = NullValueHandling.Ignore)]
        public string? file_path { get; set; }
         
        [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
        public string? path { get; set; }
        
        [JsonProperty("overwrite", NullValueHandling = NullValueHandling.Ignore)]
        public bool overwrite { get; set; }  

        [JsonProperty("save_or_load", NullValueHandling = NullValueHandling.Ignore)]
        public string? save_or_load { get; set; }  

        #endregion File Properties
         

        #region UI Properties

        [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
        public string? location { get; set; } 

        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        public string? size { get; set; }  

        #endregion UI Properties
         
        #region State Properties

        [JsonProperty("line_count", NullValueHandling = NullValueHandling.Ignore)]
        public int line_count { get; set; } = 0;

      
        [JsonProperty("last_modified", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? last_modified { get; set; }  
        

        [JsonProperty("has_unsaved_changes", NullValueHandling = NullValueHandling.Ignore)]
        public bool has_unsaved_changes { get; set; } = false; 
        
                 
        [JsonProperty("content_preview", NullValueHandling = NullValueHandling.Ignore)]
        public string? content_preview { get; set; }  
        
         
        [JsonProperty("is_focused", NullValueHandling = NullValueHandling.Ignore)]
        public bool is_focused { get; set; } = false;

        [JsonProperty("include_content_preview", NullValueHandling = NullValueHandling.Ignore)]
        public bool include_content_preview { get; set; } = false;

        [JsonProperty("included_content", NullValueHandling = NullValueHandling.Ignore)] // 'full', 'preview' or 'none'
        public string? included_content { get; set; }


        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public string? content { get; set; } 


        [JsonProperty("query_type", NullValueHandling = NullValueHandling.Ignore)]
        public string? query_type { get; set; } 

        #endregion State Properties






        #region Internal Collections

        [JsonIgnore]
        public DynamicToolResultClass dynamic_tool_result { get; set; } = new();

 

        [JsonIgnore]
        private readonly Dictionary<string, DynamicIdeState> _states = new();

 

        #endregion Internal Collections



 
         
        #region Nested Classes

        //public class DynamicIdeState
        //{
        //    public int LineCount { get; set; }
        //    public bool HasUnsavedChanges { get; set; }
        //    public bool IsFocused { get; set; }
        //    public string? ContentPreview { get; set; }
        //    public DateTime LastModified { get; set; }
        //    public string? SyntaxType { get; set; }

        //    public static DynamicIdeState CreateDefault() => new()
        //    {
        //        LineCount = 0,
        //        HasUnsavedChanges = false,
        //        IsFocused = false,
        //        LastModified = DateTime.Now,
        //        ContentPreview = string.Empty,
        //        SyntaxType = "none"
        //    };
        //}


        //public class IdeStateConfiguration
        //{
        //    public string ProjectName { get; set; }
        //    public DateTime SavedAt { get; set; }
        //    public List<DynamicIdeObject> States { get; set; } = new();
        //    public string ActiveIdeId { get; set; }
        //    public Dictionary<string, string> ProjectMetadata { get; set; } = new();
        //}

        #endregion Nested Classes
         
        //#region TextBox Management Methods

        //public FastColoredTextBox CreateTextBox()
        //{
        //    var textBox = new FastColoredTextBox
        //    {
        //        Name = editor_id,
        //        Location = ParseLocation(location),
        //        Size = ParseSize(size),
        //        WordWrap = word_wrap,
        //        BackColor = Color.Black,
        //        ForeColor = Color.White,
        //        Font = new Font("Courier New", 10F),
        //        Visible = false,
        //        ReadOnly = false,
        //        Dock = DockStyle.None,
        //        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        //    };

        //    if (!string.IsNullOrEmpty(editor_id))
        //    {
        //        _textBoxes[editor_id] = textBox;
        //        _states[editor_id] = DynamicIdeState.CreateDefault();
        //    }

        //    return textBox;
        //}

        //public void UpdateTextBox(string id, FastColoredTextBox textBox)
        //{
        //    if (_states.TryGetValue(id, out var state))
        //    {
        //        state.LineCount = textBox.LinesCount;
        //        state.LastModified = DateTime.Now;
        //        state.ContentPreview = GenerateContentPreview(textBox);
        //        state.HasUnsavedChanges = true;
        //    }
        //}

        //public FastColoredTextBox GetTextBox(string id)
        //{
        //    return _textBoxes.TryGetValue(id, out var textBox) ? textBox : null;
        //}

        //public void RemoveTextBox(string id)
        //{
        //    if (_textBoxes.TryGetValue(id, out var textBox))
        //    {
        //        textBox.Dispose();
        //        _textBoxes.Remove(id);
        //        _states.Remove(id);
        //        _buttons.Remove(id);
        //    }
        //}

        //#endregion TextBox Management Methods
         
        //#region State Management Methods

        //public DynamicIdeState GetState(string id)
        //{
        //    return _states.TryGetValue(id, out var state) ? state : null;
        //}

        //public List<DynamicIdeState> GetAllStates()
        //{
        //    return _states.Values.ToList();
        //}

        //public void SetActive(string id, bool active = true)
        //{
        //    if (_states.TryGetValue(id, out var state))
        //    {
        //       // state.IsFocused = active;
        //        StateChanged?.Invoke(this, new IdeStateChangedEventArgs(id,
        //            active ? IdeStateChangeType.Activated : IdeStateChangeType.Created));
        //    }
        //}

        //public bool ValidateState(string id)
        //{
        //    if (!_textBoxes.TryGetValue(id, out var textBox))
        //        return false;

        //    return textBox != null &&
        //           textBox.Parent != null &&
        //           textBox.Visible &&
        //           _states.ContainsKey(id);
        //}

        //#endregion State Management Methods
         
        //#region File Management Methods

        //public async Task SaveContentAsync(string id, string filePath)
        //{
        //    if (!_textBoxes.TryGetValue(id, out var textBox))
        //        throw new KeyNotFoundException($"Editor with ID {id} not found");

        //    try
        //    {
        //        var directory = Path.GetDirectoryName(filePath);
        //        if (!string.IsNullOrEmpty(directory))
        //            Directory.CreateDirectory(directory);

        //        await File.WriteAllTextAsync(filePath, textBox.Text);
        //        UpdateFilePaths(id, filePath);
        //    }
        //    catch (Exception ex)
        //    {
        //        Error?.Invoke(this, new IdeErrorEventArgs(id, "Failed to save file", ex));
        //        throw;
        //    }
        //}

        //public void UpdateFilePaths(string id, string filePath)
        //{
        //    if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(filePath))
        //        return;

        //    var fullPath = Path.GetFullPath(filePath);
        //    project_path = Path.GetDirectoryName(fullPath);
        //    file_name = Path.GetFileNameWithoutExtension(fullPath);
        //    file_extension = Path.GetExtension(fullPath).TrimStart('.');
        //    file_path = fullPath;
        //}

        //#endregion File Management Methods
         
        //#region Helper Methods

        //private Point ParseLocation(string locationStr)
        //{
        //    try
        //    {
        //        var parts = locationStr?.Replace(" ", "").Split(',');
        //        if (parts?.Length == 2 &&
        //            int.TryParse(parts[0], out int x) &&
        //            int.TryParse(parts[1], out int y))
        //        {
        //            return new Point(x, y);
        //        }
        //    }
        //    catch { }
        //    return new Point(583, 40); // Default location
        //}

        //private Size ParseSize(string sizeStr)
        //{
        //    try
        //    {
        //        var parts = sizeStr?.Replace(" ", "").Split(',');
        //        if (parts?.Length == 2 &&
        //            int.TryParse(parts[0], out int width) &&
        //            int.TryParse(parts[1], out int height))
        //        {
        //            return new Size(width, height);
        //        }
        //    }
        //    catch { }
        //    return new Size(533, 309); // Default size
        //}

        //private string GenerateContentPreview(FastColoredTextBox textBox)
        //{
        //    if (textBox.LinesCount == 0) return string.Empty;

        //    return string.Join("\n",
        //        Enumerable.Range(0, textBox.LinesCount)
        //        .Select(i => $"{i + 1}: {textBox[i].Text}"));
        //}

        //#endregion Helper Methods
   
    
    
    }
}