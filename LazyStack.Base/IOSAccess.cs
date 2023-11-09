namespace LazyStack.Base;

public interface IOSAccess
{
    /// <summary>
    /// Read json file from _content folder.
    /// WASM project implements this using
    ///     Use HttpClient
    /// MAUI project implements this using
    ///     FileSystem.OpenAppPackageFileAsync(String)
    /// if in MAUI. 
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public Task<string> ContentReadAsync(string url);
    public Task LocalCreateAsync(string filepath, string content);
    public Task<string> LocalReadAsync(string filepath); 
    public Task LocalUpdateAsync(string filepath, string content);
    public Task LocalDeleteAsync(string filepath);
    public Task S3CreateAsync(string path, string content);
    public Task<string> S3ReadAsync(string path);
    public Task S3UpdateAsync(string path, string content);
    public Task S3DeleteAsync(string path);
    public Task<string> HttpReadAsync(string path);


}
