namespace LazyStack.ViewModels;

public  class WASMOSAccess : IOSAccess
{
    public WASMOSAccess(HttpClient httpClient) { 
        this.httpClient = httpClient;
    }
    HttpClient httpClient;
    public async Task<string> ContentReadAsync(string url)
    {
        try
        {
            var text = await httpClient.GetStringAsync(url);
            return text;
        } catch (Exception ex) 
        {
            return string.Empty;
        }
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

    public Task<string> LocalReadAsync(string filepath)
    {
        throw new NotImplementedException();
    }

    public Task LocalUpdateAsync(string filepath, string content)
    {
        throw new NotImplementedException();
    }

    public Task LocalDeleteAsync(string filepath)
    {
        throw new NotImplementedException();
    }

    public Task S3CreateAsync(string path, string content)
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

    public Task S3DeleteAsync(string path)
    {
        throw new NotImplementedException();
    }


}
