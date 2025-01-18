namespace SharpLox.Extensions;

public static class CharExtensions
{
    public static bool IsLoxDigit(this char c) => c is >= '0' and <= '9';
    public static bool IsLoxAlpha(this char c) => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_';
    public static bool IsLoxAlphaNumeric(this char c) => c.IsLoxAlpha() || c.IsLoxDigit();
}