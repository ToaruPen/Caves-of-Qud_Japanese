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

        return runtimeReferencePaths
            .Concat(externalReferencePaths)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();
    }
}
