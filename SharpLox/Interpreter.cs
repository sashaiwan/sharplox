namespace SharpLox;

public sealed class Interpreter : IExprVisitor<object>, IStmtVisitor
{
    private static readonly LoxEnvironment GlobalEnvironment = new ();
    private LoxEnvironment _environment = GlobalEnvironment;
    private readonly Dictionary<Expr, int> _locals = [];

    public Interpreter()
    {
        GlobalEnvironment.Define("clock", new NativeFunction(
            arity: 0,
            (_, _) => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0)
        );
    }
    
    public void Interpret(List<Stmt> statements)
    {
        try
        {
            statements.ForEach(Execute);
        }
        catch (RuntimeError e)
        {
            Program.RuntimeError(e);
        }
    }

    public void EvaluateAndPrint(List<Stmt> stmts)
    {
        try
        {
            foreach (var statement in stmts)
            {
                if (statement is Expression expr)
                {
                    var value = Evaluate(expr.Expr);
                    Console.WriteLine(Stringify(value));
                }
                else
                { 
                    Execute(statement);
                }
            }
        }
        catch (RuntimeError e)
        {
            Program.RuntimeError(e);
        }
    }

    public object VisitLambdaExpr(Lambda expr)
    {
        return new LoxFunction(null, expr, _environment);
    }

    public object VisitLiteralExpr(Literal literal) => literal.Value;
    
    public object VisitLogicalExpr(Logical expr)
    {
        var leftObj = Evaluate(expr.Left);
        if (expr.Operator.Type == TokenType.Or)
        {
            if (IsTruthy(leftObj))
                return leftObj;
        }
        else
            if (!IsTruthy(leftObj)) 
                return leftObj;
        
        return Evaluate(expr.Right);
    }

    public object VisitSetExpr(Set expr)
    {
        var obj = Evaluate(expr.Object);

        if (obj is not LoxInstance instance)
        {
            throw new RuntimeError(expr.Name, "Only instances have fields.");
        }
        
        var valueObj = Evaluate(expr.Value);
        instance.Set(expr.Name, valueObj);

        return valueObj;
    }

    public object VisitThisExpr(This expr)
    {
        return LookupVariable(expr.Keyword, expr);
    }

    public object VisitCallExpr(Call expr)
    {
        var calleeObj = Evaluate(expr.Callee);
        
        if (calleeObj is not ILoxCallable function)  // Check type first
            throw new RuntimeError(expr.Paren, "Can only call functions and classes.");
    
        var arguments = expr.Arguments.Select(Evaluate).ToList();

        if (arguments.Count != function.Arity)  // Then check arity
            throw new RuntimeError(expr.Paren, $"Expected {function.Arity} but got {arguments.Count}.");
        
        return function.Call(this, arguments);
    }

    public object VisitGetExpr(Get expr)
    {
        var obj = Evaluate(expr.Object);
        if (obj is LoxInstance instance)
        {
            return instance.Get(expr.Name);
        }
        
        throw new RuntimeError(expr.Name, "Only instances have properties.");
    }

    public object VisitGroupingExpr(Grouping grouping) => Evaluate(grouping.Expression);

    public object VisitUnaryExpr(Unary expr)
    {
        var rightExpr = Evaluate(expr.Right);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (expr.Operator.Type)
        {
            case TokenType.Bang:
                return !IsTruthy(rightExpr);
            case TokenType.Minus:
                CheckNumberOperand(expr.Operator, rightExpr);
                return -(double)rightExpr;
            default:
                throw new RuntimeError(expr.Operator, "Unsupported operator");
        }
    }

    public object VisitConditionalExpr(Conditional expr) 
        => IsTruthy(Evaluate(expr.Condition)) ? Evaluate(expr.ThenBranch) : Evaluate(expr.ElseBranch);
    

    public object VisitCommaExpr(Comma expr)
    {
        _ = Evaluate(expr.Left);
        var rightObj = Evaluate(expr.Right);
        return rightObj;
    }

    public object VisitVariableExpr(Variable expr) => LookupVariable(expr.Name, expr);

    private object LookupVariable(Token name, Expr expr)
    {
        if (_locals.TryGetValue(expr, out var location))
        {
            return _environment.GetAt(location, name.Lexeme);
        }
    
        return GlobalEnvironment.Get(name)!;
    }
    public void VisitReturnStmt(Return stmt)
    {
        object? value = null;
        if(stmt.Value != null)
            value = Evaluate(stmt.Value);

        throw new ReturnException(value);
    }

    public void VisitVarStmt(Var stmt)
    {
        var hasInitializer = stmt.Initializer != null;
        
        var value = hasInitializer ? Evaluate(stmt.Initializer!) : null;
        
        _environment.Define(stmt.Name.Lexeme, value, initialized: hasInitializer);
    }

    public void VisitWhileStmt(While stmt)
    {
        while (IsTruthy(Evaluate(stmt.Condition)))
        {
            try
            {
                Execute(stmt.Body);
            }
            catch (BreakException)
            {
                break;
            }   
        }
    }

    public void VisitBlockStmt(Block stmt)
    {
        // var blockEnv = new LoxEnvironment(size: count, _environment);
        var previousEnv = _environment;
        try
        {
            // _environment = blockEnv;
            foreach (var statement in stmt.Statements.OfType<Stmt>())
            {
                Execute(statement);
            }
        }
        finally
        {
            _environment = previousEnv;
        }
    }

