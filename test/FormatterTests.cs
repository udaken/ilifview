using ilifview;

namespace test;

public class FormatterTests
{
    private readonly AssemblyInfo _info = TestHelper.LoadTestAssembly();

    // --- CSharpFormatter ---

    [Fact]
    public void CSharp_ContainsAssemblyHeader()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        Assert.Contains("// Assembly: test-assembly", output);
    }

    [Fact]
    public void CSharp_ContainsTargetFramework()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        Assert.Contains("// TargetFramework:", output);
    }

    [Fact]
    public void CSharp_ContainsNamespace()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        Assert.Contains("namespace TestAssembly", output);
    }

    [Fact]
    public void CSharp_ContainsClassDeclaration()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        Assert.Contains("public class SimpleClass", output);
        Assert.Contains("public abstract class AbstractClass", output);
        Assert.Contains("public sealed class SealedClass", output);
        Assert.Contains("public static class StaticClass", output);
    }

    [Fact]
    public void CSharp_ContainsStructDeclaration()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        Assert.Contains("public struct SimpleStruct", output);
    }

    [Fact]
    public void CSharp_ContainsRecordDeclaration()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        Assert.Contains("public record SimpleRecord", output);
        Assert.Contains("public record struct RecordPoint", output);
    }

    [Fact]
    public void CSharp_ContainsEnumWithValues()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        Assert.Contains("public enum Color", output);
        Assert.Contains("Red = 0,", output);
        Assert.Contains("Green = 1,", output);
        Assert.Contains("Blue = 2,", output);
    }

    [Fact]
    public void CSharp_ContainsFlagsEnum()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        Assert.Contains("[System.Flags]", output);
        Assert.Contains("public enum Permissions : byte", output);
    }

    [Fact]
    public void CSharp_ContainsDelegateDeclaration()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        Assert.Contains("public delegate void SimpleDelegate();", output);
    }

    [Fact]
    public void CSharp_ContainsInterfaceWithConstraint()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        Assert.Contains("public interface IRepository<T>", output);
        Assert.Contains("where T : class", output);
    }

    [Fact]
    public void CSharp_ContainsPropertyAccessors()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        Assert.Contains("{ get; }", output);
        Assert.Contains("{ get; set; }", output);
        Assert.Contains("{ get; init; }", output);
    }

    [Fact]
    public void CSharp_ContainsNullableAnnotations()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        Assert.Contains("string? NullableString", output);
        Assert.Contains("string NonNullableString", output);
    }

    [Fact]
    public void CSharp_ContainsNestedType()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        Assert.Contains("public class InnerClass", output);
    }

    [Fact]
    public void CSharp_ContainsObsoleteAttribute()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        Assert.Contains("[System.Obsolete(\"Use NewService instead\")]", output);
    }

    [Fact]
    public void CSharp_NoNullableContextAttributes()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        Assert.DoesNotContain("NullableContext", output);
        Assert.DoesNotContain("NullablePublicOnly", output);
    }

    [Fact]
    public void CSharp_NoCompilerGeneratedMembers()
    {
        var output = TestHelper.RunFormatter(new CSharpFormatter(), _info);
        // No lines should contain < or $ as part of a member name
        // (generic <T> is fine, but <Clone>$ is not)
        Assert.DoesNotContain("<Clone>$", output);
    }

    // --- JsonFormatter ---

    [Fact]
    public void Json_IsValidStructure()
    {
        var output = TestHelper.RunFormatter(new JsonFormatter(), _info);
        Assert.StartsWith("{", output.TrimStart());
        Assert.Contains("\"assembly\"", output);
        Assert.Contains("\"targetFramework\"", output);
        Assert.Contains("\"namespaces\"", output);
    }

    [Fact]
    public void Json_ContainsTypes()
    {
        var output = TestHelper.RunFormatter(new JsonFormatter(), _info);
        Assert.Contains("\"SimpleClass\"", output);
        Assert.Contains("\"kind\": \"class\"", output);
        Assert.Contains("\"kind\": \"enum\"", output);
        Assert.Contains("\"kind\": \"interface\"", output);
        Assert.Contains("\"kind\": \"delegate\"", output);
    }

    [Fact]
    public void Json_ContainsEnumMembers()
    {
        var output = TestHelper.RunFormatter(new JsonFormatter(), _info);
        Assert.Contains("\"enumMembers\"", output);
        Assert.Contains("\"Red\"", output);
    }

    // --- YamlFormatter ---

    [Fact]
    public void Yaml_HasCorrectHeader()
    {
        var output = TestHelper.RunFormatter(new YamlFormatter(), _info);
        Assert.StartsWith("assembly: test-assembly", output);
        Assert.Contains("targetFramework:", output);
        Assert.Contains("namespaces:", output);
    }

    [Fact]
    public void Yaml_ContainsTypes()
    {
        var output = TestHelper.RunFormatter(new YamlFormatter(), _info);
        Assert.Contains("kind: class", output);
        Assert.Contains("kind: enum", output);
        Assert.Contains("kind: interface", output);
        Assert.Contains("kind: delegate", output);
        Assert.Contains("kind: struct", output);
        Assert.Contains("kind: record", output);
    }

    [Fact]
    public void Yaml_ContainsMembers()
    {
        var output = TestHelper.RunFormatter(new YamlFormatter(), _info);
        Assert.Contains("methods:", output);
        Assert.Contains("properties:", output);
        Assert.Contains("members:", output); // enum members
    }
}
