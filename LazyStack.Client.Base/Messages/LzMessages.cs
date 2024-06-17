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
/// 1. Set the _messageDocs property to the list of message files. ex:
///     LzMessages._messageDocs = new List<string> { 
///     "_content/MyApp/data/messages.json", 
///     "_content/MyApp/data/inventory.json,
///     "_content/Tenancy/MyApp/messages.json", // tenant messages override data messages
///     "_content/Tenancy/MyApp/inventory.json // tenant inventory override data inventory
///     };
/// 2. Call SetOSAccess() with an IOSAccess object. 
/// 3. Call SetMessageSetAsync(new LzMessageSet("en-US, LzMessageUnits.Imperial")) with the culture and units to load and make current.
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
		MessageSet = new LzMessageSet("en-US", LzMessageUnits.Imperial);
    
    }


	#region  public properites
	/// <inheritdoc />
	public List<(string culture, string name)> Cultures { get; set; } =  [("en-US", "English (United States)")];
	/// <inheritdoc />
	private LzMessageSet? _messageSet;
	public LzMessageSet MessageSet { 
		get { return _messageSet!;} 
		set { SetProperty(ref _messageSet, value); }
	} 
	/// <inheritdoc />
	public string Culture => MessageSet.Culture;
	/// <inheritdoc />
	public LzMessageUnits Units => MessageSet.Units;
	/// <inheritdoc />
	public List<string> MessageFiles { get; set; } = new();
	/// <inheritdoc />
	public bool UseInspect { get; set; } = false;
	#endregion

	#region protected properties
	protected IOSAccess? _oSAccess;
	/// <summary>
	/// Key is culture, value is LzMessageSet
	/// </summary>
    protected Dictionary<string,LzMessageSet> MessageSets { get; set; } = new();
	#endregion

	#region public methods
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
		if (MessageSets.TryGetValue(culture, out LzMessageSet? messageSet))
		{
			MessageSet = messageSet;
			messageSet.Units = units;
            await MessageSet.LoadMessagesAsync(MessageFiles, _oSAccess);
        }
		else
		{
			MessageSet = new LzMessageSet(culture, units);
			MessageSets.Add(culture, MessageSet);
			await MessageSet.LoadMessagesAsync(MessageFiles, _oSAccess);
		}
	}
	/// <inheritdoc />
	public string Msg(string key, bool ignoreUseInspect = false, LzMessageUnits? unitsArg = null)
    {
		if (_oSAccess == null)
			return "";
		var msg = MessageSet.Msg(key,unitsArg);

        if (UseInspect && !ignoreUseInspect)
            msg = $"<span class=\"static-content-message\" key=\"{key}\">{msg}</span>" ;
        return msg;
    }
	/// <inheritdoc />
	public List<(string file, DocMetaData docMetaData, string culture, MsgItem msgItem)> MsgItems(string key)
		=> MessageSet.MsgItems(key);
    /// <inheritdoc />
    public void SetMsgItem(string culture, string key, MsgItem msgItem)
    {
        // Todo - add html clean
       
    }
    #endregion

    #region protected methods
		
	#endregion
}
