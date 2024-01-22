namespace LazyStack.Blazor;

public class BlazorLzClientConfig : LzClientConfig
{

    public BlazorLzClientConfig(IOSAccess oSAccess)
    {
        _oSAccess = oSAccess;
    }   

    private IOSAccess _oSAccess;

    public override async Task ReadConfigAsync(string configFilePath)
    {
        try
        {
            if (string.IsNullOrEmpty(configFilePath))
                return;

            if (_oSAccess == null)
                throw new Exception("OSAccess not set.");

            var json = await _oSAccess!.ReadConfigAsync(configFilePath);
            JsonConvert.PopulateObject(json, this);
           
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error merging config: {ex.Message}");
        }
    }


}



