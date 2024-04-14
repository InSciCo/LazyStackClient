namespace LazyStack.Client.Base;

public interface ILzHost
{
    string Url { get; set; }
    bool IsMAUI { get; set; }
    bool IsWASM { get; }
    bool IsAndroid { get; set; }    
    bool IsLocal { get; set;  }   
}

public class LzHost : ILzHost
{
    public LzHost(string? url = null, bool isMAUI = true, bool isAndroid = false, bool isLocal = false)
    {
        Url = url ?? "";
        IsMAUI = isMAUI;
        IsAndroid = isAndroid;
        IsLocal = isLocal;
    }

    public string Url { get; set; } = string.Empty;
    public bool IsMAUI { get; set; }
    public bool IsWASM => !IsMAUI;
    public bool IsAndroid { get; set; }
    public bool IsLocal { get; set; }   

}
