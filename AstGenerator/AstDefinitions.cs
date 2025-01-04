namespace AstGenerator;

public static class AstDefinitions
{
    public const string ExprNodeDefinitions = 
        """
        Expr
            Assign      : Token Name, Expr Value
            Binary      : Expr Left, Token Operator, Expr Right
            Call        : Expr Callee, Token Paren, List<Expr> Arguments
            Grouping    : Expr Expression
            Lambda      : List<Token> Parameters, List<Stmt> Body
            Literal     : object Value
            Logical     : Expr Left, Token Operator, Expr Right
            Unary       : Token Operator, Expr Right
            Conditional : Expr Condition, Expr ThenBranch, Expr ElseBranch
            Comma       : Expr Left, Expr Right
            Variable    : Token Name
        """;

    public const string StatementNodeDefinitions =
        """
        Stmt
            Block       : List<Stmt?> Statements
            Break       : Token Keyword
            Class       : Token Name, List<Function> Methods
            Expression  : Expr Expr
            Function    : Token Name, Lambda FunctionExpr
            If          : Expr Condition, Stmt ThenBranch, Stmt ElseBranch
            Print       : Expr Expression
            Return      : Token Keyword, Expr Value
            Var         : Token Name, Expr Initializer
            While       : Expr Condition, Stmt Body
        """;
}