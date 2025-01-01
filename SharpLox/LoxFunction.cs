namespace SharpLox;

public class LoxFunction(string? name, Lambda declaration, LoxEnvironment closure) : ILoxCallable
{
    public int Arity { get; } = declaration.Parameters.Count;
    public object Call(Interpreter interpreter, List<object> arguments)
    {
        var environment = new LoxEnvironment(size: declaration.Parameters.Count, closure);
        
        for (var i = 0; i < declaration.Parameters.Count; i++)
        {
            environment.Define(i, arguments[i]);
        }

        try
        {
            interpreter.ExecuteBlock(declaration.Body, environment);
        }
        catch (ReturnException e)
        {
            return e.Value!;
        }

        return null!;
    }

    public override string ToString() => name is not null ? $"<fn {name}>" : "<fn>";
}