namespace LazyStack.Client.Base;

/// <summary>
/// ILzMessages is an interface for managing messages in a multi-language, multi-tenancy application.
/// 
/// </summary>
public interface ILzMessages : INotifyPropertyChanged
{
    // Properties
    /// <summary>
    /// List of cultures supported with frieldly names.
    /// ex: ("en-US", "English (United States)")
    /// </summary>
    public List<(string culture, string name)> Cultures { get; set; }
    /// <summary>
    /// The List of message files to be loaded. These file names do not include 
    /// the culture. The culture is added to the file name before loading.
    /// ex: "_content/MyApp/data/messages.json" -> "_content/MyApp/data/messages.en-US.json"
    /// </summary>
    public List<string> MessageFiles { get; set; }
    public string Culture { get; }
    public LzMessageUnits Units { get; }
    public LzMessageSet MessageSet { get; }
    public bool UseInspect { get; set; }


    // Methods
    public void SetOSAccess(IOSAccess oSAccess);
    /// <summary>
    /// Loads the specified message files for the specified culture.
    /// </summary>
    /// <param name="culture"></param>
    /// <param name="units"></param>
    /// <returns></returns>
    public Task SetMessageSetAsync(string culture, LzMessageUnits units);
    /// <summary>
    /// Returns the message for the specified key. If the key is not found, the key is returned.
    /// When the UseInspect property is true and ignoreUseInspect is false, the key is returned 
    /// in a span tag with the attributes class="static-content-message" and key="{key}". This
    /// enables the WYSIWYG editor to display the key in the editor.
    /// Note that this method uses the current _msgs dictionary where all the merging, variable 
    /// substitution and unit processing has been done. This is the method to use when displaying
    /// messages in the UI.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="ignoreUseInspect"></param>
    /// <returns></returns>
    public string Msg(string key, bool ignoreUseInspect = false, LzMessageUnits? unitsArg = null);
    /// <summary>
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public List<(string file, DocMetaData docMetaData, string culture, MsgItem msgItem)> MsgItems(string key);
    /// <summary>
    ///  This method returns an array of tuples containing the file name and the message for the specified key.
    ///  This method is used by the WYSIWYG editor to display the messages for the key.
    /// </summary>
    /// <param name="culture"></param>
    /// <param name="key"></param>
    /// <param name="msgItem"></param>
    /// <returns></returns>
    public void SetMsgItem(string culture, string key, MsgItem msgItem);

    //public string PreviewMsg(string culture, string key);


}
