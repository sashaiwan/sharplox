using System;

namespace SharpLox;
{
    public abstract record Expr {};
    public sealed record Binary(Expr Left, Token Operator, Expr Right) : Expr;
    public sealed record Grouping(Expr Expression) : Expr;
    public sealed record Literal(object Value) : Expr;
    public sealed record Unary(Token Operator, Expr Expression) : Expr;
}
