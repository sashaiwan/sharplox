namespace SharpLox;

internal class ParserError : Exception;

public enum FunctionType
{
    Function,
    Method
}

public sealed class Parser(List<Token> tokens)
{
    
    private int _current;

    private int _loopDepth = 0;
    // Allowed tokens for creating error productions
    // ( "+" | "-" | "*" | "/" | ">" | ">=" | "<" | "<=" | "!=" )
    private readonly TokenType[] _incompleteOperators =
    [
        TokenType.Plus, TokenType.Minus, TokenType.Star, TokenType.Slash,
        TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual,
        TokenType.EqualEqual, TokenType.BangEqual
    ];
        
    // TODO: check nullability of statements list
    public List<Stmt?> Parse()
    {
        List<Stmt?> statements = [];
        while (!IsAtEnd())
            statements.Add(Declaration());

        return statements;
    }

    private Stmt? Declaration()
    {
        try
        {
            if (Match(TokenType.Class)) return ClassDeclaration();
            if (Check(TokenType.Fun) && CheckNext(TokenType.Identifier))
            {
                Consume(TokenType.Fun, string.Empty);
                return Function(FunctionType.Function);
            }
            if (Match(TokenType.Var))
                return VarDeclaration();
            return Statement();
        }
        catch (ParserError)
        {
            Synchronize();
            return null;
        }
    }

    private Class ClassDeclaration()
    {
        var nameToken = Consume(TokenType.Identifier, "Expect class name.");
        Consume(TokenType.LeftBrace, "Expect '{' before class body.");

        List<Function> methods = [];
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            methods.Add(Function(FunctionType.Method));            
        }
        
