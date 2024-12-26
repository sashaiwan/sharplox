using System.Text;

namespace SharpLox;
public static class Program
{
    private static bool _hadError;
    private static bool _hadRuntimeError;
    private static readonly Interpreter Interpreter = new(); 
    
    public static int Main(string[] args)
    {
        switch (args.Length)
        {
            case > 1:
                Console.WriteLine("Usage: SharpLox.exe [command]");
                return 64;
            case 1:
                RunFile(args[0]);
                break;
            default:
                RunPrompt();
                break;
        }

        return 0;
    }

    private static void Run(string source)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.ScanTokens();
        var parser = new Parser(tokens);
        var statements = parser.Parse();
        
        if (_hadError) return;

        var resolver = new Resolver(Interpreter);
        resolver.Resolve(statements!);
        
        if (_hadError) return;
        
        Interpreter.Interpret(statements!);
    }

    private static void RunFile(string path)
    {
        if (Path.GetExtension(path) != ".lox")
            Environment.Exit(65);
            
        var file = File.ReadAllBytes(path);
        var stringFile = Encoding.UTF8.GetString(file);
        Run(stringFile);
        
        if (_hadError) Environment.Exit(65);
        if (_hadRuntimeError) Environment.Exit(70);
    }

    private static void RunRepl(string line)
    {
        var scanner = new Scanner(line);
        var tokens = scanner.ScanTokens();
        var parser = new Parser(tokens);
        var statements = parser.Parse();
        
        if (_hadError) return;
        
        Interpreter.EvaluateAndPrint(statements!);
    }
    
    private static void RunPrompt()
    {
        using var sr = new StreamReader(Console.OpenStandardInput());

        for (;;)
        {
            Console.Write("> ");
            var line = sr.ReadLine();
            if (line == null) break;

            RunRepl(line);
            // Run(line);
            _hadError = false;
        }
    }
    
    private static void Report(int line, string where, string message)
    {
        Console.Error.WriteLine($"[line {line}] Error {where}: {message}");
        _hadError = true;
    }

    public static void Error(int line, string message) => Report(line, "", message);

    public static void Error(Token token, string message)
    {
        Report(token.Line, token.Type == TokenType.Eof ? "at end" : $"at '{token.Lexeme}'", message);
    }

    public static void RuntimeError(RuntimeError error)
    {
        Console.Error.WriteLine($"{error.Message}\n[line: {error.Token.Line}]");
        _hadRuntimeError = true;
    }
}