    public void VisitBreakStmt(Break stmt)
    {
        throw new BreakException();
    }

    public void VisitClassStmt(Class stmt)
    {
        _environment.Define(stmt.Name.Lexeme, null);

        Dictionary<string, LoxFunction> methods = [];
        foreach (var method in stmt.Methods)
        {
            var function = new LoxFunction(null, method.FunctionExpr, _environment);
            methods[method.Name.Lexeme] = function;
        }
        
        var klass = new LoxClass(stmt.Name.Lexeme, methods);
        
        _environment.Assign(stmt.Name, klass);
    }

    public void VisitExpressionStmt(Expression stmt) => Evaluate(stmt.Expr);
    public void VisitFunctionStmt(Function stmt)
    {
        var functionName = stmt.Name.Lexeme;
        var loxFunction = new LoxFunction(functionName, stmt.FunctionExpr, _environment);
        _environment.Define(stmt.Name.Lexeme, loxFunction);
    }

    public void VisitIfStmt(If stmt)
    {
        if(IsTruthy(Evaluate(stmt.Condition)))
            Execute(stmt.ThenBranch);
        else if(stmt.ElseBranch is not null)
            Execute(stmt.ElseBranch);
    }

    public void VisitPrintStmt(Print stmt)
    {
        var value = Evaluate(stmt.Expression);
        Console.WriteLine(Stringify(value));
    }

    public object VisitAssignExpr(Assign expr)
    {
        var valueObj = Evaluate(expr.Value);

        if (_locals.TryGetValue(expr, out var location))
        {
            _environment.AssignAt(location, expr.Name, valueObj);
        }
        else
        {
            GlobalEnvironment.Assign(expr.Name, valueObj);
        }
        
        return valueObj;
    }

    public object VisitBinaryExpr(Binary expr)
    {
        // Handle error productions
        if (expr.Left == null)
            throw new RuntimeError(expr.Operator, "Invalid binary expression with missing left operand.");
        
        var leftObj = Evaluate(expr.Left);
        var rightObj = Evaluate(expr.Right);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (expr.Operator.Type)
        {
            case TokenType.Minus:
                CheckNumberOperand(expr.Operator, leftObj, rightObj);
                return (double)leftObj - (double)rightObj;
            case TokenType.Slash:
                CheckNumberOperand(expr.Operator, leftObj, rightObj);
                if ((double)rightObj == 0)
                    throw new RuntimeError(expr.Operator, "Cannot divide by zero.");
                return (double)leftObj / (double)rightObj;
            case TokenType.Star:
                CheckNumberOperand(expr.Operator, leftObj, rightObj);
                return (double)leftObj * (double)rightObj;
            case TokenType.Plus when leftObj is string l:
                return string.Concat(l, rightObj.ToString());
            case TokenType.Plus when leftObj is double l && rightObj is double r:
                return l + r;
            case TokenType.Greater:
                CheckNumberOperand(expr.Operator, leftObj, rightObj);
                return (double)leftObj > (double)rightObj;
            case TokenType.GreaterEqual:
                CheckNumberOperand(expr.Operator, leftObj, rightObj);
                return (double)leftObj >= (double)rightObj;
            case TokenType.Less:
                CheckNumberOperand(expr.Operator, leftObj, rightObj);
                return (double)leftObj < (double)rightObj;
            case TokenType.LessEqual:
                CheckNumberOperand(expr.Operator, leftObj, rightObj);
                return (double)leftObj <= (double)rightObj;
            case TokenType.BangEqual:
                return !IsEqual(rightObj, leftObj);
            case TokenType.EqualEqual:
                return IsEqual(rightObj, leftObj);
            default:
                throw new RuntimeError(expr.Operator, "Operands must be two string or two numbers.");
        }
    }
    private object Evaluate(Expr expr) => expr.Accept(this);

    private void Execute(Stmt stmt)
    {
        if (stmt == null)
        {
            throw new RuntimeError(new Token(TokenType.Eof, "null", null, 0), 
                "Attempted to execute null statement.");
        }
        stmt.Accept(this);   
    }
    
    public void Resolve(Expr expr, int depth)
    {
        _locals[expr] = depth;
    }
    
    public void BeginScope(int size) => _environment = new LoxEnvironment(_environment); 
    
    public void ExecuteBlock(List<Stmt?> statements, LoxEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(statements);
        ArgumentNullException.ThrowIfNull(environment);
        
        var previous = _environment;
        try
        {
            _environment = environment;
            foreach (var statement in statements.OfType<Stmt>())
            {
                Execute(statement);
            }
        }       
        finally
        {
            _environment = previous;
        }
    }
    
    private static bool IsTruthy(object expr) => expr switch
    {
        null => false,
        bool b => b,
        _ => true,
    };

    private static bool IsEqual(object a, object b) => Equals(a, b);

    private static void CheckNumberOperand(Token operatorToken, object operand)
    {
        if (operand is double) return;
        throw new RuntimeError(operatorToken, "Operand must be a number");
    }

    private static void CheckNumberOperand(Token operatorToken, object leftObj, object rightObj)
    {
        if (leftObj is double && rightObj is double) return;
        throw new RuntimeError(operatorToken, "Operands must be numbers.");
    }

    private static string Stringify(object value) =>
        value switch
        {
            null => "null",
            true => "true",
            false => "false",
            _ => value.ToString()
        } ?? string.Empty;
}