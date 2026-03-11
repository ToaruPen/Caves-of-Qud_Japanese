using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace QudJP.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FallbackLoggingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "QJ002";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Fallback value must be preceded by logging",
        messageFormat: "Null-coalescing fallback for '{0}' should be preceded by Trace.TraceWarning or Trace.TraceError",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeCoalesceExpression, SyntaxKind.CoalesceExpression);
    }

    private static void AnalyzeCoalesceExpression(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not BinaryExpressionSyntax coalesceExpression)
        {
            return;
        }

        if (IsTestSourceFile(context.Node.SyntaxTree.FilePath) || IsInsideCatchBlock(coalesceExpression))
        {
            return;
        }

        var leftExpression = UnwrapParentheses(coalesceExpression.Left);
        if (!IsMethodCallResult(leftExpression))
        {
            return;
        }

        if (HasPrecedingTraceLog(coalesceExpression, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(Rule, coalesceExpression.GetLocation(), leftExpression.ToString());
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsTestSourceFile(string? filePath)
    {
        var sourcePath = filePath;
        if (sourcePath is null || sourcePath.Length == 0)
        {
            return false;
        }

        return sourcePath.IndexOf("QudJP.Tests", StringComparison.OrdinalIgnoreCase) >= 0
            || sourcePath.IndexOf("QudJP.Analyzers.Tests", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsInsideCatchBlock(SyntaxNode node)
    {
        return node.Ancestors().Any(static ancestor => ancestor is CatchClauseSyntax);
    }

    private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }

    private static bool IsMethodCallResult(ExpressionSyntax expression)
    {
        return expression switch
        {
            InvocationExpressionSyntax => true,
            MemberAccessExpressionSyntax memberAccess when UnwrapParentheses(memberAccess.Expression) is InvocationExpressionSyntax => true,
            _ => false,
        };
    }

    private static bool HasPrecedingTraceLog(
        BinaryExpressionSyntax coalesceExpression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var containingStatement = coalesceExpression.FirstAncestorOrSelf<StatementSyntax>();
        if (containingStatement is null || containingStatement.Parent is not BlockSyntax block)
        {
            return true;
        }

        var statementIndex = block.Statements.IndexOf(containingStatement);
        if (statementIndex <= 0)
        {
            return false;
        }

        for (var index = statementIndex - 1; index >= 0; index--)
        {
            if (ContainsTraceWarningOrError(block.Statements[index], semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsTraceWarningOrError(
        StatementSyntax statement,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        foreach (var invocation in statement.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (IsTraceWarningOrError(invocation, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTraceWarningOrError(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
        if (symbol is null)
        {
            return false;
        }

        if (symbol.ContainingType?.ToDisplayString() != "System.Diagnostics.Trace")
        {
            return false;
        }

        return symbol.Name is "TraceWarning" or "TraceError";
    }
}
