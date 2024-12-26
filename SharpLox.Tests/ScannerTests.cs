namespace SharpLox.Tests;

public class ScannerTests
{
    private static readonly Dictionary<string, TokenType> SingleCharTokens = new()
    {
        ["("] = TokenType.LeftParen,
        [")"] = TokenType.RightParen,
        ["{"] = TokenType.LeftBrace,
        ["}"] = TokenType.RightBrace,
        [","] = TokenType.Comma,
        ["."] = TokenType.Dot,
        ["-"] = TokenType.Minus,
        ["+"] = TokenType.Plus,
        [";"] = TokenType.Semicolon,
        ["*"] = TokenType.Star,
        ["/"] = TokenType.Slash
    };

    private static readonly Dictionary<string, TokenType> OneOrTwoCharTokens = new()
    {
        ["!"] = TokenType.Bang,
        ["!="] = TokenType.BangEqual,
        ["="] = TokenType.Equal,
        ["=="] = TokenType.EqualEqual,
        [">"] = TokenType.Greater,
        [">="] = TokenType.GreaterEqual,
        ["<"] = TokenType.Less,
        ["<="] = TokenType.LessEqual
    };

    public static IEnumerable<object[]> InvalidCharData()
    {
        yield return ["@"];
        yield return ["#"];
        yield return ["$"];
        yield return ["^"];
        yield return ["`"];
        yield return ["\\"];
        yield return ["~"];
        yield return ["|"];
    }
    
    public static IEnumerable<object[]> SingleCharTokensData()
    {
        foreach (var (source, tokenType) in SingleCharTokens)
        {
            yield return [source, tokenType];
        }
    }

    public static IEnumerable<object[]> OneOrTwoCharTokensData()
    {
        foreach (var (source, tokenType) in OneOrTwoCharTokens)
        {
            yield return [source, tokenType];
        }
    }
    
    [Theory]
    [MemberData(nameof(SingleCharTokensData))]
    public void Scanner_SingleCharToken_ReturnsCorrectType(string source, TokenType expectedType)
    {
        // Arrange
        var scanner = new Scanner(source);
        
        // Act
        var tokens = scanner.ScanTokens();
        
        // Assert
        Assert.Equal(2, tokens.Count);
        Assert.Equal(expectedType, tokens[0].Type);
        Assert.Equal(TokenType.Eof, tokens[1].Type);
    }

    [Theory]
    [MemberData(nameof(OneOrTwoCharTokensData))]
    public void Scanner_OneOrTwoCharToken_ReturnsCorrectType(string source, TokenType expectedType)
    {
        var scanner = new Scanner(source);
        
        var tokens = scanner.ScanTokens();
        
        Assert.Equal(2, tokens.Count);
        Assert.Equal(expectedType, tokens[0].Type);
        Assert.Equal(TokenType.Eof, tokens[1].Type);
    }

    [Theory]
    [MemberData(nameof(InvalidCharData))]
    public void Scanner_OneOrTwoCharToken_ThrowsError(string source)
    {
        var scanner = new Scanner(source);
        
        var tokens = scanner.ScanTokens();

        Assert.Single(tokens);
        Assert.Equal(TokenType.Eof, tokens[0].Type);
    }
}