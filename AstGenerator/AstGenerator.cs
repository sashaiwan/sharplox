using System.Text;
using Microsoft.CodeAnalysis;

namespace AstGenerator;

public readonly record struct AstDefinition(string ClassName, IEnumerable<string> Fields);

[Generator]
public class AstGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) {}

    public void Execute(GeneratorExecutionContext context)
    {
        const string exprBaseRecord = "Expr";
        const string stmtBaseRecord = "Stmt";
        
        var genericExprSource = GenerateAstRecords(AstDefinitions.ExprNodeDefinitions, exprBaseRecord, generic: true);
        context.AddSource($"{exprBaseRecord}.g.cs", genericExprSource);
        
        var voidExprSource = GenerateAstVoidInterface(AstDefinitions.ExprNodeDefinitions, exprBaseRecord);
        context.AddSource($"{exprBaseRecord}Void.g.cs", voidExprSource);

        var stmtSource = GenerateAstRecords(AstDefinitions.StatementNodeDefinitions, stmtBaseRecord);
        context.AddSource($"{stmtBaseRecord}.g.cs", stmtSource);
    }

    private static string GenerateAstVoidInterface(string definitions, string baseClass)
    {
        var sb = new StringBuilder();
        
        AddUsingsAndNamespace(sb);
        
        var astDefinitions = ParseAstDefinitions(definitions, baseClass);
        GenerateVisitorInterface(sb, baseClass, astDefinitions, false);

        return sb.ToString();
    }
    private static string GenerateAstRecords(string definitions, string baseClass, bool generic = false)
    {
        var sb = new StringBuilder();
        var astDefinitions = ParseAstDefinitions(definitions, baseClass);
        
        AddUsingsAndNamespace(sb);

        GenerateVisitorInterface(sb, baseClass, astDefinitions, generic);
        
        var visitorType = generic ? "<T>" : "";
        var acceptReturn = generic ? "T" : "void";
        
        sb.AppendLine($"public abstract record {baseClass}");
        sb.AppendLine("{");
        sb.AppendLine($"    public abstract {acceptReturn} Accept{visitorType}(I{baseClass}Visitor{visitorType} visitor);");
        if (baseClass == "Expr")
            sb.AppendLine($"    public abstract void Accept(I{baseClass}Visitor visitor);");
        sb.AppendLine(
            """
            };

            """);
        
        astDefinitions.ForEach(d =>
        {
            sb.AppendLine($"public sealed record {d.ClassName}({string.Join(", ", d.Fields)}) : {baseClass}");
            sb.AppendLine("{");
            sb.AppendLine($"    public override {acceptReturn} Accept{visitorType}(I{baseClass}Visitor{visitorType} visitor) => visitor.Visit{d.ClassName}{baseClass}(this);");
            if (baseClass == "Expr")
                sb.AppendLine($"    public override void Accept(I{baseClass}Visitor visitor) => visitor.Visit{d.ClassName}{baseClass}(this);");
            sb.AppendLine("}");
        });
        
        return sb.ToString();
    }

    private static List<AstDefinition> ParseAstDefinitions(string definitions, string baseClass)
    {
        List<AstDefinition> astDefinitions = [];
        
        foreach (var definition in definitions.Split('\n'))
        {
            var trimmed = definition.Trim();
            
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(baseClass) || !char.IsUpper(trimmed[0])) continue;
            
            var parts = trimmed.Split(':').Select(p => p.Trim()).ToArray();
            var className = parts.First();
            var fields = parts.Last().Split(',').Select(f => f.Trim());
            astDefinitions.Add(new AstDefinition { ClassName = className, Fields = fields });
        }

        return astDefinitions;
    }
    private static void GenerateVisitorInterface(
        StringBuilder sb, 
        string baseClass, 
        List<AstDefinition> definitions, 
        bool returnsValue)
    {
        var returnType = returnsValue ? "<T>" : "";
        var methodReturn = returnsValue ? "T" : "void";
    
        sb.AppendLine($"public interface I{baseClass}Visitor{returnType}");
        sb.AppendLine("{");
        definitions.ForEach(d => 
            sb.AppendLine($"    {methodReturn} Visit{d.ClassName}{baseClass}({d.ClassName} {baseClass.ToLowerInvariant()});"));
        sb.AppendLine("}");
    }
    
    private static void AddUsingsAndNamespace(StringBuilder sb)
        => sb.AppendLine(
            """
            using System;

            namespace SharpLox;

            """);
}