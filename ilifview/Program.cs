using System.Reflection;
using System.Runtime.InteropServices;
using ilifview;

// Parse arguments
string? dllPath = null;
string format = "csharp";
string? outputPath = null;
bool typeOnly = false;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] is "--format" or "-f")
    {
        if (i + 1 >= args.Length)
        {
            PrintUsageAndExit("Error: --format requires a value.");
            return 1;
        }
        format = args[++i].ToLowerInvariant();
    }
    else if (args[i] is "--output" or "-o")
    {
        if (i + 1 >= args.Length)
        {
            PrintUsageAndExit("Error: --output requires a value.");
            return 1;
        }
        outputPath = args[++i];
    }
    else if (args[i] is "--type-only")
    {
        typeOnly = true;
    }
    else if (args[i].StartsWith('-'))
    {
        PrintUsageAndExit($"Error: Unknown option '{args[i]}'.");
        return 1;
    }
    else
    {
        dllPath = args[i];
    }
}

if (dllPath is null)
{
    PrintUsageAndExit("Error: DLL path is required.");
    return 1;
}

if (format is not ("csharp" or "json" or "yaml"))
{
    PrintUsageAndExit($"Error: Unknown format '{format}'. Supported: csharp, json, yaml.");
    return 1;
}

if (!File.Exists(dllPath))
{
    Console.Error.WriteLine($"Error: File not found: {dllPath}");
    return 1;
}

// Setup MetadataLoadContext
try
{
    var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
    var dllDir = Path.GetDirectoryName(Path.GetFullPath(dllPath)) ?? ".";

    var assemblyPaths = Directory.GetFiles(runtimeDir, "*.dll")
        .Concat(Directory.GetFiles(dllDir, "*.dll"))
        .Distinct(StringComparer.OrdinalIgnoreCase);

    var resolver = new PathAssemblyResolver(assemblyPaths);
    using var mlc = new MetadataLoadContext(resolver);

    Assembly assembly;
    try
    {
        assembly = mlc.LoadFromAssemblyPath(Path.GetFullPath(dllPath));
    }
    catch (BadImageFormatException)
    {
        Console.Error.WriteLine($"Error: '{dllPath}' is not a valid managed .NET assembly.");
        return 1;
    }

    var model = AssemblyAnalyzer.Analyze(assembly);

    if (typeOnly)
        model = StripMembers(model);

    IOutputFormatter formatter = format switch
    {
        "json" => new JsonFormatter(),
        "yaml" => new YamlFormatter(),
        _ => new CSharpFormatter(),
    };

    if (outputPath is not null)
    {
        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
        formatter.Write(model, writer);
    }
    else
    {
        formatter.Write(model, Console.Out);
    }
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static void PrintUsageAndExit(string? error = null)
{
    if (error is not null)
        Console.Error.WriteLine(error);
    Console.Error.WriteLine();
    Console.Error.WriteLine("Usage: ilifview <DLL path> [--format csharp|json|yaml] [--output <file>] [--type-only]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Options:");
    Console.Error.WriteLine("  -f, --format  Output format (default: csharp)");
    Console.Error.WriteLine("  -o, --output  Output file path (default: stdout)");
    Console.Error.WriteLine("      --type-only  Show only type declarations without members");
}

static AssemblyInfo StripMembers(AssemblyInfo assembly) =>
    assembly with
    {
        Namespaces = assembly.Namespaces
            .Select(ns => ns with { Types = ns.Types.Select(StripTypeMembers).ToList() })
            .ToList(),
    };

static TypeModel StripTypeMembers(TypeModel type) =>
    type with
    {
        Fields = [],
        Constructors = [],
        Properties = [],
        Events = [],
        Methods = [],
        EnumMembers = [],
        DelegateInvoke = null,
        NestedTypes = type.NestedTypes.Select(StripTypeMembers).ToList(),
    };
