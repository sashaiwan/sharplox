using System.Diagnostics;
using System.Reflection;

namespace SharpLox;

[AttributeUsage(AttributeTargets.Method)]
public class TrackScopeAttribute(string? description = null) : Attribute
{
    public string? Description { get; } = description;
}

public class ScopeEventArgs : EventArgs
{
    public required string Operation { get; init; }
    public required IReadOnlyDictionary<string, bool> Variables { get; init; }
    public required int Depth { get; init; }
    public required string? CallerMethod { get; init; }
    public required string? Description { get; init; }
}

public static class ScopeTracker
{
    public static event EventHandler<ScopeEventArgs>? ScopeChanged;
    
    [Conditional("DEBUG")]
    public static void Record(IReadOnlyDictionary<string, bool> scope, int depth, 
        [System.Runtime.CompilerServices.CallerMemberName] string? caller = null)
    {
        var methodInfo = typeof(Resolver).GetMethod(caller ?? string.Empty);
        var attribute = methodInfo?.GetCustomAttribute<TrackScopeAttribute>();
        
        ScopeChanged?.Invoke(null, new ScopeEventArgs
        {
            Operation = caller ?? "Unknown",
            Variables = new Dictionary<string, bool>(scope),
            Depth = depth,
            CallerMethod = caller,
            Description = attribute?.Description
        });
    }
}

public class ScopeHistoryCollector
{
    private readonly List<ScopeEventArgs> _history = [];

    public ScopeHistoryCollector()
    {
        ScopeTracker.ScopeChanged += OnScopeChanged;
    }

    private void OnScopeChanged(object? sender, ScopeEventArgs e)
    {
        _history.Add(e);
    }
    
    public void DumpHistory()
    {
        Console.WriteLine("\n=== Scope History ===");
        foreach (var snapshot in _history)
        {
            Console.WriteLine($"\nOperation: {snapshot.Operation}");
            Console.WriteLine($"Caller: {snapshot.CallerMethod}");
            if (snapshot.Description != null)
                Console.WriteLine($"Description: {snapshot.Description}");
            Console.WriteLine($"Depth: {snapshot.Depth}");
            Console.WriteLine("Variables:");
            foreach (var (name, initialized) in snapshot.Variables)
            {
                Console.WriteLine($"  {name} (initialized: {initialized})");
            }
        }
    }
}