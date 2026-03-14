#if HAS_GAME_DLL || HAS_TMP
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using QudJP.Patches;

namespace QudJP.Tests.L2G;

[TestFixture]
[Category("L2G")]
public sealed class TargetMethodResolutionTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
#if HAS_GAME_DLL
        _ = EnsureGameAssemblyLoaded();
#endif
#if HAS_TMP
        _ = EnsureManagedAssemblyLoaded("UnityEngine.CoreModule");
        _ = EnsureManagedAssemblyLoaded("UnityEngine.TextCoreFontEngineModule");
        _ = EnsureManagedAssemblyLoaded("UnityEngine.TextCoreTextEngineModule");
        _ = EnsureManagedAssemblyLoaded("UnityEngine.TextRenderingModule");
        _ = EnsureManagedAssemblyLoaded("UnityEngine.UI");
        _ = EnsureManagedAssemblyLoaded("Unity.TextMeshPro");
#endif
    }

#if HAS_GAME_DLL
    [TestCase(typeof(GetDisplayNamePatch), "GetFor", "XRL.World.GetDisplayNameEvent", "System.String", new[]
    {
        "XRL.World.GameObject",
        "System.String",
        "System.Int32",
        "System.String",
        "System.Boolean",
        "System.Boolean",
        "System.Boolean",
        "System.Boolean",
        "System.Boolean",
        "System.Boolean",
        "System.Boolean",
        "System.Boolean",
        "System.Boolean",
        "System.Boolean",
        "System.Boolean",
        "System.Boolean",
    })]
    [TestCase(typeof(GetDisplayNameProcessPatch), "ProcessFor", "XRL.World.GetDisplayNameEvent", "System.String", new[] { "XRL.World.GameObject", "System.Boolean" })]
    [TestCase(typeof(LookTooltipContentPatch), "GenerateTooltipContent", "XRL.UI.Look", "System.String", new[] { "XRL.World.GameObject" })]
    [TestCase(typeof(DescriptionLongDescriptionPatch), "GetLongDescription", "XRL.World.Parts.Description", "System.Void", new[] { "System.Text.StringBuilder" })]
    [TestCase(typeof(UITextSkinTranslationPatch), "SetText", "XRL.UI.UITextSkin", "System.Boolean", new[] { "System.String" })]
    [TestCase(typeof(MessageLogPatch), "AddPlayerMessage", "XRL.Messages.MessageQueue", "System.Void", new[] { "System.String", "System.String", "System.Boolean" })]
    [TestCase(typeof(ConversationDisplayTextPatch), "GetDisplayText", "XRL.World.Conversations.Choice", "System.String", new[] { "System.Boolean" })]
    [TestCase(typeof(GrammarMakeAndListPatch), "MakeAndList", "XRL.Language.Grammar", "System.String", new[] { "System.Collections.Generic.IReadOnlyList`1[[System.String]]", "System.Boolean" })]
#endif
#if HAS_TMP
    [TestCase(typeof(TextMeshProUguiFontPatch), "OnEnable", "TMPro.TextMeshProUGUI", "System.Void", new string[0])]
    [TestCase(typeof(TextMeshProFontPatch), "OnEnable", "TMPro.TextMeshPro", "System.Void", new string[0])]
    [TestCase(typeof(TmpInputFieldFontPatch), "OnEnable", "TMPro.TMP_InputField", "System.Void", new string[0])]
    [TestCase(typeof(LegacyUITextFontPatch), "OnEnable", "UnityEngine.UI.Text", "System.Void", new string[0])]
#endif
    public void TargetMethod_ResolvesExpectedSignature(
        Type patchType,
        string expectedMethodName,
        string expectedDeclaringType,
        string expectedReturnType,
        string[] expectedParameterTypes)
    {
        var targetMethod = InvokeTargetMethod(patchType);

        Assert.Multiple(() =>
        {
            Assert.That(targetMethod, Is.Not.Null, $"TargetMethod returned null for {patchType.FullName}");
            Assert.That(targetMethod!.Name, Is.EqualTo(expectedMethodName));
            Assert.That(targetMethod.DeclaringType?.FullName, Is.EqualTo(expectedDeclaringType));

            var methodInfo = targetMethod as MethodInfo;
            Assert.That(methodInfo, Is.Not.Null, $"Expected MethodInfo for {patchType.FullName}");
            Assert.That(methodInfo!.ReturnType.FullName, Is.EqualTo(expectedReturnType));

            var parameterTypes = Array.ConvertAll(methodInfo.GetParameters(), static parameter => NormalizeTypeName(parameter.ParameterType.FullName));
            Assert.That(parameterTypes, Is.EqualTo(expectedParameterTypes));
        });
    }

