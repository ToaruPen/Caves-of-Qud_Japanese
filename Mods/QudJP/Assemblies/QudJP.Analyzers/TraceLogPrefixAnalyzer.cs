using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace QudJP.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TraceLogPrefixAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "QJ003";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Trace log must include QudJP prefix",
        messageFormat: "Trace.{0} message must start with 'QudJP:' for log identification",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
        if (methodSymbol is null || methodSymbol.ContainingType?.ToDisplayString() != "System.Diagnostics.Trace")
        {
            return;
        }

        if (methodSymbol.Name is not ("TraceError" or "TraceWarning"))
        {
            return;
        }

        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            var noArgDiagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), methodSymbol.Name);
            context.ReportDiagnostic(noArgDiagnostic);
            return;
        }

        var firstArgumentExpression = invocation.ArgumentList.Arguments[0].Expression;
        if (StartsWithQudJPPrefix(firstArgumentExpression))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(Rule, firstArgumentExpression.GetLocation(), methodSymbol.Name);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool StartsWithQudJPPrefix(ExpressionSyntax expression)
    {
        return expression switch
        {
            LiteralExpressionSyntax literal when literal.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression)
                => literal.Token.ValueText.StartsWith("QudJP:", StringComparison.Ordinal),
            InterpolatedStringExpressionSyntax interpolated => StartsWithQudJPPrefix(interpolated),
            _ => false,
        };
    }

    private static bool StartsWithQudJPPrefix(InterpolatedStringExpressionSyntax interpolated)
    {
        if (interpolated.Contents.Count == 0)
        {
            return false;
        }

        if (interpolated.Contents[0] is not InterpolatedStringTextSyntax text)
        {
            return false;
        }

        return text.TextToken.ValueText.StartsWith("QudJP:", StringComparison.Ordinal);
    }
}
