namespace LazyStack.Client.Base;

public interface ILzHost
{
    string Url { get; set; } 
    string AssetsUrl { get; set; } 
    string WsUrl { get; set;    }
    bool IsMAUI { get; set; }
    bool IsWASM { get; }
    bool IsAndroid { get; set; }    
    bool IsLocal { get; set;  }   
}

public class LzHost : ILzHost
{
    public LzHost(string? url = null, string? tenancyUrl = null, string? wsUrl = null, bool isMAUI = true, bool isAndroid = false, bool isLocal = false)
    {
        Url = url ?? "";
        AssetsUrl = tenancyUrl ?? "";
        WsUrl = wsUrl ?? "";
        IsMAUI = isMAUI;
        IsAndroid = isAndroid;
        IsLocal = isLocal;
    }

    public string Url { get; set; } = string.Empty;
    public string AssetsUrl { get; set; } = string.Empty;
    public string WsUrl { get; set; } = string.Empty;
    public bool IsMAUI { get; set; }
    public bool IsWASM => !IsMAUI;
    public bool IsAndroid { get; set; }
    public bool IsLocal { get; set; }   

}
