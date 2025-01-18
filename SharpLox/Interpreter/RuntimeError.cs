namespace SharpLox;

public class RuntimeError(Token token, string message) : Exception(message)
{
    public Token Token { get; } = token;
}

public class BreakException : Exception;

public class ReturnException(object? value) : Exception
{
    public object? Value { get; } = value;
};