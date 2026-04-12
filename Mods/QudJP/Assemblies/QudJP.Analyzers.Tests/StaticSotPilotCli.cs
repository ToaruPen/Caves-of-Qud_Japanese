using System;
using System.IO;
using System.Text;
using QudJP.Analyzers;

namespace QudJP.Analyzers.Tests;

public static class StaticSotPilotCli
{
    public static int Run(string[] args)
    {
        var sourceRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dev", "coq-decompiled_stable");
        string? outputJsonlPath = null;

        for (var index = 0; index < args.Length; index++)
        {
            if (string.Equals(args[index], "--source-root", StringComparison.Ordinal))
            {
                if (index + 1 >= args.Length)
                {
                    Console.Error.WriteLine("Missing value for --source-root.");
                    return 1;
                }

                sourceRoot = args[++index];
            }
            else if (string.Equals(args[index], "--output-jsonl", StringComparison.Ordinal))
            {
                if (index + 1 >= args.Length)
                {
                    Console.Error.WriteLine("Missing value for --output-jsonl.");
                    return 1;
                }

                outputJsonlPath = args[++index];
            }
            else
            {
                Console.Error.WriteLine($"Unknown argument: {args[index]}");
                return 1;
            }
        }

        if (string.IsNullOrWhiteSpace(outputJsonlPath))
        {
            Console.Error.WriteLine("The --output-jsonl option is required.");
            return 1;
        }

        var sourceTexts = new string[StaticSotPilot.RequiredRelativePaths.Length];
        for (var index = 0; index < StaticSotPilot.RequiredRelativePaths.Length; index++)
        {
            var relativePath = StaticSotPilot.RequiredRelativePaths[index];
            var fullPath = Path.Combine(sourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                sourceTexts[index] = File.ReadAllText(fullPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                Console.Error.WriteLine($"Failed to read pilot input '{fullPath}': {ex.Message}");
                return 1;
            }
        }

        var jsonLines = StaticSotPilot.GenerateJsonLines(StaticSotPilot.RequiredRelativePaths, sourceTexts);
        var outputDirectory = Path.GetDirectoryName(outputJsonlPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        using var stream = new FileStream(outputJsonlPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        for (var index = 0; index < jsonLines.Length; index++)
        {
            writer.Write(jsonLines[index]);
            writer.Write('\n');
        }

        return 0;
    }
}
