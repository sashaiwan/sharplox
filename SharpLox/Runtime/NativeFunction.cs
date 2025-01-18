namespace SharpLox;

public class NativeFunction(int arity, Func<Interpreter, List<object>, object> function) : ILoxCallable
{
    public int Arity() => arity;
    
    public object Call(Interpreter interpreter, List<object> arguments)
        => function(interpreter, arguments);
    
    public override string ToString() => "<native fn>";
}