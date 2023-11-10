namespace LazyStack.Utils;

public class LzMessageSet
{
    public LzMessageSet(string culture, LzMessageUnits units)
    {
        Culture = culture;
        Units = units;
    }   
    public string Culture { get; set;  } = "en-US";
    public LzMessageUnits Units { get; set; } = LzMessageUnits.Imperial;
}
