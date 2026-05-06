using System.Text;
using System.Text.Json;

namespace QudJP.Tools.RoslynSemanticProbe;

internal static class Program
{
    public static int Main(string[] args)
    {
        var options = CliOptions.Parse(args);
        if (options.ShowHelp)
        {
            Console.Out.WriteLine(CliOptions.HelpText);
            return 0;
        }

        if (options.Error is not null)
        {
            Console.Error.WriteLine(options.Error);
            Console.Error.WriteLine(CliOptions.HelpText);
            return 2;
        }

        if (!Directory.Exists(options.SourceRoot))
        {
            Console.Error.WriteLine($"source root does not exist: {options.SourceRoot}");
            return 1;
        }

        var result = SemanticProbe.Run(options);
        var json = JsonSerializer.Serialize(result, ProbeJsonContext.Default.ProbeResult) + "\n";
        if (options.OutputPath is null)
        {
            Console.Out.Write(json);
        }
        else
        {
            var directory = Path.GetDirectoryName(options.OutputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(options.OutputPath, json, new UTF8Encoding(false));
        }

        return 0;
    }
}
