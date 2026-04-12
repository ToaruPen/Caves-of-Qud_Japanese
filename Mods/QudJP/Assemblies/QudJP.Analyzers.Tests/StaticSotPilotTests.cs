using System;
using System.IO;
using NUnit.Framework;
using QudJP.Analyzers;

namespace QudJP.Analyzers.Tests;

[TestFixture]
public sealed class StaticSotPilotTests
{
    private const string ExpectedJsonLines = """
{"file":"XRL.World.Effects/Prone.cs","line":3,"col":8,"text":"alpha","literal_kind":"literal","enclosing_type":"Prone","enclosing_method":null,"containing_invocation":null,"argument_index":0,"argument_name":null,"concat_parts":null,"interpolation_parts":null,"attribute_context":"Label"}
{"file":"XRL.World.Effects/Prone.cs","line":8,"col":22,"text":"named","literal_kind":"literal","enclosing_type":"Prone","enclosing_method":"Build","containing_invocation":"Log","argument_index":0,"argument_name":"message","concat_parts":null,"interpolation_parts":null,"attribute_context":null}
{"file":"XRL.World.Effects/Prone.cs","line":9,"col":16,"text":"\"prefix \" + value + \" suffix\"","literal_kind":"concatenation","enclosing_type":"Prone","enclosing_method":"Build","containing_invocation":null,"argument_index":null,"argument_name":null,"concat_parts":[{"kind":"literal","text":"prefix "},{"kind":"expression","text":"value"},{"kind":"literal","text":" suffix"}],"interpolation_parts":null,"attribute_context":null}
{"file":"XRL.World.Capabilities/Firefighting.cs","line":7,"col":16,"text":"$\"hello {name}\"","literal_kind":"interpolation","enclosing_type":"Firefighting","enclosing_method":"Describe","containing_invocation":null,"argument_index":null,"argument_name":null,"concat_parts":null,"interpolation_parts":[{"kind":"text","text":"hello "},{"kind":"expression","text":"name"}],"attribute_context":null}
{"file":"XRL.World.Effects/HolographicBleeding.cs","line":7,"col":16,"text":"done","literal_kind":"literal","enclosing_type":"HolographicBleeding","enclosing_method":"Stop","containing_invocation":null,"argument_index":null,"argument_name":null,"concat_parts":null,"interpolation_parts":null,"attribute_context":null}
{"file":"XRL.World.Parts.Mutation/ElectricalGeneration.cs","line":10,"col":24,"text":"charge ","literal_kind":"literal","enclosing_type":"ElectricalGeneration","enclosing_method":"Build","containing_invocation":"builder.Append","argument_index":0,"argument_name":null,"concat_parts":null,"interpolation_parts":null,"attribute_context":null}
{"file":"XRL.World.Parts.Mutation/ElectricalGeneration.cs","line":11,"col":24,"text":"units","literal_kind":"literal","enclosing_type":"ElectricalGeneration","enclosing_method":"Build","containing_invocation":"builder.Append","argument_index":0,"argument_name":null,"concat_parts":null,"interpolation_parts":null,"attribute_context":null}
""";

    [Test]
    public void WriteJsonLines_EmitsStableSchemaAndPreservesStringShape()
    {
        using var workspace = new TemporaryDirectory();
        WritePilotInputs(workspace.Path);

        var outputPath = Path.Combine(workspace.Path, "pilot.jsonl");
        InvokePilot(workspace.Path, outputPath);

        var actual = NormalizeLineEndings(File.ReadAllText(outputPath));
        Assert.That(actual, Is.EqualTo(NormalizeLineEndings(ExpectedJsonLines + "\n")));
    }

    [Test]
    public void WriteJsonLines_IsDeterministicAcrossRuns()
    {
        using var workspace = new TemporaryDirectory();
        WritePilotInputs(workspace.Path);

        var firstOutputPath = Path.Combine(workspace.Path, "first.jsonl");
        var secondOutputPath = Path.Combine(workspace.Path, "second.jsonl");

        InvokePilot(workspace.Path, firstOutputPath);
        InvokePilot(workspace.Path, secondOutputPath);

        var firstBytes = File.ReadAllBytes(firstOutputPath);
        var secondBytes = File.ReadAllBytes(secondOutputPath);

        Assert.That(secondBytes, Is.EqualTo(firstBytes));
    }

    [Test]
    public void StaticSotPilotCli_RunWritesJsonlToRequestedPath()
    {
        using var workspace = new TemporaryDirectory();
        WritePilotInputs(workspace.Path);

        var outputPath = Path.Combine(workspace.Path, "cli-output.jsonl");
        var result = StaticSotPilotCli.Run(["--source-root", workspace.Path, "--output-jsonl", outputPath]);
        Assert.That(result, Is.EqualTo(0));
        Assert.That(File.Exists(outputPath), Is.True);
        Assert.That(NormalizeLineEndings(File.ReadAllText(outputPath)), Is.EqualTo(NormalizeLineEndings(ExpectedJsonLines + "\n")));
    }

    private static void InvokePilot(string sourceRoot, string outputJsonlPath)
    {
        var sourceTexts = new string[StaticSotPilot.RequiredRelativePaths.Length];
        for (var index = 0; index < StaticSotPilot.RequiredRelativePaths.Length; index++)
        {
            var relativePath = StaticSotPilot.RequiredRelativePaths[index];
            var fullPath = Path.Combine(sourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            sourceTexts[index] = File.ReadAllText(fullPath);
        }

        var lines = StaticSotPilot.GenerateJsonLines(StaticSotPilot.RequiredRelativePaths, sourceTexts);
        var outputDirectory = Path.GetDirectoryName(outputJsonlPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var contents = lines.Length == 0
            ? string.Empty
            : string.Join("\n", lines) + "\n";
        File.WriteAllText(outputJsonlPath, contents);
    }

    private static void WritePilotInputs(string sourceRoot)
    {
        WritePilotFile(
            sourceRoot,
            StaticSotPilot.RequiredRelativePaths[0],
            """
namespace XRL.World.Effects;

[Label("alpha")]
public class Prone
{
    public string Build(string value)
    {
        Log(message: "named");
        return "prefix " + value + " suffix";
    }
}
""");

        WritePilotFile(
            sourceRoot,
            StaticSotPilot.RequiredRelativePaths[1],
            """
namespace XRL.World.Capabilities;

public static class Firefighting
{
    public static string Describe(string name)
    {
        return $"hello {name}";
    }
}
""");

        WritePilotFile(
            sourceRoot,
            StaticSotPilot.RequiredRelativePaths[2],
            """
namespace XRL.World.Effects;

public class HolographicBleeding
{
    public string Stop()
    {
        return "done";
    }
}
""");

        WritePilotFile(
            sourceRoot,
            StaticSotPilot.RequiredRelativePaths[3],
            """
using System.Text;

namespace XRL.World.Parts.Mutation;

public class ElectricalGeneration
{
    public string Build(int amount)
    {
        var builder = new StringBuilder();
        builder.Append("charge ");
        builder.Append("units");
        return builder.ToString();
    }
}
""");
    }

    private static void WritePilotFile(string sourceRoot, string relativePath, string contents)
    {
        var fullPath = Path.Combine(sourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(fullPath);
        if (directory is null)
        {
            throw new AssertionException($"Could not resolve parent directory for '{relativePath}'.");
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(fullPath, NormalizeLineEndings(contents, Environment.NewLine));
    }

    private static string NormalizeLineEndings(string text, string replacement = "\n")
    {
        return text.Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\n", replacement);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
