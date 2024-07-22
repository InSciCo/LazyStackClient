using ReactiveUI;
using System;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace LazyStack.Client.Base;

/// <summary>
/// Loads the messages for a specific culture. Provides them in 
/// both Imperial and Metric units. 
/// This class implements a lazy process pattern where only the 
/// default units are processed initially.
/// </summary>
public class LzMessageSet : NotifyBase
{
    /// <summary> 
    /// 
    /// </summary>
    /// <param name="culture">Culture to load. ex: en-US</param>
    /// <param name="defaultUnits">Initial units. Ex: LzMessageUnits.Imperial </param>
    public LzMessageSet(string culture, LzMessageUnits defaultUnits)
    {
        Culture = culture;
        Units = defaultUnits;
    }

    #region Public Properties
    public string Culture {  get; private set; }
    private LzMessageUnits _units;
    public LzMessageUnits Units 
    {   get => _units; 
        set => SetProperty(ref _units, value); 
    }
    private bool _dirty;
    public bool Dirty 
    { 
        get 
        {
            var foundDirty = false;
            foreach (var messageDoc in MessageDocs.Values)
                if (messageDoc.Dirty)
                {
                    foundDirty = true; 
                    break;
                }
            if(!(foundDirty == _dirty))
                SetProperty(ref _dirty, foundDirty);
            return _dirty;   
        } 
    }
    public Dictionary<string, MessageDoc> MessageDocs { get; } = new Dictionary<string, MessageDoc>();
    public string AssetsUrl { get; set; }
    public Dictionary<string, MsgItemsModel> MsgItemsModels { get; } = new Dictionary<string, MsgItemsModel>(); // key is the Msg Key
    private MsgItemsModel _msgItemsModel;
    public MsgItemsModel CurrentMsgItemsModel 
        { get => _msgItemsModel;
          private set => SetProperty(ref _msgItemsModel, value);
        }
    #endregion

    #region Private Members
    protected IOSAccess? _oSAccess;
    private Dictionary<string, string> _msgsImperial = new Dictionary<string, string>();
    private Dictionary<string, string> _msgsMetric = new Dictionary<string, string>();
    private bool _keepDocs = false;
    private List<string> _messageFiles = new List<string>();
    #endregion

    /// <summary>
    /// Get a message by key and optionally override the units.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="unitsArg">Optional units </param>
    /// <returns></returns>
    public string Msg(string key, LzMessageUnits? unitsArg = null)
    {
        var units = unitsArg ?? Units;
        var msgs = (units == LzMessageUnits.Imperial) ? _msgsImperial : _msgsMetric;

        if (msgs.Count == 0)
            UpdateMsgs(units);

        msgs = (units == LzMessageUnits.Imperial) ? _msgsImperial : _msgsMetric;

        if (msgs.TryGetValue(key, out string value))
            return value;

        return key;
    }

    /// <summary>
    /// Create a new MsgItemsModel which contains  a list of editable message items for a given key 
    /// There are entries in the MsgItemsModel.Items for each document the key exists in for
    /// this message set.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// 
    public MsgItemsModel SetMsgItemsModel(string key)
    {
        if(MsgItemsModels.TryGetValue(key, out MsgItemsModel msgItemsModel)) 
        {
            CurrentMsgItemsModel = msgItemsModel;
            return msgItemsModel;
        }
        
        msgItemsModel = new MsgItemsModel(this, key);
        MsgItemsModels.Add(key, msgItemsModel);
        CurrentMsgItemsModel = msgItemsModel;
        return msgItemsModel;
    }
    //public List<MsgItemModel> MsgItemModels(string key)
    //{
    //    var items = new List<MsgItemModel>();
    //    if (string.IsNullOrEmpty(key))
    //        return items;
    //    var lastMsg = ""; // Used to provide default on new MstgItem creation
    //    foreach (var messageDoc in MessageDocs)
    //    {
    //        if (CurrentMsgItemModels.TryGetValue((messageDoc.Key, key), out MsgItemModel? existingMsgItemModel))
    //        {
    //            if (existingMsgItemModel.MsgItemState != MsgItemState.Clean)
    //            {
    //                items.Add(existingMsgItemModel);
    //                continue;
    //            }
    //        }
    //        MsgItem? msgItem;
    //        _ = messageDoc.Value.Messages.TryGetValue(key, out msgItem);
    //        var isEditable = (msgItem != null && (msgItem.Editable ?? false)) || messageDoc.Value.DocMetaData.Editable;
    //        var isEmpty = msgItem == null || string.IsNullOrEmpty(msgItem.Msg);

