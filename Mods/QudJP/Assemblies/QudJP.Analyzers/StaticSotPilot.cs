using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace QudJP.Analyzers;

public static class StaticSotPilot
{
    private static readonly string[] RequiredRelativePaths =
    [
        "XRL.World.Effects/Prone.cs",
        "XRL.World.Capabilities/Firefighting.cs",
        "XRL.World.Effects/HolographicBleeding.cs",
        "XRL.World.Parts.Mutation/ElectricalGeneration.cs",
    ];

    public static string[] GenerateJsonLines(string[] relativePaths, string[] sourceTexts)
    {
        if (relativePaths is null)
        {
            throw new ArgumentNullException(nameof(relativePaths));
        }

        if (sourceTexts is null)
        {
            throw new ArgumentNullException(nameof(sourceTexts));
        }

        if (relativePaths.Length != RequiredRelativePaths.Length || sourceTexts.Length != RequiredRelativePaths.Length)
        {
            throw new ArgumentException("The Roslyn pilot requires exactly four fixed inputs.");
        }

        for (var index = 0; index < RequiredRelativePaths.Length; index++)
        {
            if (!string.Equals(relativePaths[index], RequiredRelativePaths[index], StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected pilot input '{relativePaths[index]}'. Expected '{RequiredRelativePaths[index]}'.", nameof(relativePaths));
            }
        }

        var records = ExtractRecords(relativePaths, sourceTexts);
        var jsonLines = new string[records.Count];
        for (var index = 0; index < records.Count; index++)
        {
            jsonLines[index] = SerializeRecord(records[index]);
        }

        return jsonLines;
    }

    private static List<StaticSotRecord> ExtractRecords(string[] relativePaths, string[] sourceTexts)
    {
        var records = new List<StaticSotRecord>();
        for (var fileIndex = 0; fileIndex < relativePaths.Length; fileIndex++)
        {
            var relativePath = relativePaths[fileIndex];
            var sourceText = sourceTexts[fileIndex] ?? throw new ArgumentException($"Source text at index {fileIndex} was null.", nameof(sourceTexts));
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: relativePath);
            var root = syntaxTree.GetRoot();
            var collector = new StaticSotCollector(relativePath, fileIndex, syntaxTree);
            collector.Visit(root);
            records.AddRange(collector.Records);
        }

        records.Sort(StaticSotRecordComparer.Instance);
        return records;
    }

    private static string SerializeRecord(StaticSotRecord record)
    {
        var builder = new StringBuilder();
        builder.Append('{');
        AppendStringProperty(builder, "file", record.File);
        AppendNumberProperty(builder, "line", record.Line);
        AppendNumberProperty(builder, "col", record.Column);
        AppendStringProperty(builder, "text", record.Text);
        AppendStringProperty(builder, "literal_kind", record.LiteralKind);
        AppendStringProperty(builder, "enclosing_type", record.EnclosingType);
        AppendNullableStringProperty(builder, "enclosing_method", record.EnclosingMethod);
        AppendNullableStringProperty(builder, "containing_invocation", record.ContainingInvocation);
        AppendNullableNumberProperty(builder, "argument_index", record.ArgumentIndex);
        AppendNullableStringProperty(builder, "argument_name", record.ArgumentName);
        AppendNullablePartsProperty(builder, "concat_parts", record.ConcatParts);
        AppendNullablePartsProperty(builder, "interpolation_parts", record.InterpolationParts);
        AppendNullableStringProperty(builder, "attribute_context", record.AttributeContext, isLast: true);
        builder.Append('}');
        return builder.ToString();
    }

    private static void AppendStringProperty(StringBuilder builder, string name, string value)
    {
        AppendPropertyName(builder, name);
        AppendJsonString(builder, value);
        builder.Append(',');
    }

    private static void AppendNullableStringProperty(StringBuilder builder, string name, string? value, bool isLast = false)
    {
        AppendPropertyName(builder, name);
        if (value is null)
        {
            builder.Append("null");
        }
        else
        {
            AppendJsonString(builder, value);
        }

        if (!isLast)
        {
            builder.Append(',');
        }
    }

    private static void AppendNumberProperty(StringBuilder builder, string name, int value)
    {
        AppendPropertyName(builder, name);
        builder.Append(value);
        builder.Append(',');
    }

    private static void AppendNullableNumberProperty(StringBuilder builder, string name, int? value)
    {
        AppendPropertyName(builder, name);
        if (value.HasValue)
        {
            builder.Append(value.Value);
        }
        else
        {
            builder.Append("null");
        }

        builder.Append(',');
    }

    private static void AppendNullablePartsProperty(StringBuilder builder, string name, List<StaticSotPart>? parts)
    {
        AppendPropertyName(builder, name);
        if (parts is null)
        {
            builder.Append("null");
            builder.Append(',');
            return;
        }

        builder.Append('[');
        for (var index = 0; index < parts.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            var part = parts[index];
            builder.Append('{');
            AppendStringProperty(builder, "kind", part.Kind);
            AppendNullableStringProperty(builder, "text", part.Text, isLast: true);
            builder.Append('}');
        }

        builder.Append(']');
        builder.Append(',');
    }

