namespace LazyStack.Client.Base;
using System.Text.RegularExpressions;

public enum LzMessageUnits { Imperial, Metric }
/// <summary>
/// LzMessages provide a way to localize text in a Blazor app.
/// In addition to localization, messages can be tailored to a tenancy. 
/// LzMessages are stored in JSON object format. 
/// {
///    key: { "msg": "message text" }
/// }
/// 
/// External message resources are culture specific with a sufix determining the 
/// culture of the messages. ex: "_content/{assembly}/LzMessages.en-US.json".s
/// Message resources are loaded using IOSAccess.ContentReadAsync so 
/// you need to call SetOSAccess before loading external message resources. 
/// 
/// To load external message resources:
/// 1. Set the MessageDocs property to the list of message files. ex:
///     MessageDocs = new List<string> { 
///     "_content/MyApp/data/messages.json", 
///     "_content/MyApp/data/inventory.json,
///     "_content/Tenancy/MyApp/messages.json", // tenant messages override data messages
///     "_content/Tenancy/MyApp/inventory.json // tenant inventory override data inventory
///     };
/// 2. Call SetOSAccess() with an IOSAccess object. 
/// 3. Call SetMessageSetAsync("en-US, LzMessageUnits.Imperial") with the culture 
///    and units to load the specific language files and make the "message set" current.
/// 
/// To retrieve messages call Msg(key).
/// If a key is not found in the current culture, the key is searched for in the
/// internal messages. If the key is not found in the internal messages, the key
/// is returned.
/// 
/// A MessageSet is a combination of a culture and the units of measure. The LzMessageSet 
/// class is used to identify a MessageSet. The default Equals and GetHashCode methods 
/// are ovdrridden so that MessageSet can be used as a key in a dictionary easily.
/// 
/// Overrides/Tenancy:
/// When multiple message files are loaded for a culture, the keys in the last 
/// loaded file override keys in previously loaded files. This allows for 
/// customization by tenancy.
/// 
/// </summary>
public class LzMessages : NotifyBase, ILzMessages
{
	public LzMessages()
    {
		// Set the defaults for culture and units
		// This doesn't load any message files so the message set is empty.
		//MessageSet = new LzMessageSet("en-US", LzMessageUnits.Imperial);
        MessageSet = new LzMessageSet("en-US", LzMessageUnits.Imperial);
    }

    #region  public properites
    /// <inheritdoc />
    public List<(string culture, string name)> Cultures { get; set; } =  [("en-US", "English (United States)"), ("es-MX", "Español (Mexico)")];
    /// <inheritdoc />
    private LzMessageSet? _messageSet;
	public LzMessageSet MessageSet { 
		get { return _messageSet!;} 
		set { SetProperty(ref _messageSet, value); }
	} 
	/// <inheritdoc />
	public string AssetsUrl { get; set; }
	/// <inheritdoc />
	public string Culture => MessageSet.Culture;
	/// <inheritdoc />
	public LzMessageUnits Units => MessageSet.Units;
    /// <inheritdoc />
    public List<string> MessageFiles { get; set; } = new();
    /// <inheritdoc />
    public bool UseInspect { get; set; } = false;
	private int _refreshCount = 0;
    /// <inheritdoc />
    public int RefreshCount { get => _refreshCount; set => SetProperty(ref _refreshCount, value); }
    /// <inheritdoc />
    public bool Dirty 
	{
		get
		{
			foreach (var messageSet in _MessageSets.Values)
				if (messageSet.Dirty)
					return true;
			return false;
		}
	}
	#endregion

	#region protected properties
	protected IOSAccess? _oSAccess;
	/// <summary>
	/// Key is culture, value is LzMessageSet
	/// </summary>
    protected Dictionary<string,LzMessageSet> _MessageSets { get; set; } = new();
	#endregion

	#region public methods
	public void Refresh()
	{
		RefreshCount++;
	}
	/// <inheritdoc />
	public void SetOSAccess(IOSAccess oSAccess)
	{
		_oSAccess = oSAccess;
	}
    
	/// <inheritdoc />
	public async Task SetMessageSetAsync(string culture, LzMessageUnits units)
	{
		if(_oSAccess == null)
			throw new InvalidOperationException("SetOSAccess must be called before SetMessageSetAsync");
		if (_MessageSets.TryGetValue(culture, out LzMessageSet? messageSet))
		{
			MessageSet = messageSet;
			messageSet.Units = units;
            MessageSet.AssetsUrl = AssetsUrl;
            await MessageSet.LoadMessagesAsync(MessageFiles, _oSAccess);
        }
		else
		{
			MessageSet = new LzMessageSet(culture, units);
			_MessageSets.Add(culture, MessageSet);
			MessageSet.AssetsUrl = AssetsUrl;
			await MessageSet.LoadMessagesAsync(MessageFiles, _oSAccess);
		}
	}
	/// <inheritdoc />
	public string Msg(string key, bool ignoreUseInspect = false, LzMessageUnits? unitsArg = null)
    {
		if (string.IsNullOrEmpty(key)) return "";
        try
		{
			if (_oSAccess == null)
				return "";
			var msg = MessageSet.Msg(key, unitsArg);

			if (UseInspect && !ignoreUseInspect)
				msg = $"<span class=\"static-content-message\" key=\"{key}\">{msg}</span>";
			return msg;
		}
		catch (Exception ex)
		{
            return $"<span style='color:red;'>{key}, {ex.Message}</span>";
        }
    }
	/// <inheritdoc />
	public List<MsgItemModel> MsgItemModels(string key)
		=> MessageSet.MsgItemModels(key);
    /// <inheritdoc />
    public void SetMsgItem(string culture, string key, MsgItem msgItem)
    {
        // Todo - add html clean
       
    }

	public async Task SaveMessageSetsAsync()
	{
		foreach(var messageSet in _MessageSets.Values)
            await messageSet.SaveMessageSetAsync();
	}
    #endregion

    #region protected methods
		
	#endregion
}
