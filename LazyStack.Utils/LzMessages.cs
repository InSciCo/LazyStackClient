namespace LazyStack.Utils;
using LazyStack.Base;
using System.Text.RegularExpressions;

public enum LzMessageUnits { Imperial, Metric }   

public interface ILzMessages
{
    public void ReplaceVars();
    public void SetOSAccess(IOSAccess oSAccess);
    public Task SetMessageSetAsync(LzMessageSet messageSet);  
    public Task SetMessageSetAsync(string culture, LzMessageUnits units);
    public void AddInternalMsg(string key, string msg);
    public List<string> MessageFiles { get; set; }
    public string Msg(string key);
    public bool TryGetMsg(string key, out string msg);
    public void MergeJson(string messageJson);
    public string Culture => MessageSet.Culture;
    public LzMessageUnits Units => MessageSet.Units;
    public List<LzMessageSet> MessageSets { get; }   
    public LzMessageSet MessageSet { get; }

}
/// <summary>
/// LzMessages provide a way to localize text in a Blazor app.
/// In addition to localization, messages can be tailored to a tenancy. 
/// LzMessages are stored in JSON object format. { key: value}.
/// 
/// LzMessages can be loaded from embedded assembly resources or from external files.
/// Internal resources are loaded in each assembly. By convention, LazyStack has a 
/// Config folder in projects that register messages. Internal message files are 
/// mono-culture. An example: "Config/LzMessages.json".
/// 
/// External message resources are culture specific with a sufix determining the 
/// culture of the messages. ex: "_content/{assembly}/LzMessages.en-US.json".
/// External message resources are loaded using IOSAccess.ContentReadAsync so 
/// you need to call SetOSAccess before loading external message resources. 
/// 
/// To load external message resources:
/// 1. Set the MessageFiles property to the list of message files. ex:
///     LzMessages.MessageFiles = new List<string> { 
///     "_content/MyApp/data/messages.en-US.json", 
///     "_content/MyApp/data/inventory.en-US.json,
///     "_content/MyApp/tenant/messages.en-US.json", // tenant messages override data messages
///     "_content/MyApp/tenant/inventory.en-US.json // tenant inventory override data inventory
///     };
/// 2. Call SetOSAccess() with an IOSAccess object. 
/// 3. Call SetMessageSetAsync("en-US") with the culture to load and make current.
/// 
/// To retrieve messages call Msg(key).
/// If a key is not found in the current culture, the key is searched for in the
/// internal messages. If the key is not found in the internal messages, the key
/// is returned.
/// 
/// A MessageSet is a combination of a culture and a unit of measure. The LzMessageSet 
/// class is used to identify a MessageSet. The default Equals and GetHashCode methods 
/// are ovdrridden so that MessageSet can be used as a key in a dictionary easily.
/// 
/// Overrides/Tenancy:
/// When multiple message files are loaded for a culture, the keys in the last 
/// loaded file override keys in previously loaded files. This allows for 
/// customization by tenancy.
/// 
/// </summary>
public class LzMessages : ILzMessages
{
    const string keyPattern = "__.*__";
    public LzMessages()
    {
        // Add the internal message set so config methods can 
        // add messages to it from embedded assembly resources. 
        MessageSet = new LzMessageSet("internal", LzMessageUnits.Imperial);
        _internalMsgs = _msgs = new Dictionary<string, string>();
        // Internal messages are those loaded from embedded resources
        _messageSetData.Add(MessageSet, _internalMsgs);
    }

    protected Dictionary<string, string> _msgs;
    protected Dictionary<string, string> _internalMsgs;
    protected Dictionary<LzMessageSet, Dictionary<string, string>> _messageSetData = new();
    public LzMessageSet MessageSet { get; set; }
    public List<LzMessageSet> MessageSets { get; } = new List<LzMessageSet>();
    public List<string> MessageFiles { get; set; } = new();
    protected IOSAccess? _oSAccess;
	public LzMessageUnits Units => MessageSet.Units;

	public LzMessageSet GetMessageSet(string culture, LzMessageUnits units)
    {
        var messageSet = MessageSets.FirstOrDefault(ms => ms.Culture == culture && ms.Units == units)
            ?? throw new Exception($"MessageSet not found for culture: {culture} and units: {units}");
        return messageSet;
    }
    public bool TryGetMessageSet(string culture, LzMessageUnits units, out LzMessageSet? messageSet)
    {
        messageSet = MessageSets.FirstOrDefault(ms => ms.Culture == culture && ms.Units == units);
        return messageSet != null;  
    }

    public void AddInternalMsg(string key, string msg)
    {
        _internalMsgs[key] = ReplaceUnits(msg);
    }