    //        if (!isEditable && isEmpty)
    //            continue;

    //        var msgItemModel = new MsgItemModel()
    //        {
    //            MsgItemsModel = this,
    //            Key = key,
    //            File = messageDoc.Key
    //        };
    //        if(msgItem is not null)
    //        {
    //            if (msgItemModel.MsgItemState == MsgItemState.Clean)
    //            {
    //                msgItemModel.Msg = msgItem.Msg;
    //                msgItemModel.Editable = msgItem.Editable ?? messageDoc.Value.DocMetaData.Editable;
    //            }
    //        } else
    //        {
    //            msgItemModel.Msg = lastMsg;
    //            msgItemModel.Editable = messageDoc.Value.DocMetaData.Editable;
    //        }
    //        lastMsg = msgItemModel.Msg;

    //        items.Add(msgItemModel);
    //        CurrentMsgItemModels[(messageDoc.Key, key)] = msgItemModel;
    //    }
    //    return items;
    //}

    public async Task LoadMessagesAsync(List<string> messageFiles, IOSAccess osAccess, bool keepDocs = false)
    {
        _messageFiles = messageFiles;
        _keepDocs = keepDocs;
        _oSAccess = osAccess;
        foreach (var msgFile in messageFiles)
        {
            // msgFile example: "messages.json"
            var filePath = "";
            try
            {
                filePath = FilePathWithCulture(msgFile, Culture); // ex: "messages.en-US.json"

                var json = await _oSAccess.ReadContentAsync(filePath);
                if (!string.IsNullOrEmpty(json))
                {
                    var doc = JsonConvert.DeserializeObject<MessageDoc>(json)!;
                    MessageDocs[filePath] = doc;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading messages file: {filePath} {ex.Message}");
            }
        }
        UpdateMsgs();
    }
    public void UpdateMsgs(LzMessageUnits? unitsArg = null, string? key = null)
    {
        foreach (LzMessageUnits units in Enum.GetValues(typeof(LzMessageUnits)))
        {
            if (unitsArg != null && unitsArg != units)
                continue;

            var msgs = (units == LzMessageUnits.Imperial)
                ? _msgsImperial
                : _msgsMetric;
            if (string.IsNullOrEmpty(key)) msgs.Clear();
            try
            {
                if (_oSAccess == null)
                    throw new Exception("SetOSAccess must be called before SetMessageSetAsync.");
                foreach (var msgFile in _messageFiles) // preserve the precidence order of the files
                {
                    var filePath = FilePathWithCulture(msgFile, Culture);
                    if (MessageDocs.TryGetValue(filePath, out MessageDoc? doc))
                    {
                        if (key is not null)
                        {
                            if (doc.Messages.TryGetValue(key, out MsgItem msgItem))
                                msgs[key] = GetMessage(key!, filePath, msgItem.Msg);
                        }
                        else
                        {
                            foreach (var msg in doc.Messages)
                                msgs[msg.Key] = GetMessage(key!, filePath, msg.Value.Msg);
                        }
                    }
                }
                ReplaceVars(units, key); // Performs variable substitution and Units conversion in msgs
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting message set: {ex.Message}");
            }
        }
    }
    /// <summary>
    /// Gets the current message. Accomodates messages stored in MsgItemsModels.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="filePath"></param>
    /// <param name="msgs"></param>
    /// <returns></returns>
    private string GetMessage(string key, string filePath, string msg)
    {

        if (key != null && filePath != null && MsgItemsModels.TryGetValue(key, out MsgItemsModel messageItemsModel))
            return messageItemsModel.Items[filePath].Msg;
        
        return msg;
    }

    public async Task SaveMessageSetAsync()
    {
        foreach(var messageDoc in MessageDocs)
            await messageDoc.Value.SaveAsync(messageDoc.Key);
        MsgItemsModels.Clear();
    }
    protected string MergeMessages(string key)
    {
        var msg = key;
        foreach(var messageDoc in MessageDocs.Values)
            if (messageDoc.Messages.TryGetValue(key, out MsgItem? msgItem))
                msg = msgItem.Msg;
        return msg;
    }
    protected string FilePathWithCulture(string filePath, string culture) 
    {
		// ex: "Assets/System/BlazoriseComponentMessage.json"
		var fileName = Path.GetFileNameWithoutExtension(filePath); // BlazorseComponentMessage
		var directoryPath = Path.GetDirectoryName(filePath); // Assets/System 
		var newDirectoryName = directoryPath + "-" + culture; 
		return Path.Combine(newDirectoryName, fileName + "." + culture + ".json");
    }
    protected bool TryGetMsg(string key, out string msg, LzMessageUnits? unitsArg = null)
    {
        var units = unitsArg ?? Units;
        var msgs = (units == LzMessageUnits.Imperial) ? _msgsImperial : _msgsMetric;
        msg = key;
        if (key == null)
            return false;

        if (key == "Nothing")
            return false;
        // Try and get the message from the current culture messages
        if (msgs.TryGetValue(key, out string? value))
            msg = string.IsNullOrEmpty(value) ? key : value;
        return !key.Equals(msg);
    }
    protected string ReplaceUnits(string msg, LzMessageUnits? unitsArg = null)
    {
        if (!msg.Contains("@Unit")) // typically, most messages don't have units, this is a quick check to see if we need to do anything
            return msg;

        var units = unitsArg ?? Units;

        var msgIn = msg;
        MatchCollection matches;

        // Process @Unit() functions 
        while ((matches = Regex.Matches(msg, "@Unit\\((.*?)\\)")).Count > 0)
        {
            foreach (Match match in matches)
            {
                var val = match.Value.Substring(6, match.Value.Length - 7);
                msg = msg.Replace(match.Value, ProcessUnitConversion(val, units));
            }
        }

        // Process @UnitS() functions 
        while ((matches = Regex.Matches(msg, "@UnitS\\((.*?)\\)")).Count > 0)
        {
            foreach (Match match in matches)
            {
                var val = match.Value.Substring(7, match.Value.Length - 8);
                msg = msg.Replace(match.Value, ProcessUnitS(val, units));
            }
        }

        return msg;
    }
    protected void ReplaceVars(LzMessageUnits? unitsArg = null, string? keyArg = null )
    {
        var units = unitsArg ?? Units;  
        var msgs = (units == LzMessageUnits.Imperial) ? _msgsImperial : _msgsMetric;
        // Refactored to support C# 8.0 which is the latest supported by .netstandard2.0 target
        if (keyArg is null)
        {
            for (var i = 0; i < msgs.Count; i++)
            {
                // replace variables satisfying the keyPattern '__.*__' with the value of the key
                var msg = msgs.ElementAt(i).Value;
                var key = msgs.ElementAt(i).Key;
                msg = ReplaceVars(msg, units);
                msgs[key] = ReplaceUnits(msg, units);
            }
        } else
        {
            var msg = msgs[keyArg];
            msg = ReplaceVars(msg, units);
            msg = ReplaceUnits(msg, units);
            msgs[keyArg] = msg;
        }
    }
    protected string ReplaceVars(string msg, LzMessageUnits? unitsArg = null)
    {
        var units = unitsArg ?? Units;
        var msgs = (units == LzMessageUnits.Imperial) ? _msgsImperial : _msgsMetric;

        // replace variables satisfying the keyPattern '__.*__' with the value of the key
        MatchCollection matches;
        while ((matches = Regex.Matches(msg, keyPattern)).Count > 0)
        {
            foreach (Match match in matches)
            {
                // Using Substring instead of range operator
                var matchValue = match.Value.Substring(2, match.Value.Length - 4);

                if (msgs.TryGetValue(matchValue, out string? replacement))
                    msg = msg.Replace(match.Value, replacement);
                else
                    throw new Exception($"Msgs[{matchValue}] not found.");
            }
        }
        return msg;
    }
    const string keyPattern = "__.*__";
    static string[] imperialUnits = { "in", "\"", "ft", "'", "yd", "mi", "oz", "lb", "sq in", "sq ft" };
    static string[] metricUnits = { "mm", "cm", "m", "km", "g", "kg", "sq mm", "sq cm", "sq m" };
    static Dictionary<string, string> defaultConversions() => new()
    {
        { "in", "mm" },
        { "\"", "mm" },
        { "ft", "m" },
        { "'", "m" },
        { "yd", "m" },
        { "mi", "km" },
        { "oz", "g" },
        { "lb", "kg" },
        { "sq in", "sq cm" },
        { "sq ft", "sq m"},
        { "mm", "\"" },
        { "cm", "\"" },
        { "m", "'" },
        { "km", "mi" },
        { "g", "oz" },
        { "kg", "lb" },
        { "sq mm", "sq in" },
        { "sq cm", "sq in" },
        { "sq m", "sq ft"}
    };
    static Dictionary<string, (double factor, int precision)> conversionFactors = new()
    {
        { "in,mm", (25.4, 2)},
        { "\",mm", (25.4, 2)},
        { "ft,m",  (0.3048, 1)},
        { "',m",  (0.3048, 1)},
        { "yd,m", (0.9144,1) },
        { "mi,km", (1.609344, 2) },
        { "oz,g", (28.349523125,0) },
        { "lb,kg", (0.45359237, 1) },
        { "sq in,sq cm", (6.4516, 0) },
        { "sq ft,sq m", (0.09290304, 1)},
        { "mm,in", (0.0393700787, 2) },
        { "mm,\"", (0.0393700787, 2) },
        { "cm,in", (0.393700787, 2) },
        { "cm,\"", (0.393700787, 2) },
        { "m,ft", (3.2808399, 1) },
        { "m,'", (3.2808399, 1) },
        { "km,mi", (0.621371192, 2) },
        { "g,oz", (0.0352739619, 2) },
        { "kg,lb", (2.20462262, 1) },
        { "sq mm,sq in", (0.0015500031,1) },
        { "sq cm,sq in", (0.15500031, 1) },
        { "sq m,sq ft", (10.7639104, 1)}
    };

    protected string ProcessUnitConversion(string arguments, LzMessageUnits? unitsArg = null)
    {
        var units = unitsArg ?? Units;

        var args = arguments.Split(',');
        if (args.Length != 2)
            throw new Exception($"@Unit() function requires at least two arguments: {arguments}");
        if (args.Length == 2)
            return UnitConversion(units, args[0], args[1]);
        if (args.Length == 3)
            return UnitConversion(units, args[0], args[1], int.Parse(args[2]));
        if (args.Length == 4)
            return UnitConversion(units,args[0], args[1], int.Parse(args[2]), args[3]);
        throw new Exception($"@Unit() too many arguments passed: {arguments}");
    }
    protected string UnitConversion(LzMessageUnits units, string value, string valueUnit, int? precision = null, string? toUnit = null)
    {
        LzMessageUnits valueUnits = LzMessageUnits.Imperial;
        if (imperialUnits.Contains(valueUnit))
            valueUnits = LzMessageUnits.Imperial;
        else if (metricUnits.Contains(valueUnit))
            valueUnits = LzMessageUnits.Metric;
        else
            throw new Exception($"UnitConversion: {valueUnit} is not a recognized unit.");

        if (valueUnits == units)
            return $"{value}{valueUnit}";
        try
        {
            double convertedValue = double.Parse(value);
            string convertedUnit = valueUnit;
            toUnit ??= defaultConversions()[valueUnit];
            var conversionFactor = conversionFactors[$"{valueUnit},{toUnit}"].factor;
            convertedValue *= conversionFactor;
            var strValue = convertedValue.ToString();
            precision ??= conversionFactors[$"{valueUnit},{toUnit}"].precision;
            string formatSpecifier = precision.HasValue ? $"F{precision.Value}" : "G";
            return $"{convertedValue.ToString(formatSpecifier)} {toUnit}";
        }
        catch
        {
            return $"{value} {valueUnit} can't be converted.";
        }
    }
    protected string ProcessUnitS(string arguments, LzMessageUnits? unitsArg = null)
    {
        var units = unitsArg ?? Units;  
        var args = arguments.Split(',');
        if (args.Length != 2)
            return $"UnitS requires two arguments. ";

        return (units == LzMessageUnits.Imperial) ? args[0] : args[1];
    }
}
