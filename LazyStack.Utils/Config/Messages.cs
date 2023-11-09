namespace LazyStack.Utils;
using LazyStack.Base;

public enum MessageUnits { I, M }   

public interface IMessages
{
    public void ReplaceVars();
    public void SetOSAccess(IOSAccess oSAccess);
    public Task SetLanguageAsync(string language, MessageUnits units);  
    public void AddInternalMsg(string key, string msg);
    public List<string> MessageFiles { get; set; }
    public string Msg(string key);
    public bool TryGetMsg(string key, out string msg);
    public void MergeJson(string messageJson);
    public string CurrentLanguage { get; } 
}
/// <summary>
/// Messages provide a way to localize text in a Blazor app.
/// In addition to localization, messages can be tailored to a tenancy. 
/// Messages are stored in JSON object format. { key: value}.
/// 
/// Messages can be loaded from embedded assembly resources or from external files.
/// Internal resources are loaded in each assembly. By convention, LazyStack has a 
/// Config folder in projects that register messages. Internal message files are 
/// mono-language. An example: "Config/Messages.json".
/// 
/// External message resources are language specific with a sufix determining the 
/// language of the messages. ex: "_content/{assembly}/Messages.en-US.json".
/// External message resources are loaded using IOSAccess.ContentReadAsync. 
/// 
/// To load external message resources:
/// 1. Set the MessageFiles property to the list of message files. ex:
///     Messages.MessageFiles = new List<string> { "_content/MyApp/messages.en-US.json", "_content/MyApp/inventory.en-US.json };
/// 2. Call SetOSAccess() with an IOSAccess object. 
/// 3. Call SetLanguageAsync("en-US") with the language to load and make current.
/// 
/// To retrieve messages call Msg(key).
/// If a key is not found in the current language, the key is searched for in the
/// internal messages. If the key is not found in the internal messages, the key
/// is returned.
/// 
/// Overrides/Tenancy:
/// When multiple message files are loaded for a language, the keys in the last 
/// loaded file override keys in previously loaded files. This allows for 
/// customization by tenancy.
/// 
/// </summary>
public class Messages : IMessages
{
    const string keyPattern = "__.*__";
    public Messages()
    {
        // Internal messages are those loaded from embedded resources
        Languages.Add("internal", new Dictionary<string, string>());   
    }
    public void AddInternalMsg(string key, string msg)
    {
        Languages["internal"][key] = msg;
    }
    public void ReplaceVars()
    {
        for (var i = 0; i < Msgs.Count; i++)
        {
            // TODO: optimize
            // replace variables in the form __.*__ with the value of the key
            var msg = Msgs.ElementAt(i).Value;
            var key = Msgs.ElementAt(i).Key;
            MatchCollection matches;
            while ((matches = Regex.Matches(msg, keyPattern)).Count > 0)
                foreach (Match match in matches)
                    if (TryGetMsg(match.Value[2..^2], out string? replacement))
                        msg = msg.Replace(match.Value, replacement);
                    else
                        throw new Exception($"Msgs[{match.Value[2..^2]}] not found.");
            Msgs[key] = msg;
            // Process @Unit() functions 
            while ((matches = Regex.Matches(msg, "@Unit\\((.*?)\\)")).Count > 0)
                foreach (Match match in matches)
                    msg = msg.Replace(match.Value, ProcessUnitConversion(match.Value[6..^2]));
            // Process @UnitS() functions 
            while ((matches = Regex.Matches(msg, "@UnitS\\((.*?)\\)")).Count > 0)
                foreach (Match match in matches)
                    msg = msg.Replace(match.Value, ProcessUnitS(match.Value[7..^2]));

        }
    }
    public string CurrentLanguage { get; private set; } = "internal";  
    protected Dictionary<string, string> Msgs => Languages[CurrentLanguage];
    public Dictionary<string, Dictionary<string,string>> Languages { get; set; } = new();
    public List<string> MessageFiles { get; set; } = new();
    protected IOSAccess? _oSAccess;
    public void SetOSAccess (IOSAccess oSAccess)
    {
        _oSAccess = oSAccess;
    }
    public async Task SetLanguageAsync(string language, MessageUnits units)
    {
        if (_oSAccess == null)
            throw new Exception("SetOSAccess must be called before SetLanguageAsync.");
        var languageAndUnits = language + "-" + units;
        CurrentLanguage = languageAndUnits;
        if (Languages.ContainsKey(languageAndUnits))
            return;
        var languageMessages = new Dictionary<string, string>();
        Languages.Add(languageAndUnits, languageMessages);
        foreach (var msgFile in MessageFiles)
        {
            // msgFile example: "messages.en-US.json"
            var filePath = msgFile.Replace(".json", $".{language}.json");    
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

        // Try and get the message from the current language messages
        if (Msgs.TryGetValue(key, out string? value))
            msg = string.IsNullOrEmpty(value) ? key : value;

        // Try and get the message from the internal messages
        if (Languages["internal"].TryGetValue(key, out string? internalValue))
            msg = string.IsNullOrEmpty(value) ? key : internalValue;

        return key.Equals(msg);
    }
    public string Msg(string key)
    {
        if (key == null)
            return "";

        if (key == "Nothing")
            return "";

        var msg = "";
        // Try and get the message from the current language    
        if (Msgs.TryGetValue(key, out string? value))
            msg = string.IsNullOrEmpty(value) ? key : value;

        // Try and get the message from the internal messages
        if (msg == "" && Languages["internal"].TryGetValue(key, out string? internalValue))
            msg = string.IsNullOrEmpty(value) ? key : internalValue;
        
        // Perform valueUnit conversion and replacement for each @Unit() in the msg 
        MatchCollection matches;
        while ((matches = Regex.Matches(msg, "@Unit\\((.*?)\\)")).Count > 0)
            foreach (Match match in matches)
                msg = msg.Replace(match.Value, UnitConversion(match.Value[6..^2], match.Value[6..^2]));

        return msg == "" ? key : msg;
    }
    public void MergeJson(string messagesJson)
    {
        if (string.IsNullOrEmpty(messagesJson))
            return;
        var newMsgs = JsonConvert.DeserializeObject<Dictionary<string,string>>(messagesJson);
        if(newMsgs != null) 
            foreach (var msg in newMsgs)
                Msgs[msg.Key] = msg.Value;
    }
    public MessageUnits CurrentUnits { get; set; } = MessageUnits.I;  

    static string[] imperialUnits = { "in", "ft", "yd", "mi", "oz", "lb", "sq in", "sq ft" };
    static string[] metricUnits = { "mm", "cm", "m", "km", "g", "kg", "sq mm", "sq cm", "sq m" };
    static Dictionary<string, string> defaultConversions() => new()
    {
        { "in", "mm" },
        { "ft", "m" },
        { "yd", "m" },
        { "mi", "km" },
        { "oz", "g" },
        { "lb", "kg" },
        { "sq in", "sq cm" },
        { "sq ft", "sq m"},
        { "mm", "in" },
        { "cm", "in" },
        { "m", "ft" },
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
        { "ft,m",  (0.3048, 1)},
        { "yd,m", (0.9144,1) },
        { "mi,km", (1.609344, 2) },
        { "oz,g", (28.349523125,0) },
        { "lb,kg", (0.45359237, 1) },
        { "sq in,sq cm", (6.4516, 0) },
        { "sq ft,sq m", (0.09290304, 1)},
        { "mm,in", (0.0393700787, 2) },
        { "cm,in", (0.393700787, 2) },
        { "m,ft", (3.2808399, 1) },
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
        MessageUnits valueUnits = MessageUnits.I; 
        if(imperialUnits.Contains(valueUnit))
            valueUnits = MessageUnits.I;
        else if(metricUnits.Contains(valueUnit))
            valueUnits = MessageUnits.M;
        else
            throw new Exception($"UnitConversion: {valueUnit} is not a recognized unit.");

        if(valueUnits == CurrentUnits)
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

    public static int CountDigitsAfterDecimal(string number)
    {
        // Check if the number contains a decimal point.
        int decimalIndex = number.IndexOf('.');
        if (decimalIndex == -1)
        {
            // No decimal point found, so return 0.
            return 0;
        }

        // Count the number of digits after the decimal point.
        // We subtract 1 to account for the decimal point itself.
        return number.Length - decimalIndex - 1;
    }
    protected string ProcessUnitS(string arguments)
    {
        var args = arguments.Split(',');
        if (args.Length != 2)
            return $"UnitS requires two arguments. ";

        if(CurrentUnits == MessageUnits.I)
            return args[0];
        else
            return args[1];
    }

}
