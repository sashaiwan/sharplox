namespace SharpLox;


public class Resolver(Interpreter interpreter) : IStmtVisitor, IExprVisitor
{
    private enum FunctionType
    {
        None,
        Function,
        Lambda
    }
    private readonly Stack<Dictionary<string, bool>> _scopes = [];
    private FunctionType _currentFunction = FunctionType.None;
    
    public void Resolve(List<Stmt> statements) => statements.ForEach(Resolve);
    public void VisitBlockStmt(Block stmt)
    {
        BeginScope();
        Resolve(stmt.Statements!);
        EndScope();
    }

    public void VisitBreakStmt(Break stmt)
    {
        // Nothing to resolve yet
    }

    public void VisitVarStmt(Var stmt)
    {
        Declare(stmt.Name);
        if (stmt.Initializer != null)
            Resolve(stmt.Initializer);
        Define(stmt.Name);
    }

    public void VisitCommaExpr(Comma expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
    }

    public void VisitVariableExpr(Variable expr)
    {
        if (_scopes.Count != 0 && _scopes.Peek().TryGetValue(expr.Name.Lexeme, out bool isDefined))
        {
            if (!isDefined)
            {
                Program.Error(expr.Name, "Can't read local variable in its own initializer.");
            }
        }

        ResolveLocal(expr, expr.Name);
    }
    
    public void VisitAssignExpr(Assign expr)
    {
        Resolve(expr.Value);
        ResolveLocal(expr, expr.Name);
    }

    public void VisitFunctionStmt(Function stmt)
    {
        Declare(stmt.Name);
        Define(stmt.Name);

        ResolveFunction(stmt, FunctionType.Function);
    }
    
    public void VisitLambdaExpr(Lambda stmt) => ResolveLambda(stmt);
    
    public void VisitExpressionStmt(Expression stmt) => Resolve(stmt.Expr);

    public void VisitIfStmt(If stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.ThenBranch);
        if (stmt.ElseBranch != null) 
            Resolve(stmt.ElseBranch);
    }

    public void VisitPrintStmt(Print stmt) => Resolve(stmt.Expression);

    public void VisitReturnStmt(Return stmt)
    {
        if (_currentFunction == FunctionType.None) {
            Program.Error(stmt.Keyword, "Can't return from top-level code.");
        }
        
        if (stmt.Value != null)
            Resolve(stmt.Value);
    }

    public void VisitWhileStmt(While stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.Body);
    }

    public void VisitBinaryExpr(Binary expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
    }

    public void VisitCallExpr(Call expr)
    {
        Resolve(expr.Callee);

        foreach (var argument in expr.Arguments.OfType<Expr>())
        {
            Resolve(argument);
        }
    }

    public void VisitGroupingExpr(Grouping stmt) => Resolve(stmt.Expression);

    public void VisitLiteralExpr(Literal expr)
    {
        // Nothing to resolve
    }

    public void VisitLogicalExpr(Logical expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
    }
    
    public void VisitUnaryExpr(Unary expr) => Resolve(expr.Right);
    public void VisitConditionalExpr(Conditional expr)
    {
        Resolve(expr.Condition);
        Resolve(expr.ThenBranch);
        Resolve(expr.ThenBranch);
    }

    private void Resolve(Stmt stmt) => stmt.Accept(this);
    private void Resolve(Expr expr) => expr.Accept(this);
    private void ResolveFunction(Function function, FunctionType type)
    {
        var enclosingType = _currentFunction;
        _currentFunction = type;
        
        BeginScope();
        foreach (var param in function.FunctionExpr.Parameters)
        {
            Declare(param);
            Define(param);
        }
        Resolve(function.FunctionExpr.Body);
        EndScope();

        _currentFunction = enclosingType;
    }
    private void ResolveLambda(Lambda lambda)
    {
        var enclosingType = _currentFunction;
        _currentFunction = FunctionType.Lambda;

        BeginScope();
        foreach (var param in lambda.Parameters)
        {
            Declare(param);
            Define(param);
        }
        Resolve(lambda.Body);
        EndScope();
        
        _currentFunction = enclosingType;
    }
    private void BeginScope() => _scopes.Push([]);
    private void EndScope() => _scopes.Pop();
    private void Declare(Token name)
    {
        if (_scopes.Count == 0)
            return;
        
        var scope = _scopes.Peek();
        if (scope.ContainsKey(name.Lexeme))
        {
            Program.Error(name, "Already a variable with this name in this scope.");
        }
        
        scope[name.Lexeme] = false;
    }
    private void Define(Token name)
    {
        if (_scopes.Count == 0)
            return;
        _scopes.Peek()[name.Lexeme] = true;
    }
    private void ResolveLocal(Expr expr, Token name)
    {
        for (var i = 0; i < _scopes.Count; i++)  // Changed direction
        {
            var scope = _scopes.ElementAt(i);
            if (scope.ContainsKey(name.Lexeme))
            {
                interpreter.Resolve(expr, i);  // Use i directly
                return;
            }
        }
    }
}