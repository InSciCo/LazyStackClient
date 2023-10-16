namespace LazyStack.CognitoValidation
{
    public interface ITokenValidator
    {
        Task<bool> ValidateTokenHttpAsync(string? token);
    }
}