        Consume(TokenType.RightBrace, "Expect '}' after class body.");
        return new Class(nameToken, methods);
    }
    
    private Function Function(FunctionType type)
    {
        // TODO: find a better way
        var kind = type == FunctionType.Function ? "function" : "method";
        var nameToken = Consume(TokenType.Identifier, $"Expect {kind} name.");
        return new Function(nameToken, FunctionBody(kind));
    }

    private Lambda FunctionBody(string kind)
    {
        Consume(TokenType.LeftParen, $"Expect '(' after {kind}.");
        List<Token> parameters = [];
        if (!Check(TokenType.RightParen))
        {
            do
            {
                if (parameters.Count >= 255)
                    Error(Peek(), $"Can't have more than 255 parameters.");

                parameters.Add(Consume(TokenType.Identifier, "Expect parameter name."));
            } while (Match(TokenType.Comma));
        }
        Consume(TokenType.RightParen, $"Expect ')' after {kind} parameters.");

        Consume(TokenType.LeftBrace, $"Expect '{{' before {kind} body.");
        var body = Block();
    
        return new Lambda(parameters, body);
    }
    
    private Stmt VarDeclaration()
    {
        var nameToken = Consume(TokenType.Identifier, "Expect variable name.");

        Expr? initializerExpr = null;
        if (Match(TokenType.Equal))
            initializerExpr = Expression();
        
        Consume(TokenType.Semicolon, "Expect ';' after variable declaration.");
        return new Var(nameToken, initializerExpr);
    }
    
    private Stmt Statement()
    {
        if (Match(TokenType.If)) return IfStatement();
        if (Match(TokenType.Print)) return PrintStatement();
        if (Match(TokenType.Return)) return ReturnStatement();
        if (Match(TokenType.For)) return ForStatement();
        if (Match(TokenType.While)) return WhileStatement();
        if (Match(TokenType.LeftBrace)) return new Block(Block());
        if (Match(TokenType.Break)) return Break();
        
        return ExpressionStatement();
    }

    private Return ReturnStatement()
    {
        var keywordToken = Previous();
        Expr? value = null;
        if (!Check(TokenType.Semicolon))
            value = Expression();
        
        Consume(TokenType.Semicolon, "Expect ';' after return value.");
        return new Return(keywordToken, value);
    }
    
    private Break Break()
    {
        if (_loopDepth <= 0) Error(Previous(), "Cannot use 'break' keyword outside of loop.");

        Consume(TokenType.Semicolon, "Expect ';' after 'break' keyword.");
        return new Break(Previous());
    }
    
    private Stmt ForStatement()
    {
        _loopDepth++;
        Consume(TokenType.LeftParen, "Expect '(' after 'for'.");
        
        Stmt? initializerStmt;
        if (Match(TokenType.Semicolon))
            initializerStmt = null;
        else if (Match(TokenType.Var))
            initializerStmt = VarDeclaration();
        else
            initializerStmt = ExpressionStatement();
        
        Expr? conditionExpr = null;
        if (!Check(TokenType.Semicolon))
            conditionExpr = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after loop condition.");
        
        Expr? increment = null;
        if (!Check(TokenType.RightParen))
            increment = Expression();
        Consume(TokenType.RightParen, "Expect ')' after 'for' clauses.");

        var bodyStmt = Statement();

        if (increment != null)
            bodyStmt = new Block([bodyStmt, new Expression(increment)]);

        conditionExpr ??= new Literal(true);
        
        bodyStmt = new While(conditionExpr, bodyStmt);

        if (initializerStmt != null)
            bodyStmt = new Block([initializerStmt, bodyStmt]);

        _loopDepth--;
        return new Block([bodyStmt]);
    }
    private While WhileStatement()
    {
        _loopDepth++;
        Consume(TokenType.LeftParen, "Expect '(' after 'while'.");
        var conditionExpr = Expression();
        Consume(TokenType.RightParen, "Expect ')' after while condition.");
        
        var bodyStmt = Statement();

        _loopDepth--;
        return new While(conditionExpr, bodyStmt);
    }
    private If IfStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'if'.");
        var conditionExpr = Expression();
        Consume(TokenType.RightParen, "Expect ')' after if condition.");
        
        var thenBranchStmt = Statement();
        Stmt? elseBranchStmt = null;
        if (Match(TokenType.Else))
            elseBranchStmt = Statement();
        
        return new If(conditionExpr, thenBranchStmt, elseBranchStmt);
    }
    private List<Stmt?> Block()
    {
        List<Stmt?> statements = [];

        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            statements.Add(Declaration());
        }
        
        Consume(TokenType.RightBrace, "Expect '}' after block.");

        return statements;
    }
    
    private Print PrintStatement()
    {
        var valueExpr = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after expression.");
        return new Print(valueExpr);
    }

    private Expression ExpressionStatement()
    {
        var expr = Expression();
        
        Consume(TokenType.Semicolon, "Expect ';' after expression.");

        return new Expression(expr);
    }
    
    private Expr Expression()
    {
        return Assignment();
    }

    private Expr Assignment()
    {
        var expr = Or();

        // ReSharper disable once InvertIf
        if (Match(TokenType.Equal))
        {
            var equalsToken = Previous();
            var value = Assignment();
            
            switch (expr)
            {
                case Variable variable:
                {
                    var nameToken = variable.Name;
                    return new Assign(nameToken, value);
                }
                case Get getter:
                    return new Set(getter.Object, getter.Name, value);
                default:
                    Error(equalsToken, "Invalid assignment.");
                    break;
            }
        }
        
        return expr;
    }

    private Expr Or()
    {
        var expr = And();

        while (Match(TokenType.Or))
        {
            var operatorToken = Previous();
            var rightExpr = And();
            
            expr = new Logical(expr, operatorToken, rightExpr);
        }

        return expr;
    }

    private Expr And()
    {
        var expr = Conditional();

        while (Match(TokenType.And))
        {
            var operatorToken = Previous();
            var rightExpr = Conditional();
            
            expr = new Logical(expr, operatorToken, rightExpr);
        }

        return expr;
    }
    private Expr Conditional()
    {
        var expr = Comma();
        // Disable Resharper rule in sake of clarity
        // ReSharper disable once InvertIf
        if (Match(TokenType.QuestionMark))
        {
            var thenBranchExpr = Equality();
            _ = Consume(TokenType.Colon, "Expect ':' after then branch of conditional.");
            var elseBranchExpr = Conditional();
            expr = new Conditional(expr, thenBranchExpr, elseBranchExpr);
        }

        return expr;
    }

    private Expr Comma()
    {
        var expr = Equality();
        // Disable Resharper rule in sake of clarity
        // ReSharper disable once InvertIf
        while (Match(TokenType.Comma))
        {
            var rightExpr = Conditional();
            expr = new Comma(expr, rightExpr);
        }

        return expr;
    }

    private Expr Equality()
    {
        return ParseRightExpression(
            Comparison,
            TokenType.EqualEqual);
    }

    private Expr Comparison()
    {
        return ParseRightExpression(
            Term,
            TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual);
    }

    private Expr Term()
    {
        return ParseRightExpression(
            Factor,
            TokenType.Plus, TokenType.Minus);
    }

    private Expr Factor()
    {
        return ParseRightExpression(
            Unary,
            TokenType.Star, TokenType.Slash);
    }

    private Expr Unary()
    {
        if (Match(TokenType.Bang, TokenType.Minus))
        {
            var operatorToken = Previous();
            var rightExpr = Unary();
            return new Unary(operatorToken, rightExpr);
        }
        // Disable Resharper rule in sake of clarity
        // ReSharper disable once InvertIf
        if (Match(_incompleteOperators))
        {
            var operatorToken = Previous();
            ReportError(operatorToken, "Binary operator without left-hand side.");
            var rightExpr = Equality();
            return new Binary(null, operatorToken, rightExpr);
        }

        return Call();
    }

    private Call FinishCall(Expr callee)
    {
        List<Expr> arguments = [];
        if (!Check(TokenType.RightParen))
            do
            {
                if (arguments.Count >= 255)
                    Error(Peek(), "Can't have more than 255 arguments.");
                
                arguments.Add(Equality());
            } while (Match(TokenType.Comma));
        
        var parenToken = Consume(TokenType.RightParen, "Expect ')' after call.");

        return new Call(callee, parenToken, arguments);
    }
    
    private Expr Call()
    {
        var expr = Primary();

        while (true)
        {
            if (Match(TokenType.LeftParen))
                expr = FinishCall(expr);
            else if (Match(TokenType.Dot))
            {
                var nameToken = Consume(TokenType.Identifier, "Expect property name after '.'.");
                expr = new Get(expr, nameToken);
            }
            else
                break;
        }

        return expr;
    }

    private Expr Primary()
    {
        if (Match(TokenType.False)) return new Literal(false);
        if (Match(TokenType.True)) return new Literal(true);
        if (Match(TokenType.Nil)) return new Literal(null);
        if (Match(TokenType.Fun)) return FunctionBody("function");
        
        if (Match(TokenType.Number, TokenType.String))
            return new Literal(Previous().Literal);

        if (Match(TokenType.This)) return new This(Previous());
        
        if (Match(TokenType.Identifier))
            return new Variable(Previous());
        
        // Disable Resharper rule in sake of clarity
        // ReSharper disable once InvertIf
        if (Match(TokenType.LeftParen))
        {
            var rightExpr = Expression();
            _ = Consume(TokenType.RightParen, "Expect: ')' after expression.");
            return new Grouping(rightExpr);
        }

        throw Error(Peek(), "Expected expression.");
    }
    
    private Expr ParseRightExpression(Func<Expr> higherPrecedence, params TokenType[] types)
    {
        var expr = higherPrecedence();

        while (Match(types))
        {
            var operatorToken = Previous();
            var rightExpr = higherPrecedence();
            expr = new Binary(expr, operatorToken, rightExpr);
        }

        return expr;
    }

    private bool Match(params TokenType[] tokenTypes)
    {
        if (!tokenTypes.Any(Check)) return false;
        Advance();
        return true;
    }

    private Token Consume(TokenType tokenType, string message)
    {
        if (Check(tokenType)) return Advance();

        throw Error(Peek(), message);
    }

    private bool Check(TokenType type)
    {
        return !IsAtEnd() && Peek().Type == type;
    }

    private Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return Previous();
    }

    private bool IsAtEnd()
    {
        return Peek().Type == TokenType.Eof;
    }

    private Token Peek()
    {
        return tokens[_current];
    }

    private Token Previous()
    {
        return tokens[_current - 1];
    }

    private bool CheckNext(TokenType tokenType)
    {
        if (IsAtEnd()) return false;
        if (tokens[_current + 1].Type == TokenType.Eof) return false;
        return tokens[_current + 1].Type == tokenType;
    }

    private static ParserError Error(Token token, string message)
    {
        Program.Error(token, message);
        return new ParserError();
    }
    private static void ReportError(Token token, string message)
    {
        Program.Error(token, message);
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Previous().Type == TokenType.Semicolon) return;
            
            // Disable Resharper rule in sake of clarity
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (Peek().Type)
            {
                case TokenType.Class:
                case TokenType.Fun:
                case TokenType.Var:
                case TokenType.For:
                case TokenType.If:
                case TokenType.While:
                case TokenType.Print:
                case TokenType.Return:
                    return;
            }

            Advance();
        }
    }
}