#if HAS_GAME_DLL
using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;
using QudJP.Patches;

#pragma warning disable S3011

namespace QudJP.Tests.L2G;

[TestFixture]
[Category("L2G")]
public sealed class XDidYTranslationPatchResolutionTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _ = EnsureGameAssemblyLoaded();
    }

    [TestCase("XDidY")]
    [TestCase("XDidYToZ")]
    [TestCase("WDidXToYWithZ")]
    public void TargetMethods_ResolveMessagingOwnerMethods(string methodName)
    {
        var targetMethods = typeof(XDidYTranslationPatch)
            .GetMethod("TargetMethods", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(targetMethods, Is.Not.Null);

        var methods = ((IEnumerable<MethodBase>)targetMethods!.Invoke(null, null)!).ToArray();

        Assert.That(
            methods.Any(method =>
                string.Equals(method.Name, methodName, StringComparison.Ordinal)
                && string.Equals(method.DeclaringType?.FullName, "XRL.World.Capabilities.Messaging", StringComparison.Ordinal)),
            Is.True,
            $"Messaging.{methodName} was not resolved by XDidYTranslationPatch.TargetMethods().");
    }

    [TestCase("One", true, false)]
    [TestCase("one", false, true)]
    public void TryBuildDisplayNameMethodArgs_AcceptsRealGameObjectDisplayNameSignatures(
        string methodName,
        bool useFullNames,
        bool indefiniteArticle)
    {
        var assembly = EnsureGameAssemblyLoaded();
        var gameObjectType = assembly.GetType("XRL.World.GameObject", throwOnError: false);
        Assert.That(gameObjectType, Is.Not.Null);

        var displayNameMethod = gameObjectType!
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(method =>
                string.Equals(method.Name, methodName, StringComparison.Ordinal)
                && method.ReturnType == typeof(string)
                && method.GetParameters().Length >= 13);

        var buildArgsMethod = typeof(XDidYTranslationPatch)
            .GetMethod("TryBuildDisplayNameMethodArgs", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(buildArgsMethod, Is.Not.Null);

        var invokeArgs = new object?[] { displayNameMethod, useFullNames, indefiniteArticle, null };
        var accepted = (bool)buildArgsMethod!.Invoke(null, invokeArgs)!;
        var builtArgs = (object?[])invokeArgs[3]!;
        var parameters = displayNameMethod.GetParameters();

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(builtArgs, Has.Length.EqualTo(parameters.Length));
            Assert.That(GetArg(parameters, builtArgs, "WithoutTitles"), Is.EqualTo(!useFullNames));
            Assert.That(GetArg(parameters, builtArgs, "Short"), Is.EqualTo(!useFullNames));
            Assert.That(GetArg(parameters, builtArgs, "WithIndefiniteArticle"), Is.EqualTo(indefiniteArticle));
        });
    }

    private static object? GetArg(ParameterInfo[] parameters, object?[] args, string name)
    {
        for (var index = 0; index < parameters.Length; index++)
        {
            if (string.Equals(parameters[index].Name, name, StringComparison.Ordinal))
            {
                return args[index];
            }
        }

        Assert.Fail($"Parameter not found: {name}");
        return null;
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