    public string ReplaceUnits(string msg)
    {
        if (!msg.Contains("@Unit")) // typically, most messages don't have units, this is a quick check to see if we need to do anything
            return msg;
        var msgIn = msg;    
        MatchCollection matches;
        // Process @Unit() functions 
        while ((matches = Regex.Matches(msg, "@Unit\\((.*?)\\)")).Count > 0)
            foreach (Match match in matches)
            {
                var val = match.Value[6..^1];
                msg = msg.Replace(match.Value, ProcessUnitConversion(match.Value[6..^1]));
            }
        // Process @UnitS() functions 
        while ((matches = Regex.Matches(msg, "@UnitS\\((.*?)\\)")).Count > 0)
            foreach (Match match in matches)
                msg = msg.Replace(match.Value, ProcessUnitS(match.Value[7..^1]));
        return msg;
    }
    public void ReplaceVars()
    {
        for (var i = 0; i < _msgs.Count; i++)
        {
            // TODO: optimize
            // replace variables in the form __.*__ with the value of the key
            var msg = _msgs.ElementAt(i).Value;
            var key = _msgs.ElementAt(i).Key;
            MatchCollection matches;
            while ((matches = Regex.Matches(msg, keyPattern)).Count > 0)
                foreach (Match match in matches)
                    if (TryGetMsg(match.Value[2..^2], out string? replacement))
                        msg = msg.Replace(match.Value, replacement);
                    else
                        throw new Exception($"Msgs[{match.Value[2..^2]}] not found.");
            _msgs[key] = ReplaceUnits(msg);
        }
    }
    public void SetOSAccess(IOSAccess oSAccess)
    {
        _oSAccess = oSAccess;
    }
    public Task SetMessageSetAsync(string culture, LzMessageUnits units)
    {
        if (TryGetMessageSet(culture, units, out LzMessageSet? messageSet))
            return SetMessageSetAsync(messageSet!);
        else
            return SetMessageSetAsync(new LzMessageSet(culture, units));
    }
    public async Task SetMessageSetAsync(LzMessageSet messageSet)
    {
        if (_oSAccess == null)
            throw new Exception("SetOSAccess must be called before SetMessageSetAsync.");
        Console.WriteLine($"SetMessageSetAsync: {messageSet.Culture} {messageSet.Units}");
        MessageSet = messageSet;
        if (_messageSetData.ContainsKey(messageSet))
        {
            _msgs = _messageSetData[messageSet];
            return;
        }
        _msgs = new Dictionary<string, string>();
        _messageSetData.Add(messageSet, _msgs);
        if(MessageFiles is not null)
            foreach (var msgFile in MessageFiles)
            {
                // msgFile example: "messages.en-US.json"
                var filePath = msgFile.Replace(".json", $".{messageSet.Culture}.json");
                var json = await _oSAccess.ContentReadAsync(filePath);
                MergeJson(json);
            }
        ReplaceVars();
        return;
    }
    public bool TryGetMsg(string key, out string msg)
    {
        msg = key;
        if (key == null)
            return false;

        if (key == "Nothing")
            return false;
        var msgs = _messageSetData[MessageSet]; 
        // Try and get the message from the current culture messages
        if (msgs.TryGetValue(key, out string? value))
            msg = string.IsNullOrEmpty(value) ? key : value;
        else
        // Try and get the message from the internal messages
        if (_internalMsgs.TryGetValue(key, out string? internalValue))
            msg = string.IsNullOrEmpty(internalValue) ? key : internalValue;
        return !key.Equals(msg);
    }
    public string Msg(string key)
        {
        if(TryGetMsg(key, out string msg))
            return msg;
        else
            return key; 
        }

    public void MergeJson(string messagesJson)
    {
        if (string.IsNullOrEmpty(messagesJson))
            return;
        var newMsgs = JsonConvert.DeserializeObject<Dictionary<string,string>>(messagesJson);
        if(newMsgs != null) 
            foreach (var msg in newMsgs)
                _msgs[msg.Key] = msg.Value;
    }

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

    protected string ProcessUnitConversion(string arguments)
    {
        var args = arguments.Split(',');
        if (args.Length != 2)
            throw new Exception($"@Unit() function requires at least two arguments: {arguments}");   
        if(args.Length == 2)
            return UnitConversion(args[0], args[1]);   
        if(args.Length == 3)
            return UnitConversion(args[0], args[1], int.Parse(args[2]));
        if(args.Length == 4)
            return UnitConversion(args[0], args[1], int.Parse(args[2]), args[3]);  
        throw new Exception($"@Unit() too many arguments passed: {arguments}");
    }
    protected string UnitConversion(string value, string valueUnit, int? precision = null, string? toUnit = null )
    {
        LzMessageUnits valueUnits = LzMessageUnits.Imperial; 
        if(imperialUnits.Contains(valueUnit))
            valueUnits = LzMessageUnits.Imperial;
        else if(metricUnits.Contains(valueUnit))
            valueUnits = LzMessageUnits.Metric;
        else
            throw new Exception($"UnitConversion: {valueUnit} is not a recognized unit.");

        if(valueUnits == Units)
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
    protected string ProcessUnitS(string arguments)
    {
        var args = arguments.Split(',');
        if (args.Length != 2)
            return $"UnitS requires two arguments. ";

        if(Units == LzMessageUnits.Imperial)
            return args[0];
        else
            return args[1];
    }

}
