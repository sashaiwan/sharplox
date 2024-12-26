namespace SharpLox;

public sealed class Scanner(string source)
{
    private string Source { get; } = source;

    private Dictionary<string, TokenType> Keywords { get; } = new()
    {
        { "and", TokenType.And },
        { "class", TokenType.Class},
        { "else", TokenType.Else},
        { "false", TokenType.False },
        { "for", TokenType.For },
        { "fun", TokenType.Fun },
        { "if", TokenType.If },
        { "nil", TokenType.Nil },
        { "or", TokenType.Or },
        // Move print to the standard lib
        { "print", TokenType.Print },
        { "return", TokenType.Return },
        { "super", TokenType.Super },
        { "this", TokenType.This },
        { "true", TokenType.True },
        { "var", TokenType.Var },
        { "while", TokenType.While },
        { "break", TokenType.Break }
    };
    private List<Token> Tokens { get; } = [];
    private int _start;
    private int _current;
    private int _line = 1;

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd())
        {
            // We are at the beginning of the next lexeme.
            _start = _current;
            ScanToken();
        }
        
        Tokens.Add(new Token(TokenType.Eof, "", null, _line));
        return Tokens;
    }

    private void ScanToken()
    {
        var c = Advance();
        switch (c)
        {
            case '(':
                AddToken(TokenType.LeftParen);
                break;
            case ')':
                AddToken(TokenType.RightParen);
                break;
            case '{':
                AddToken(TokenType.LeftBrace);
                break;
            case '}':
                AddToken(TokenType.RightBrace);
                break;
            case ',':
                AddToken(TokenType.Comma);
                break;
            case '.':
                AddToken(TokenType.Dot);
                break;
            case '-':
                AddToken(TokenType.Minus);
                break;
            case '+':
                AddToken(TokenType.Plus);
                break;
            case ';':
                AddToken(TokenType.Semicolon);
                break;
            case '*':
                AddToken(TokenType.Star);
                break;
            case '!':
                AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
                break;
            case '=':
                AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
                break;
            case '<':
                AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                break;
            case '>':
                AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
                break;
            case '?':
                AddToken(TokenType.QuestionMark);
                break;
            case ':':
                AddToken(TokenType.Colon);
                break;
            case '/':
                if (Match('/'))
                    while (Peek() != '\n' && !IsAtEnd()) Advance();
                else if (Match('*'))
                    ProcessMultilineComments();
                else
                    AddToken(TokenType.Slash);
                break;
            // Ignore whitespaces.
            case ' ' or '\r' or '\t':
                break;
            case '\n':
                _line++; 
                break;
            case '"': 
                ProcessString(); 
                break;
            default:
                if (c.IsLoxDigit())
                {
                    ProcessNumber();
                }
                else if (c.IsLoxAlpha())
                {
                    ProcessIdentifier();
                }
                else
                {
                    Program.Error(_line, $"Unexpected character '{c}'");
                }
                break;
        }
    }

    private void ProcessMultilineComments()
    {
        while (!IsAtEnd())
        {
            if (Peek() == '\n')
                _line++;
            else if (Peek() == '/' && PeekNext() == '*')
            {
                Advance(); // consume /
                Advance(); // consume *
                ProcessMultilineComments();
                continue;
            }
            else if (Peek() == '*' && PeekNext() == '/')
            {
                Advance(); // consume *
                Advance(); // consume /
                return;
            }

            Advance();
        }

        if (IsAtEnd())
        {
            Program.Error(_line, $"Unexpected multiline comment");
        }
    }

    private void ProcessIdentifier()
    {
        while(Peek().IsLoxAlphaNumeric()) Advance();
        
        var value = Source.Substring(_start, _current - _start);
        var tokenType = Keywords.GetValueOrDefault(value, TokenType.Identifier);
        AddToken(tokenType);
    }
    private void ProcessNumber()
    {
        while (Peek().IsLoxDigit()) Advance();

        if (Peek() == '.' && PeekNext().IsLoxDigit())
        {
            Advance();
            
            while(Peek().IsLoxDigit()) Advance();
        }
        
        var value = double.Parse(Source.Substring(_start, _current - _start));
        AddToken(TokenType.Number, value);
    }
    private void ProcessString()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n')
                _line++;
            Advance();
        }

        if (IsAtEnd())
        {
            Program.Error(_line, "Unterminated string.");
            return;
        }
        
        Advance();
        var value = Source[(_start + 1)..(_current - 1)];
        AddToken(TokenType.String, value);
    }
    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (Source[_current] != expected) return false;

        _current++;
        return true;
    }
    private char Peek() => IsAtEnd() ? '\0' : Source[_current];
    private char PeekNext() => _current + 1 >= Source.Length ? '\0' : Source[_current + 1];
    private bool IsAtEnd() => _current >= Source.Length;
    private char Advance() => Source[_current++];
    private void AddToken(TokenType tokenType, object? literal = null)
    {
        var text = Source[_start.._current];
        Tokens.Add(new Token(tokenType, text, literal, _line));
    }
}