namespace SharpLox;

public sealed class LoxEnvironment
{
    private LoxEnvironment? Enclosing { get; }
    private readonly (object? Value, bool Initialized)[] _locals;
    private readonly Dictionary<string, (object? Value, bool Initialized)> _globals;

    // Constructor for global environment
    public LoxEnvironment()
    {
        _locals = [];
        _globals = new Dictionary<string, (object?, bool)>();
        Enclosing = null;
    }

    // Constructor for local scopes
    public LoxEnvironment(int size, LoxEnvironment? enclosing)
    {
        _locals = new (object? Value, bool Initialized)[size];
        _globals = new Dictionary<string, (object?, bool)>();
        Enclosing = enclosing;
    }
    
    public void Define(int index, object? value, bool initialized = true)
    {
        if (index < _locals.Length)
            _locals[index] = (value, initialized);
    }

    public object GetAt(int depth, int index) => Ancestor(depth)._locals[index].Value!;
    
    private LoxEnvironment Ancestor(int distance)
    {
        var environment = this;
        for (var i = 0; i < distance; i++)
        {
            environment = environment.Enclosing!;  // The ! is safe because resolver verified the depth
        }
        return environment;
    }

    public void AssignAt(int depth, int index, object value)
        => Ancestor(depth)._locals[index] = (value, true);
    
    // For globals
    public void Define(string name, object? value, bool initialized = true)
    {
        _globals[name] = (value, initialized);
    }
    
    // For globals
    public object? Get(Token name)
    {
        if (_globals.TryGetValue(name.Lexeme, out var entry))
        {
            if (!entry.Initialized)
                throw new RuntimeError(name, $"Variable '{name.Lexeme}' is not initialized.");
            
            return entry.Value;
        }
        
        if (Enclosing is null)
            throw new RuntimeError(name, $"Variable '{name.Lexeme}' is not defined.");
        
        return Enclosing.Get(name);
    }
    // For globals
    public void Assign(Token name, object value)
    {
        if (_globals.ContainsKey(name.Lexeme))
        {
             _globals[name.Lexeme] = (value, true);
             return;
        }

        if (Enclosing is null) throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
        
        Enclosing.Assign(name, value);
    }
}