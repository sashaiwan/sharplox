namespace SharpLox;

internal readonly struct VariableState
{
    private readonly StateFlags _state;
    public Token DeclarationToken { get; }
    public int Index { get; }
    
    private VariableState(StateFlags state, Token token, int index)
    {
        _state = state;
        DeclarationToken = token;
        Index = index;
    }
    
    public static VariableState CreateDeclared(Token token, int index) => 
        new(StateFlags.Declared, token, index);

    public static VariableState CreateParameter(Token token, int index) => 
        new(StateFlags.Declared | StateFlags.Initialized, token, index);

    public static VariableState CreateSynthetic(Token token, int index) => 
        new(StateFlags.Declared | StateFlags.Initialized | StateFlags.Synthetic, token, index);
    
    public VariableState WithInitialized() => 
        new(_state | StateFlags.Initialized, DeclarationToken, Index);
        
    public VariableState WithUsed() => 
        new(_state | StateFlags.Used, DeclarationToken, Index);
    
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