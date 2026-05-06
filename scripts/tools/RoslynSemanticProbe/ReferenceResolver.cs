using Microsoft.CodeAnalysis;

namespace QudJP.Tools.RoslynSemanticProbe;

internal static class ReferenceResolver
{
    public static IReadOnlyList<MetadataReference> Resolve(IReadOnlyList<string> externalReferencePaths)
    {
        var trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        var runtimeReferencePaths = trustedPlatformAssemblies is null
            ? AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                .Select(assembly => assembly.Location)
            : trustedPlatformAssemblies.Split(Path.PathSeparator);

        var references = new List<MetadataReference>();
        foreach (var path in runtimeReferencePaths
                     .Concat(externalReferencePaths)
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.Ordinal)
                     .Order(StringComparer.Ordinal))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
            catch (Exception exception) when (IsReferenceLoadFailure(exception))
            {
                continue;
            }
        }

        return references;
    }

    private static bool IsReferenceLoadFailure(Exception exception) =>
        exception is ArgumentException
            or BadImageFormatException
            or IOException
            or NotSupportedException
            or UnauthorizedAccessException;
}
