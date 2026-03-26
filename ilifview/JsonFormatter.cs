using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ilifview;

class JsonFormatter : IOutputFormatter
{
    public void Write(AssemblyInfo assembly, TextWriter output)
    {
        var doc = new JsonAssemblyDoc
        {
            Assembly = assembly.Name,
            TargetFramework = assembly.TargetFramework,
            Namespaces = assembly.Namespaces.Select(ConvertNamespace).ToList(),
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        output.Write(JsonSerializer.Serialize(doc, options));
        output.WriteLine();
    }

    private static JsonNamespaceDoc ConvertNamespace(NamespaceInfo ns) => new()
    {
        Name = ns.Name,
        Types = ns.Types.Select(ConvertType).ToList(),
    };

    private static JsonTypeDoc ConvertType(TypeModel type) => new()
    {
        Kind = TypeKindHelper.ToKeyword(type.Kind),
        Name = FormatNameWithGenerics(type.Name, type.GenericParameters),
        Modifiers = NullIfEmpty(type.Modifiers),
        GenericParameters = NullIfEmpty(type.GenericParameters),
        BaseType = type.BaseType,
        Interfaces = NullIfEmpty(type.Interfaces),
        Constraints = NullIfEmpty(type.Constraints),
        Attributes = NullIfEmpty(type.Attributes),
        Fields = NullIfEmpty(type.Fields.Select(ConvertField).ToList()),
        Constructors = NullIfEmpty(type.Constructors.Select(ConvertConstructor).ToList()),
        Properties = NullIfEmpty(type.Properties.Select(ConvertProperty).ToList()),
        Events = NullIfEmpty(type.Events.Select(ConvertEvent).ToList()),
        Methods = NullIfEmpty(type.Methods.Select(ConvertMethod).ToList()),
        EnumMembers = NullIfEmpty(type.EnumMembers.Select(ConvertEnumMember).ToList()),
        DelegateParameters = type.DelegateInvoke?.Parameters.Select(ConvertParameter).ToList(),
        DelegateReturnType = type.DelegateInvoke?.ReturnType,
        NestedTypes = NullIfEmpty(type.NestedTypes.Select(ConvertType).ToList()),
    };

    private static JsonFieldDoc ConvertField(FieldModel f) => new()
    {
        Modifiers = f.Modifiers, Type = f.Type, Name = f.Name, Value = f.Value,
    };

    private static JsonConstructorDoc ConvertConstructor(ConstructorModel c) => new()
    {
        Modifiers = c.Modifiers,
        Parameters = NullIfEmpty(c.Parameters.Select(ConvertParameter).ToList()),
    };

    private static JsonPropertyDoc ConvertProperty(PropertyModel p) => new()
    {
        Modifiers = p.Modifiers, Type = p.Type, Name = p.Name,
        HasGet = p.HasGet ? true : null,
        HasSet = p.HasSet && !p.IsInit ? true : null,
        IsInit = p.IsInit ? true : null,
    };

    private static JsonEventDoc ConvertEvent(EventModel e) => new()
    {
        Modifiers = e.Modifiers, Type = e.Type, Name = e.Name,
    };

    private static JsonMethodDoc ConvertMethod(MethodModel m) => new()
    {
        Modifiers = NullIfEmpty(m.Modifiers),
        ReturnType = m.ReturnType,
        Name = FormatNameWithGenerics(m.Name, m.GenericParameters),
        GenericParameters = NullIfEmpty(m.GenericParameters),
        Parameters = NullIfEmpty(m.Parameters.Select(ConvertParameter).ToList()),
        Constraints = NullIfEmpty(m.Constraints),
    };

    private static JsonParameterDoc ConvertParameter(ParameterModel p) => new()
    {
        Modifier = NullIfEmpty(p.Modifier),
        Type = p.Type,
        Name = p.Name,
        DefaultValue = p.DefaultValue,
    };

    private static JsonEnumMemberDoc ConvertEnumMember(EnumMemberModel m) => new()
    {
        Name = m.Name, Value = m.Value,
    };

    private static string FormatNameWithGenerics(string name, List<string> genericParams)
    {
        if (genericParams.Count == 0) return name;
        var cleaned = genericParams.Select(g => g.Replace("in ", "").Replace("out ", ""));
        return $"{name}<{string.Join(", ", cleaned)}>";
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;
    private static List<T>? NullIfEmpty<T>(List<T> list) => list.Count == 0 ? null : list;
}

// JSON document model classes
class JsonAssemblyDoc
{
    public string Assembly { get; set; } = "";
    public string? TargetFramework { get; set; }
    public List<JsonNamespaceDoc> Namespaces { get; set; } = [];
}

class JsonNamespaceDoc
{
    public string Name { get; set; } = "";
    public List<JsonTypeDoc> Types { get; set; } = [];
}

class JsonTypeDoc
{
    public string Kind { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Modifiers { get; set; }
    public List<string>? GenericParameters { get; set; }
    public string? BaseType { get; set; }
    public List<string>? Interfaces { get; set; }
    public List<string>? Constraints { get; set; }
    public List<string>? Attributes { get; set; }
    public List<JsonFieldDoc>? Fields { get; set; }
    public List<JsonConstructorDoc>? Constructors { get; set; }
    public List<JsonPropertyDoc>? Properties { get; set; }
    public List<JsonEventDoc>? Events { get; set; }
    public List<JsonMethodDoc>? Methods { get; set; }
    public List<JsonEnumMemberDoc>? EnumMembers { get; set; }
    public string? DelegateReturnType { get; set; }
    public List<JsonParameterDoc>? DelegateParameters { get; set; }
    public List<JsonTypeDoc>? NestedTypes { get; set; }
}

class JsonFieldDoc
{
    public string Modifiers { get; set; } = "";
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Value { get; set; }
}

class JsonConstructorDoc
{
    public string Modifiers { get; set; } = "";
    public List<JsonParameterDoc>? Parameters { get; set; }
}

class JsonPropertyDoc
{
    public string Modifiers { get; set; } = "";
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public bool? HasGet { get; set; }
    public bool? HasSet { get; set; }
    public bool? IsInit { get; set; }
}

class JsonEventDoc
{
    public string Modifiers { get; set; } = "";
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
}

class JsonMethodDoc
{
    public string? Modifiers { get; set; }
    public string ReturnType { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string>? GenericParameters { get; set; }
    public List<JsonParameterDoc>? Parameters { get; set; }
    public List<string>? Constraints { get; set; }
}

class JsonParameterDoc
{
    public string? Modifier { get; set; }
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string? DefaultValue { get; set; }
}

class JsonEnumMemberDoc
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}
