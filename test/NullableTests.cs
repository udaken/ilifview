using ilifview;

namespace test;

public class NullableTests
{
    private readonly AssemblyInfo _info = TestHelper.LoadTestAssembly();

    [Fact]
    public void Property_NonNullableString_NoQuestionMark()
    {
        var type = _info.GetType("NullableDemo");
        var prop = type.Properties.First(p => p.Name == "NonNullableString");
        Assert.Equal("string", prop.Type);
    }

    [Fact]
    public void Property_NullableString_HasQuestionMark()
    {
        var type = _info.GetType("NullableDemo");
        var prop = type.Properties.First(p => p.Name == "NullableString");
        Assert.Equal("string?", prop.Type);
    }

    [Fact]
    public void Property_NonNullableInt_NoQuestionMark()
    {
        var type = _info.GetType("NullableDemo");
        var prop = type.Properties.First(p => p.Name == "NonNullableInt");
        Assert.Equal("int", prop.Type);
    }

    [Fact]
    public void Property_NullableInt_HasQuestionMark()
    {
        var type = _info.GetType("NullableDemo");
        var prop = type.Properties.First(p => p.Name == "NullableInt");
        Assert.Equal("int?", prop.Type);
    }

    [Fact]
    public void Property_NullableList_HasQuestionMarkOnList()
    {
        var type = _info.GetType("NullableDemo");
        var prop = type.Properties.First(p => p.Name == "NullableList");
        Assert.Equal("System.Collections.Generic.List<string>?", prop.Type);
    }

    [Fact]
    public void Property_ListWithNullableItems_HasQuestionMarkOnString()
    {
        var type = _info.GetType("NullableDemo");
        var prop = type.Properties.First(p => p.Name == "ListWithNullableItems");
        Assert.Equal("System.Collections.Generic.List<string?>", prop.Type);
    }

    [Fact]
    public void Property_FullyNullableList_BothQuestionMarks()
    {
        var type = _info.GetType("NullableDemo");
        var prop = type.Properties.First(p => p.Name == "FullyNullableList");
        Assert.Equal("System.Collections.Generic.List<string?>?", prop.Type);
    }

    [Fact]
    public void Property_DictWithNullableValue()
    {
        var type = _info.GetType("NullableDemo");
        var prop = type.Properties.First(p => p.Name == "DictWithNullableValue");
        Assert.Contains("string", prop.Type);
        Assert.Contains("int?", prop.Type);
    }

    [Fact]
    public void Method_NullableReturnType()
    {
        var type = _info.GetType("NullableDemo");
        var method = type.Methods.First(m => m.Name == "GetNullable");
        Assert.Equal("string?", method.ReturnType);
    }

    [Fact]
    public void Method_NonNullableReturnType()
    {
        var type = _info.GetType("NullableDemo");
        var method = type.Methods.First(m => m.Name == "GetNonNullable");
        Assert.Equal("string", method.ReturnType);
    }

    [Fact]
    public void Method_NullableParameter()
    {
        var type = _info.GetType("NullableDemo");
        var method = type.Methods.First(m => m.Name == "GetNullable");
        Assert.Equal("string?", method.Parameters[0].Type);
    }

    [Fact]
    public void Method_NonNullableParameter()
    {
        var type = _info.GetType("NullableDemo");
        var method = type.Methods.First(m => m.Name == "GetNonNullable");
        Assert.Equal("string", method.Parameters[0].Type);
    }

    [Fact]
    public void Method_OutNullableParameter()
    {
        var type = _info.GetType("NullableDemo");
        var method = type.Methods.First(m => m.Name == "TryGet");
        var outParam = method.Parameters.First(p => p.Name == "value");
        Assert.Equal("out", outParam.Modifier);
        Assert.Equal("string?", outParam.Type);
    }

    [Fact]
    public void Property_NullableArray()
    {
        var type = _info.GetType("ArrayDemo");
        var prop = type.Properties.First(p => p.Name == "NullableArray");
        Assert.Equal("string?[]?", prop.Type);
    }

    [Fact]
    public void IRepository_NullableReturn()
    {
        var type = _info.GetType("IRepository");
        var method = type.Methods.First(m => m.Name == "GetById");
        Assert.Equal("T?", method.ReturnType);
    }

    [Fact]
    public void GenericService_NullableReturn()
    {
        var type = _info.GetType("GenericService");
        var method = type.Methods.First(m => m.Name == "FindByKey");
        Assert.Equal("T?", method.ReturnType);
    }
}