#if HAS_GAME_DLL
    [TestCase(typeof(GrammarMakeOrListPatch), new[]
    {
        "System.String[]|System.Boolean",
        "System.Collections.Generic.List`1[[System.String]]|System.Boolean",
    })]
    [TestCase(typeof(PopupTranslationPatch), new[]
    {
        "System.String|System.String|System.String|System.Boolean|System.Boolean|System.Boolean|System.Boolean|Genkit.Location2D",
        "System.String|System.Collections.Generic.IReadOnlyList`1[[System.String]]|System.Collections.Generic.IReadOnlyList`1[[System.Char]]|System.Int32|System.String|System.Int32|System.Boolean|System.Boolean|System.Int32|System.String|System.Action`1[[System.Int32]]|XRL.World.GameObject|System.Collections.Generic.IReadOnlyList`1[[ConsoleLib.Console.IRenderable]]|ConsoleLib.Console.IRenderable|System.Collections.Generic.IReadOnlyList`1[[Qud.UI.QudMenuItem]]|System.Boolean|System.Boolean|System.Int32|System.Boolean",
    })]
    public void TargetMethods_ResolveExpectedOverloads(Type patchType, string[] expectedSignatures)
    {
        var targetMethodsMethod = patchType.GetMethod("TargetMethods", BindingFlags.NonPublic | BindingFlags.Static);
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

            var signature = string.Join("|", Array.ConvertAll(methodInfo.GetParameters(), static parameter => NormalizeTypeName(parameter.ParameterType.FullName)));
            actualSignatures.Add(signature);
        }

        Assert.That(actualSignatures, Is.EquivalentTo(expectedSignatures));
    }

    [TestCase("XRL.World.Conversations.ConversationLoader", "LoadConversations", 0)]
    [TestCase("XRL.World.Conversations.ConversationLoader", "ReadConversation", 2)]
    [TestCase("XRL.World.GameObjectFactory", "LoadBlueprints", 0)]
    [TestCase("XRL.World.GameObjectFactory", "LoadBakedXML", 1)]
    public void HookInventoryProbe_ResolvesXmlLoaderMethods(
        string declaringTypeName,
        string methodName,
        int parameterCount)
    {
        var assembly = EnsureGameAssemblyLoaded();
        var declaringType = assembly.GetType(declaringTypeName, throwOnError: false);
        Assert.That(declaringType, Is.Not.Null, $"Type not found: {declaringTypeName}");

        var method = FindMethodByNameAndParameterCount(declaringType!, methodName, parameterCount);
        Assert.That(
            method,
            Is.Not.Null,
            $"Method not found: {declaringTypeName}.{methodName} with {parameterCount} parameter(s)");
    }

    [TestCase("XRL.Messages.Messaging", "XRL.World.Messaging", "XRL.UI.Messaging")]
    [TestCase("XRL.World.Conversations.ConversationUI", "XRL.World.ConversationUI", null)]
    public void NamespaceProbe_DocumentsCurrentDecompilationGapCandidates(
        string firstCandidateTypeName,
        string secondCandidateTypeName,
        string? thirdCandidateTypeName)
    {
        string?[] candidateTypeNames =
        {
            firstCandidateTypeName,
            secondCandidateTypeName,
            thirdCandidateTypeName,
        };

        var assembly = EnsureGameAssemblyLoaded();
        var resolvedTypeName = Array.Find(
            candidateTypeNames,
            candidateTypeName => candidateTypeName is not null
                && assembly.GetType(candidateTypeName, throwOnError: false) is not null);

        Assert.That(
            resolvedTypeName,
            Is.Null,
            $"Expected current decompilation-gap candidates to remain unresolved: {string.Join(", ", candidateTypeNames)}");
    }

    [TestCase("QudGenotypeModule")]
    [TestCase("QudMutationsModule")]
    [TestCase("QudCyberneticsModule")]
    [TestCase("EmbarkBuilder")]
    public void CharGenProbe_ResolvesKnownSimpleTypeNames(string simpleTypeName)
    {
        var assembly = EnsureGameAssemblyLoaded();
        var resolvedType = FindTypeBySimpleName(assembly, simpleTypeName);

        Assert.That(resolvedType, Is.Not.Null, $"Type not found by simple name: {simpleTypeName}");
    }

    [Test]
    public void CharGenLocalizationPatch_TargetMethods_ResolveCurrentCharGenSurface()
    {
        var targetMethodsMethod = typeof(CharGenLocalizationPatch).GetMethod("TargetMethods", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(targetMethodsMethod, Is.Not.Null, "TargetMethods not found for CharGenLocalizationPatch");

        var result = targetMethodsMethod!.Invoke(null, null) as System.Collections.IEnumerable;
        Assert.That(result, Is.Not.Null, "TargetMethods returned null for CharGenLocalizationPatch");

        var foundKnownCharGenType = false;
        var foundAnyMethod = false;
        foreach (var item in result!)
        {
            if (item is not MethodInfo methodInfo)
            {
                continue;
            }

            foundAnyMethod = true;
            var declaringTypeName = methodInfo.DeclaringType?.Name;
            if (!string.IsNullOrEmpty(declaringTypeName)
                && (declaringTypeName.Contains("Embark", StringComparison.Ordinal)
                    || declaringTypeName.Contains("Genotype", StringComparison.Ordinal)
                    || declaringTypeName.Contains("Mutation", StringComparison.Ordinal)
                    || declaringTypeName.Contains("Cybernetics", StringComparison.Ordinal)
                    || declaringTypeName.Contains("CharacterCreation", StringComparison.Ordinal)
                    || declaringTypeName.Contains("Calling", StringComparison.Ordinal)))
            {
                foundKnownCharGenType = true;
                break;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(foundAnyMethod, Is.True, "CharGenLocalizationPatch resolved no target methods.");
            Assert.That(foundKnownCharGenType, Is.True, "CharGenLocalizationPatch did not resolve any char-gen-related declaring types.");
        });
    }
#endif

    private static MethodBase? InvokeTargetMethod(Type patchType)
    {
        var targetMethod = patchType.GetMethod("TargetMethod", BindingFlags.NonPublic | BindingFlags.Static);
        return targetMethod?.Invoke(null, null) as MethodBase;
    }

    private static MethodBase? FindMethodByNameAndParameterCount(
        Type declaringType,
        string methodName,
        int parameterCount)
    {
        var methods = declaringType.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        for (var index = 0; index < methods.Length; index++)
        {
            var method = methods[index];
            if (method.Name == methodName && method.GetParameters().Length == parameterCount)
            {
                return method;
            }
        }

        return null;
    }

    private static Type? FindTypeBySimpleName(Assembly assembly, string simpleTypeName)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = Array.FindAll(ex.Types, static type => type is not null)!;
        }

        for (var index = 0; index < types.Length; index++)
        {
            if (types[index].Name == simpleTypeName)
            {
                return types[index];
            }
        }

        return null;
    }

    // Regex: strip assembly-qualified parts from generic type args
    // "List`1[[System.String, System.Private.CoreLib, Version=...]]" → "List`1[[System.String]]"
    private static string NormalizeTypeName(string? typeName)
    {
        if (typeName is null)
        {
            return string.Empty;
        }

        return Regex.Replace(typeName, @",\s*[^\[\],]+,\s*Version=[^\]]+", string.Empty);
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

#if HAS_GAME_DLL
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
#endif

#if HAS_TMP
    private static Assembly EnsureManagedAssemblyLoaded(string assemblyName)
    {
        var loadedAssembly = Array.Find(
            AppDomain.CurrentDomain.GetAssemblies(),
            assembly => string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal));
        if (loadedAssembly is not null)
        {
            return loadedAssembly;
        }

        var managedDir = ResolveManagedDirectory();
        var assemblyPath = Path.Combine(managedDir, assemblyName + ".dll");

        Assert.That(File.Exists(assemblyPath), Is.True, $"{assemblyName}.dll not found at {assemblyPath}");
        return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
    }
#endif
}
#endif
