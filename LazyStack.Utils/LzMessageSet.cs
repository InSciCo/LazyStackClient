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

    public override bool Equals(object? obj)
    {
		if (obj == null)
			return false;
		if (obj is not LzMessageSet)
			return false;
		var other = obj as LzMessageSet;
		return Culture == other.Culture && Units == other.Units;
	}

    public override int GetHashCode()
    { 
        unchecked // Overflow is fine, just wrap
        {
			int hash = 17;
			hash = hash * 23 + Culture.GetHashCode();
			hash = hash * 23 + Units.GetHashCode();
			return hash;
		}   
    }
}