    private static void AppendPropertyName(StringBuilder builder, string name)
    {
        AppendJsonString(builder, name);
        builder.Append(':');
    }

    private static void AppendJsonString(StringBuilder builder, string value)
    {
        builder.Append('"');
        for (var index = 0; index < value.Length; index++)
        {
            var ch = value[index];
            switch (ch)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (char.IsControl(ch))
                    {
                        builder.Append("\\u");
                        builder.Append(((int)ch).ToString("x4"));
                    }
                    else
                    {
                        builder.Append(ch);
                    }
                    break;
            }
        }

        builder.Append('"');
    }

    private sealed class StaticSotCollector : CSharpSyntaxWalker
    {
        private readonly string file;
        private readonly int fileOrder;
        private readonly SyntaxTree syntaxTree;

        public StaticSotCollector(string file, int fileOrder, SyntaxTree syntaxTree)
        {
            this.file = file;
            this.fileOrder = fileOrder;
            this.syntaxTree = syntaxTree;
        }

        public List<StaticSotRecord> Records { get; } = [];

        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.StringLiteralExpression) && !IsNestedInsideTrackedExpression(node))
            {
                Records.Add(CreateRecord(node, node.Token.ValueText, "literal", concatParts: null, interpolationParts: null));
            }

            base.VisitLiteralExpression(node);
        }

        public override void VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
        {
            if (!IsNestedInsideTrackedExpression(node))
            {
                Records.Add(CreateRecord(node, node.ToString(), "interpolation", concatParts: null, interpolationParts: BuildInterpolationParts(node)));
            }

            base.VisitInterpolatedStringExpression(node);
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (IsTopLevelStringConcatenation(node))
            {
                Records.Add(CreateRecord(node, node.ToString(), "concatenation", BuildConcatParts(node), interpolationParts: null));
            }

            base.VisitBinaryExpression(node);
        }

        private StaticSotRecord CreateRecord(
            ExpressionSyntax expression,
            string text,
            string literalKind,
            List<StaticSotPart>? concatParts,
            List<StaticSotPart>? interpolationParts)
        {
            var lineSpan = syntaxTree.GetLineSpan(expression.Span);
            var type = expression.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            var method = expression.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>();
            var invocationArgument = expression.FirstAncestorOrSelf<ArgumentSyntax>();
            var attributeArgument = expression.FirstAncestorOrSelf<AttributeArgumentSyntax>();

            var containingInvocation = invocationArgument?.Parent?.Parent as InvocationExpressionSyntax;
            var argumentList = invocationArgument?.Parent as BaseArgumentListSyntax;
            var attributeArgumentList = attributeArgument?.Parent as AttributeArgumentListSyntax;

            return new StaticSotRecord(
                file,
                fileOrder,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1,
                text,
                literalKind,
                type?.Identifier.ValueText ?? string.Empty,
                GetEnclosingMethodName(method),
                containingInvocation?.Expression.ToString(),
                invocationArgument is not null
                    ? argumentList?.Arguments.IndexOf(invocationArgument)
                    : attributeArgument is not null
                        ? attributeArgumentList?.Arguments.IndexOf(attributeArgument)
                        : null,
                invocationArgument?.NameColon?.Name.Identifier.ValueText
                    ?? attributeArgument?.NameEquals?.Name.Identifier.ValueText
                    ?? attributeArgument?.NameColon?.Name.Identifier.ValueText,
                concatParts,
                interpolationParts,
                GetAttributeContext(expression),
                expression.SpanStart);
        }

        private static string? GetEnclosingMethodName(BaseMethodDeclarationSyntax? method)
        {
            return method switch
            {
                MethodDeclarationSyntax methodDeclaration => methodDeclaration.Identifier.ValueText,
                ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
                DestructorDeclarationSyntax destructor => destructor.Identifier.ValueText,
                OperatorDeclarationSyntax @operator => @operator.OperatorToken.ValueText,
                ConversionOperatorDeclarationSyntax conversion => conversion.Type.ToString(),
                _ => null,
            };
        }

        private static string? GetAttributeContext(SyntaxNode node)
        {
            var attribute = node.FirstAncestorOrSelf<AttributeSyntax>();
            if (attribute is null)
            {
                return null;
            }

            return GetUnqualifiedName(attribute.Name);
        }

        private static string GetUnqualifiedName(NameSyntax name)
        {
            return name switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name.Identifier.ValueText,
                _ => name.ToString(),
            };
        }

        private static bool IsNestedInsideTrackedExpression(ExpressionSyntax expression)
        {
            var parentExpression = expression.Parent as ExpressionSyntax;
            while (parentExpression is not null)
            {
                if (parentExpression is InterpolatedStringExpressionSyntax)
                {
                    return true;
                }

                if (parentExpression is BinaryExpressionSyntax binaryExpression && IsStringConcatenation(binaryExpression))
                {
                    return true;
                }

                parentExpression = parentExpression.Parent as ExpressionSyntax;
            }

            return false;
        }

        private static bool IsTopLevelStringConcatenation(BinaryExpressionSyntax node)
        {
            if (!IsStringConcatenation(node))
            {
                return false;
            }

            return node.Parent is not BinaryExpressionSyntax parent || !IsStringConcatenation(parent);
        }

        private static bool IsStringConcatenation(BinaryExpressionSyntax node)
        {
            if (!node.IsKind(SyntaxKind.AddExpression))
            {
                return false;
            }

            return ContainsTrackedStringShape(node.Left) || ContainsTrackedStringShape(node.Right);
        }

        private static bool ContainsTrackedStringShape(ExpressionSyntax expression)
        {
            expression = UnwrapParentheses(expression);
            return expression switch
            {
                LiteralExpressionSyntax literal => literal.IsKind(SyntaxKind.StringLiteralExpression),
                InterpolatedStringExpressionSyntax => true,
                BinaryExpressionSyntax binary => IsStringConcatenation(binary),
                _ => false,
            };
        }

        private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
        {
            while (expression is ParenthesizedExpressionSyntax parenthesized)
            {
                expression = parenthesized.Expression;
            }

            return expression;
        }

        private static List<StaticSotPart> BuildConcatParts(ExpressionSyntax expression)
        {
            var parts = new List<StaticSotPart>();
            AppendConcatParts(UnwrapParentheses(expression), parts);
            return parts;
        }

        private static void AppendConcatParts(ExpressionSyntax expression, List<StaticSotPart> parts)
        {
            expression = UnwrapParentheses(expression);
            if (expression is BinaryExpressionSyntax binaryExpression && IsStringConcatenation(binaryExpression))
            {
                AppendConcatParts(binaryExpression.Left, parts);
                AppendConcatParts(binaryExpression.Right, parts);
                return;
            }

            if (expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                parts.Add(new StaticSotPart("literal", literal.Token.ValueText));
                return;
            }

            parts.Add(new StaticSotPart("expression", expression.ToString()));
        }

        private static List<StaticSotPart> BuildInterpolationParts(InterpolatedStringExpressionSyntax expression)
        {
            var parts = new List<StaticSotPart>();
            for (var index = 0; index < expression.Contents.Count; index++)
            {
                switch (expression.Contents[index])
                {
                    case InterpolatedStringTextSyntax text:
                        parts.Add(new StaticSotPart("text", text.TextToken.ValueText));
                        break;
                    case InterpolationSyntax interpolation:
                        parts.Add(new StaticSotPart("expression", interpolation.Expression.ToString()));
                        break;
                }
            }

            return parts;
        }
    }

    private sealed class StaticSotRecord
    {
        public StaticSotRecord(
            string file,
            int fileOrder,
            int line,
            int column,
            string text,
            string literalKind,
            string enclosingType,
            string? enclosingMethod,
            string? containingInvocation,
            int? argumentIndex,
            string? argumentName,
            List<StaticSotPart>? concatParts,
            List<StaticSotPart>? interpolationParts,
            string? attributeContext,
            int sortKey)
        {
            File = file;
            FileOrder = fileOrder;
            Line = line;
            Column = column;
            Text = text;
            LiteralKind = literalKind;
            EnclosingType = enclosingType;
            EnclosingMethod = enclosingMethod;
            ContainingInvocation = containingInvocation;
            ArgumentIndex = argumentIndex;
            ArgumentName = argumentName;
            ConcatParts = concatParts;
            InterpolationParts = interpolationParts;
            AttributeContext = attributeContext;
            SortKey = sortKey;
        }

        public string File { get; }

        public int FileOrder { get; }

        public int Line { get; }

        public int Column { get; }

        public string Text { get; }

        public string LiteralKind { get; }

        public string EnclosingType { get; }

        public string? EnclosingMethod { get; }

        public string? ContainingInvocation { get; }

        public int? ArgumentIndex { get; }

        public string? ArgumentName { get; }

        public List<StaticSotPart>? ConcatParts { get; }

        public List<StaticSotPart>? InterpolationParts { get; }

        public string? AttributeContext { get; }

        public int SortKey { get; }
    }

    private sealed class StaticSotPart
    {
        public StaticSotPart(string kind, string? text)
        {
            Kind = kind;
            Text = text;
        }

        public string Kind { get; }

        public string? Text { get; }
    }

    private sealed class StaticSotRecordComparer : IComparer<StaticSotRecord>
    {
        public static StaticSotRecordComparer Instance { get; } = new();

        public int Compare(StaticSotRecord? x, StaticSotRecord? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var fileComparison = x.FileOrder.CompareTo(y.FileOrder);
            if (fileComparison != 0)
            {
                return fileComparison;
            }

            var sortKeyComparison = x.SortKey.CompareTo(y.SortKey);
            if (sortKeyComparison != 0)
            {
                return sortKeyComparison;
            }

            return string.CompareOrdinal(x.LiteralKind, y.LiteralKind);
        }
    }
}
