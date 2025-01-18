namespace SharpLox;

public sealed record Token(TokenType Type, string Lexeme, object? Literal, int Line)
{
    public override string ToString() => $"{Type}: {Lexeme} ({Line}) {Literal}";
};