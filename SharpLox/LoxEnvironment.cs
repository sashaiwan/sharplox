namespace SharpLox;

public sealed class LoxEnvironment
{
    public LoxEnvironment? Enclosing { get; }
    private readonly Dictionary<string, (object? Value, bool Initialized)> _values = new();

    public LoxEnvironment()
    {
        Enclosing = null;
    }

    public LoxEnvironment(LoxEnvironment enclosing)
    {
        Enclosing = enclosing;
    }

    public Dictionary<string, (object? Value, bool Initialized)> GetValues() => _values;
    public void Define(string name, object? value, bool initialized = true)
    {
        _values[name] = (value, initialized);
    }

    public object GetAt(int distance, string name)
    {
        var env = Ancestor(distance);
        return env._values[name].Value!;
    }
    
    private LoxEnvironment Ancestor(int distance)
    {
        var environment = this;
        for (var i = 0; i < distance; i++)
        {
            environment = environment.Enclosing!;  // The ! is safe because resolver verified the depth
        }
        return environment;
    }

    public void AssignAt(int distance, Token name, object value)
    {
        if (Ancestor(distance)._values.TryGetValue(name.Lexeme, out _))
        {
            Ancestor(distance)._values[name.Lexeme] = (value, true);
        }
    }
    
    public object? Get(Token name)
    {
        if (_values.TryGetValue(name.Lexeme, out var entry))
        {
            if (!entry.Initialized)
                throw new RuntimeError(name, $"Variable '{name.Lexeme}' is not initialized.");
            
            return entry.Value;
        }
        
        if (Enclosing is null)
            throw new RuntimeError(name, $"Variable '{name.Lexeme}' is not defined.");
        
        return Enclosing.Get(name);
    }

    public void Assign(Token name, object value)
    {
        if (_values.ContainsKey(name.Lexeme))
        {
             _values[name.Lexeme] = (value, true);
             return;
        }

        if (Enclosing is null) throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
        
        Enclosing.Assign(name, value);
    }
}