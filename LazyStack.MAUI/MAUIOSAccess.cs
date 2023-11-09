namespace LazyStack.MAUI;
public class MAUIOSAccess : IOSAccess
{
    public MAUIOSAccess(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }
    HttpClient httpClient;
    public async Task<string> ContentReadAsync(string path)
    {
        path = path.Split('?').First(); // Get rid of any parameters in path
        using Stream stream = await FileSystem.Current.OpenAppPackageFileAsync(path);
        using StreamReader reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();
        return text;
    }
    public async Task<string> HttpReadAsync(string url)
    {
        try
        {
            var text = await httpClient.GetStringAsync(url);
            return text;
        }
        catch (Exception ex)
        {
            return string.Empty;
        }
    }

    public Task LocalCreateAsync(string filepath, string content)
    {
        throw new NotImplementedException();
    }

    public Task LocalDeleteAsync(string filepath)
    {
        throw new NotImplementedException();
    }

    public Task<string> LocalReadAsync(string filepath)
    {
        throw new NotImplementedException();
    }

    public Task LocalUpdateAsync(string filepath, string content)
    {
        throw new NotImplementedException();
    }

    public Task S3CreateAsync(string path, string content)
    {
        throw new NotImplementedException();
    }

    public Task S3DeleteAsync(string path)
    {
        throw new NotImplementedException();
    }

    public Task<string> S3ReadAsync(string path)
    {
        throw new NotImplementedException();
    }

    public Task S3UpdateAsync(string path, string content)
    {
        throw new NotImplementedException();
    }


}