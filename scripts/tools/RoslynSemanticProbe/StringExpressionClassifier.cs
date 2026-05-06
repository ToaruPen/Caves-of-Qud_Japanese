using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace QudJP.Tools.RoslynSemanticProbe;

internal static class StringExpressionClassifier
{
    public static IEnumerable<StringArgumentHit> AnalyzeInvocationArguments(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation)
    {
        for (var index = 0; index < invocation.ArgumentList.Arguments.Count; index++)
        {
            var argument = invocation.ArgumentList.Arguments[index];
            if (!IsStringExpression(semanticModel, argument.Expression))
            {
                continue;
            }

            yield return BuildStringArgumentHit(
                semanticModel,
                index,
                argument.NameColon?.Name.Identifier.ValueText,
                argument.Expression);
        }
    }

    public static IEnumerable<StringArgumentHit> AnalyzeAssignmentExpression(
        SemanticModel semanticModel,
        ExpressionSyntax expression)
    {
        if (IsStringExpression(semanticModel, expression))
        {
            yield return BuildStringArgumentHit(semanticModel, 0, null, expression);
        }
    }

    private static StringArgumentHit BuildStringArgumentHit(
        SemanticModel semanticModel,
        int index,
        string? name,
        ExpressionSyntax expression)
    {
        var constant = semanticModel.GetConstantValue(expression);
        var literalValue = constant.HasValue && constant.Value is string value ? value : null;
        var expressionText = expression.ToString();
        var scanText = literalValue ?? expressionText;
        var hasQudMarkup = scanText.Contains("{{", StringComparison.Ordinal)
            || scanText.Contains('&', StringComparison.Ordinal)
            || scanText.Contains('^', StringComparison.Ordinal);
        var hasTmpMarkup = scanText.Contains("<color", StringComparison.OrdinalIgnoreCase);
        var hasPlaceholderLikeText = scanText.Contains('=') || scanText.Contains('{') || scanText.Contains('}');

        return new StringArgumentHit(
            Index: index,
            Name: name,
            ExpressionKind: ClassifyExpression(expression),
            Expression: expressionText,
            ConstantValue: literalValue,
            HasQudMarkup: hasQudMarkup,
            HasTmpMarkup: hasTmpMarkup,
            HasPlaceholderLikeText: hasPlaceholderLikeText);
    }

    private static bool IsStringExpression(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        var typeInfo = semanticModel.GetTypeInfo(expression);
        if (typeInfo.ConvertedType?.SpecialType == SpecialType.System_String
            || typeInfo.Type?.SpecialType == SpecialType.System_String)
        {
            return true;
        }

        return expression is LiteralExpressionSyntax literal
            && literal.IsKind(SyntaxKind.StringLiteralExpression);
    }

    private static string ClassifyExpression(ExpressionSyntax expression)
    {
        return expression switch
        {
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => "string_literal",
            InterpolatedStringExpressionSyntax => "interpolated_string",
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression) => "concatenation",
            ConditionalExpressionSyntax => "conditional",
            IdentifierNameSyntax or MemberAccessExpressionSyntax => "variable_or_member",
            InvocationExpressionSyntax => "invocation",
            ElementAccessExpressionSyntax => "element_access",
            ParenthesizedExpressionSyntax parenthesized => ClassifyExpression(parenthesized.Expression),
            _ => expression.Kind().ToString(),
        };
    }
}
