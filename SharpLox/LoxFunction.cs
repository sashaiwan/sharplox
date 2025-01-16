namespace SharpLox;

public class LoxFunction(string? name, Lambda declaration, LoxEnvironment closure, bool isInitializer) : ILoxCallable
{
    private readonly bool _isInitializer = isInitializer;
    public int Arity() => declaration.Parameters.Count;
    public object Call(Interpreter interpreter, List<object> arguments)
    {
        var environment = new LoxEnvironment(closure);
        
        for (var i = 0; i < declaration.Parameters.Count; i++)
        {
            environment.Define(declaration.Parameters[i].Lexeme, arguments[i]);
        }

        try
        {
            interpreter.ExecuteBlock(declaration.Body, environment);
        }
        catch (ReturnException e)
        {
            if (_isInitializer)
                return closure.GetAt(0, "this");
            
            return e.Value!;
        }

        if (_isInitializer) return closure.GetAt(0, "this");   
        
        return null!;
    }

    public LoxFunction Bind(LoxInstance instance)
    {
        var environment = new LoxEnvironment(closure);
        environment.Define("this", instance);
        return new LoxFunction(null, declaration, environment, _isInitializer);
    }
    
    public override string ToString() => name is not null ? $"<fn {name}>" : "<fn>";
}