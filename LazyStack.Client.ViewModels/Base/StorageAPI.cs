namespace LazyStack.Client.ViewModels;

public enum StorageAPI
{
    Default, // Usually DTO API
    DTO, // Calls manipulating Data Transfer Objects. Primarily used for database access.
    Model, // Calls manipulating Model objects. Primarily used for updating in memory objects.
    S3, // bucket access, generally requires auth
    Http, // limited to gets
    Local, // local device storage
    Content, // _content access
    None // no storage calls. Use for data that is updated in place.
}
