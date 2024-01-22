namespace LazyStack.Base;

public class LzService
{
    public string Auth { get; set; } = string.Empty;
    public Dictionary<string, JObject> Resources { get; set; } = new();
}
