#if HAS_GAME_DLL
using System.Reflection;
using System.Runtime.Loader;

namespace QudJP.Tests.L2G;

[TestFixture]
[Category("L2G")]
public sealed class CharGenProducerTranslationPatchResolutionTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _ = EnsureGameAssemblyLoaded();
    }

    [Test]
    public void BreadcrumbPatch_TargetMethods_ResolveExpectedOverrides()
    {
        AssertTargetMethods(
            "QudJP.Patches.CharGenBreadcrumbTranslationPatch",
            new[]
            {
                "XRL.CharacterBuilds.Qud.UI.QudAttributesModuleWindow|GetBreadcrumb|",
                "XRL.CharacterBuilds.Qud.UI.QudBuildLibraryModuleWindow|GetBreadcrumb|",
                "XRL.CharacterBuilds.Qud.UI.QudBuildSummaryModuleWindow|GetBreadcrumb|",
                "XRL.CharacterBuilds.Qud.UI.QudChartypeModuleWindow|GetBreadcrumb|",
                "XRL.CharacterBuilds.Qud.UI.QudChooseStartingLocationModuleWindow|GetBreadcrumb|",
                "XRL.CharacterBuilds.Qud.UI.QudCustomizeCharacterModuleWindow|GetBreadcrumb|",
                "XRL.CharacterBuilds.Qud.UI.QudCyberneticsModuleWindow|GetBreadcrumb|",
                "XRL.CharacterBuilds.Qud.UI.QudGamemodeModuleWindow|GetBreadcrumb|",
                "XRL.CharacterBuilds.Qud.UI.QudGenotypeModuleWindow|GetBreadcrumb|",
                "XRL.CharacterBuilds.Qud.UI.QudMutationsModuleWindow|GetBreadcrumb|",
                "XRL.CharacterBuilds.Qud.UI.QudPregenModuleWindow|GetBreadcrumb|",
                "XRL.CharacterBuilds.Qud.UI.QudSubtypeModuleWindow|GetBreadcrumb|",
            });
    }

    [Test]
    public void MenuOptionPatch_TargetMethods_ResolveExpectedOverrides()
    {
        AssertTargetMethods(
            "QudJP.Patches.CharGenMenuOptionTranslationPatch",
            new[]
            {
                "XRL.CharacterBuilds.Qud.UI.QudAttributesModuleWindow|GetKeyMenuBar|",
                "XRL.CharacterBuilds.Qud.UI.QudBuildLibraryModuleWindow|GetKeyMenuBar|",
                "XRL.CharacterBuilds.Qud.UI.QudBuildSummaryModuleWindow|GetKeyMenuBar|",
                "XRL.CharacterBuilds.Qud.UI.QudGamemodeModuleWindow|GetKeyMenuBar|",
                "XRL.CharacterBuilds.Qud.UI.QudMutationsModuleWindow|GetKeyMenuBar|",
            });
    }

    [Test]
    public void ChromePatch_TargetMethods_ResolveFrameworkHooks()
    {
        AssertTargetMethods(
            "QudJP.Patches.CharGenChromeTranslationPatch",
            new[]
            {
                "XRL.UI.Framework.FrameworkScroller|BeforeShow|XRL.CharacterBuilds.EmbarkBuilderModuleWindowDescriptor|System.Collections.Generic.IEnumerable`1[[XRL.UI.Framework.FrameworkDataElement]]",
                "XRL.UI.Framework.CategoryMenuController|setData|XRL.UI.Framework.FrameworkDataElement",
            });
    }

    private static void AssertTargetMethods(string patchTypeName, string[] expectedSignatures)
    {
        var patchType = typeof(Translator).Assembly.GetType(patchTypeName, throwOnError: false);
        Assert.That(patchType, Is.Not.Null, $"Patch type not found: {patchTypeName}");

        var targetMethodsMethod = patchType!.GetMethod("TargetMethods", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(targetMethodsMethod, Is.Not.Null, $"TargetMethods not found for {patchType.FullName}");

        var result = targetMethodsMethod!.Invoke(null, null) as System.Collections.IEnumerable;
        Assert.That(result, Is.Not.Null, $"TargetMethods returned null for {patchType.FullName}");

        var actualSignatures = new List<string>();
        foreach (var item in result!)
        {
            if (item is not MethodInfo methodInfo)
            {
                continue;
            }

            var signature = methodInfo.DeclaringType!.FullName + "|" + methodInfo.Name + "|" + string.Join(
                "|",
                Array.ConvertAll(methodInfo.GetParameters(), static parameter => NormalizeTypeName(parameter.ParameterType.FullName)));
            actualSignatures.Add(signature);
        }

        Assert.That(actualSignatures, Is.EquivalentTo(expectedSignatures));
    }

    private static string NormalizeTypeName(string? typeName)
    {
        if (typeName is null)
        {
            return string.Empty;
        }

        return System.Text.RegularExpressions.Regex.Replace(typeName, @",\s*[^\[\],]+,\s*Version=[^\]]+", string.Empty);
    }

    private static string ResolveManagedDirectory()
    {
        var envDir = Environment.GetEnvironmentVariable("COQ_MANAGED_DIR");
        if (!string.IsNullOrWhiteSpace(envDir))
        {
            return envDir;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var defaultDir = Path.Combine(
            home,
            "Library/Application Support/Steam/steamapps/common/Caves of Qud/CoQ.app/Contents/Resources/Data/Managed");

        if (Directory.Exists(defaultDir))
        {
            return defaultDir;
        }

        Assert.Ignore("Game managed directory not found. Set COQ_MANAGED_DIR to run game-DLL-backed tests.");
        return string.Empty;
    }

    private static Assembly EnsureGameAssemblyLoaded()
    {
        var loadedAssembly = Array.Find(
            AppDomain.CurrentDomain.GetAssemblies(),
            static assembly => string.Equals(assembly.GetName().Name, "Assembly-CSharp", StringComparison.Ordinal));
        if (loadedAssembly is not null)
        {
            return loadedAssembly;
        }

        var managedDir = ResolveManagedDirectory();
        var assemblyPath = Path.Combine(managedDir, "Assembly-CSharp.dll");

        Assert.That(File.Exists(assemblyPath), Is.True, $"Assembly-CSharp.dll not found at {assemblyPath}");
        loadedAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        Assert.That(loadedAssembly.GetType("XRL.World.GameObject", throwOnError: false), Is.Not.Null);
        return loadedAssembly;
    }
}
#endif
