namespace SharpLox;

internal readonly struct VariableState
{
    private readonly StateFlags _state;
    public Token DeclarationToken { get; }
    
    private VariableState(StateFlags state, Token token)
    {
        _state = state;
        DeclarationToken = token;
    }
    
    public static VariableState CreateDeclared(Token token) => 
        new(StateFlags.Declared, token);

    public static VariableState CreateParameter(Token token) => 
        new(StateFlags.Declared | StateFlags.Initialized, token);

    public static VariableState CreateSynthetic(Token token) => 
        new(StateFlags.Declared | StateFlags.Initialized | StateFlags.Synthetic, token);
    
    public VariableState WithInitialized() => 
        new(_state | StateFlags.Initialized, DeclarationToken);
        
    public VariableState WithUsed() => 
        new(_state | StateFlags.Used, DeclarationToken);
    
    public bool Declared => _state.HasFlag(StateFlags.Declared);
    public bool Initialized => _state.HasFlag(StateFlags.Initialized);
    public bool Used => _state.HasFlag(StateFlags.Used);
    public bool Parameter => _state.HasFlag(StateFlags.Parameter);
    public bool Synthetic => _state.HasFlag(StateFlags.Synthetic);
    
    [Flags]
    private enum StateFlags
    {
        None = 0,
        Declared = 1,
        Initialized = 2,
        Used = 4,
        Parameter = 8,
        Synthetic = 16,
    }
}