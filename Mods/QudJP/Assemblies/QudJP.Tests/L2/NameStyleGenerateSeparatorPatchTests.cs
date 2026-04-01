using System.Reflection;
using HarmonyLib;
using QudJP.Patches;
using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L2;

[TestFixture]
[Category("L2")]
[NonParallelizable]
public sealed class NameStyleGenerateSeparatorPatchTests
{
    [Test]
    public void Transpiler_RewritesSingleNameHyphenSeparators()
    {
        RunWithPatch(() =>
        {
            Assert.That(DummyNameStyleTarget.Generate(), Is.EqualTo("pre・mid・post・end"));
        });
    }

    [Test]
    public void Transpiler_RewritesTwoNameSeparatorBetweenGeneratedNames()
    {
        RunWithPatch(() =>
        {
            Assert.That(DummyNameStyleTarget.Generate(twoNames: true), Is.EqualTo("pre・mid・post・end・pre・mid・post・end"));
        });
    }

    [Test]
    public void Transpiler_LeavesTemplateHandlingUntouched()
    {
        RunWithPatch(() =>
        {
            Assert.That(DummyNameStyleTarget.Generate(template: "Seeker *Name*"), Is.EqualTo("Seeker pre・mid・post・end"));
        });
    }

    private static void RunWithPatch(Action assertion)
    {
        var harmonyId = $"qudjp.tests.name-style.{Guid.NewGuid():N}";
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyNameStyleTarget), nameof(DummyNameStyleTarget.Generate), typeof(bool), typeof(string)),
                transpiler: new HarmonyMethod(RequireMethod(typeof(NameStyleGenerateSeparatorPatch), nameof(NameStyleGenerateSeparatorPatch.Transpiler))));

            assertion();
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    private static MethodInfo RequireMethod(Type type, string methodName, params Type[] parameterTypes)
    {
        var method = parameterTypes.Length == 0
            ? AccessTools.Method(type, methodName)
            : AccessTools.Method(type, methodName, parameterTypes);

        return method
               ?? throw new InvalidOperationException($"Method not found: {type.FullName}.{methodName}");
    }
}
