namespace SharpLox;

public class LoxInstance(LoxClass klass)
{
    private readonly Dictionary<string, object> _fields = [];

    public object Get(Token name)
    {
        if (_fields.TryGetValue(name.Lexeme, out var value))
            return value;

        var method = klass.FindMethod(name.Lexeme);
        if (method != null)
            return method.Bind(this);
        
        throw new RuntimeError(name, $"Undefined property {name.Lexeme}.");
    }
    
    public void Set(Token name, object value) => _fields[name.Lexeme] = value;
    
    public override string ToString() => $"{klass.Name} instance";
}