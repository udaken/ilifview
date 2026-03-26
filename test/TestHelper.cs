using System.Reflection;
using System.Runtime.InteropServices;
using ilifview;

namespace test;

internal static class TestHelper
{
    private static AssemblyInfo? _cached;

    public static string TestAssemblyPath =>
        Path.Combine(AppContext.BaseDirectory, "test-assembly.dll");

    public static AssemblyInfo LoadTestAssembly()
    {
        if (_cached is not null)
            return _cached;

        var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        var dllDir = Path.GetDirectoryName(Path.GetFullPath(TestAssemblyPath))!;

        var assemblyPaths = Directory.GetFiles(runtimeDir, "*.dll")
            .Concat(Directory.GetFiles(dllDir, "*.dll"))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var resolver = new PathAssemblyResolver(assemblyPaths);
        using var mlc = new MetadataLoadContext(resolver);
        var assembly = mlc.LoadFromAssemblyPath(Path.GetFullPath(TestAssemblyPath));

        _cached = AssemblyAnalyzer.Analyze(assembly);
        return _cached;
    }

    public static NamespaceInfo GetNamespace(this AssemblyInfo info, string name = "TestAssembly") =>
        info.Namespaces.First(ns => ns.Name == name);

    public static TypeModel GetType(this AssemblyInfo info, string name, string ns = "TestAssembly") =>
        info.GetNamespace(ns).Types.First(t => t.Name == name);

    public static string RunFormatter(IOutputFormatter formatter, AssemblyInfo assembly)
    {
        using var writer = new StringWriter();
        formatter.Write(assembly, writer);
        return writer.ToString();
    }
}
