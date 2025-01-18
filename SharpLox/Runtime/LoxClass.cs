namespace SharpLox;

public sealed class LoxClass(string name, LoxClass? superclass, Dictionary<string, LoxFunction> methods) : ILoxCallable
{
    public string Name { get; } = name;
    private LoxClass? SuperClass { get; } = superclass;
    private Dictionary<string, LoxFunction> Methods { get; } = methods;
    
    public override string ToString() => Name;

    public int Arity()
    {
        var initializer = FindMethod("init");
        return initializer?.Arity() ?? 0;
    }

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        var instance = new LoxInstance(this);
        LoxFunction? initializer = FindMethod("init");
        initializer?.Bind(instance).Call(interpreter, arguments);
        
        return instance;
    }

    public LoxFunction? FindMethod(string name)     
        => Methods.TryGetValue(name, out var method) ? method : SuperClass?.FindMethod(name);
    
}