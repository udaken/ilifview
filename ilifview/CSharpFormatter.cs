namespace ilifview;

class CSharpFormatter : IOutputFormatter
{
    public void Write(AssemblyInfo assembly, TextWriter output)
    {
        output.WriteLine($"// Assembly: {assembly.Name}");
        if (assembly.TargetFramework is not null)
            output.WriteLine($"// TargetFramework: {assembly.TargetFramework}");
        output.WriteLine();

        for (int i = 0; i < assembly.Namespaces.Count; i++)
        {
            var ns = assembly.Namespaces[i];
            if (i > 0) output.WriteLine();

            output.WriteLine($"namespace {ns.Name}");
            output.WriteLine("{");

            for (int j = 0; j < ns.Types.Count; j++)
            {
                if (j > 0) output.WriteLine();
                WriteType(ns.Types[j], output, "    ");
            }

            output.WriteLine("}");
        }
    }

    private static void WriteType(TypeModel type, TextWriter output, string indent)
    {
        foreach (var attr in type.Attributes)
            output.WriteLine($"{indent}{attr}");

        if (type.Kind == TypeKind.Delegate)
        {
            WriteDelegate(type, output, indent);
            return;
        }

        if (type.Kind == TypeKind.Enum)
        {
            WriteEnum(type, output, indent);
            return;
        }

        var keyword = TypeKindHelper.ToKeyword(type.Kind);
        var genericSuffix = type.GenericParameters.Count > 0
            ? $"<{string.Join(", ", type.GenericParameters)}>"
            : "";

        var inheritance = new List<string>();
        if (type.BaseType is not null)
            inheritance.Add(type.BaseType);
        inheritance.AddRange(type.Interfaces);

        var inheritanceStr = inheritance.Count > 0 ? $" : {string.Join(", ", inheritance)}" : "";
        output.WriteLine($"{indent}{type.Modifiers} {keyword} {type.Name}{genericSuffix}{inheritanceStr}");

        foreach (var constraint in type.Constraints)
            output.WriteLine($"{indent}    {constraint}");

        output.WriteLine($"{indent}{{");

        var memberIndent = indent + "    ";
        bool needsBlank = false;

        needsBlank = WriteMembers(type.Fields, output, memberIndent, needsBlank, WriteField);
        needsBlank = WriteMembers(type.Constructors, output, memberIndent, needsBlank, WriteConstructor);
        needsBlank = WriteMembers(type.Properties, output, memberIndent, needsBlank, WriteProperty);
        needsBlank = WriteMembers(type.Events, output, memberIndent, needsBlank, WriteEvent);
        needsBlank = WriteMembers(type.Methods, output, memberIndent, needsBlank, WriteMethod);

        foreach (var nested in type.NestedTypes)
        {
            if (needsBlank) output.WriteLine();
            WriteType(nested, output, memberIndent);
            needsBlank = true;
        }

        output.WriteLine($"{indent}}}");
    }

    private static bool WriteMembers<T>(List<T> members, TextWriter output, string indent, bool needsBlank, Action<T, TextWriter, string> writer)
    {
        if (members.Count == 0) return needsBlank;
        if (needsBlank) output.WriteLine();
        foreach (var m in members)
            writer(m, output, indent);
        return true;
    }

    private static void WriteDelegate(TypeModel type, TextWriter output, string indent)
    {
        var invoke = type.DelegateInvoke;
        if (invoke is null) return;

        var genericSuffix = type.GenericParameters.Count > 0
            ? $"<{string.Join(", ", type.GenericParameters)}>"
            : "";

        var parameters = FormatParameters(invoke.Parameters);
        output.Write($"{indent}{type.Modifiers} delegate {invoke.ReturnType} {type.Name}{genericSuffix}({parameters})");

        foreach (var constraint in type.Constraints)
            output.Write($" {constraint}");

        output.WriteLine(";");
    }

    private static void WriteEnum(TypeModel type, TextWriter output, string indent)
    {
        var baseType = type.BaseType is not null ? $" : {type.BaseType}" : "";
        output.WriteLine($"{indent}{type.Modifiers} enum {type.Name}{baseType}");
        output.WriteLine($"{indent}{{");

        foreach (var member in type.EnumMembers)
            output.WriteLine($"{indent}    {member.Name} = {member.Value},");

        output.WriteLine($"{indent}}}");
    }

    private static void WriteField(FieldModel field, TextWriter output, string indent)
    {
        var value = field.Value is not null ? $" = {field.Value}" : "";
        output.WriteLine($"{indent}{field.Modifiers} {field.Type} {field.Name}{value};");
    }

    private static void WriteConstructor(ConstructorModel ctor, TextWriter output, string indent)
    {
        var parameters = FormatParameters(ctor.Parameters);
        output.WriteLine($"{indent}{ctor.Modifiers} {ctor.TypeName}({parameters});");
    }

    private static void WriteProperty(PropertyModel prop, TextWriter output, string indent)
    {
        var accessors = new List<string>();
        if (prop.HasGet) accessors.Add("get;");
        if (prop.IsInit) accessors.Add("init;");
        else if (prop.HasSet) accessors.Add("set;");

        output.WriteLine($"{indent}{prop.Modifiers} {prop.Type} {prop.Name} {{ {string.Join(" ", accessors)} }}");
    }

    private static void WriteEvent(EventModel evt, TextWriter output, string indent)
    {
        output.WriteLine($"{indent}{evt.Modifiers} event {evt.Type} {evt.Name};");
    }

    private static void WriteMethod(MethodModel method, TextWriter output, string indent)
    {
        var genericSuffix = method.GenericParameters.Count > 0
            ? $"<{string.Join(", ", method.GenericParameters)}>"
            : "";
        var parameters = FormatParameters(method.Parameters);
        output.Write($"{indent}{method.Modifiers} {method.ReturnType} {method.Name}{genericSuffix}({parameters})");

        foreach (var constraint in method.Constraints)
            output.Write($" {constraint}");

        output.WriteLine(";");
    }

    private static string FormatParameters(List<ParameterModel> parameters)
    {
        return string.Join(", ", parameters.Select(p =>
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(p.Modifier)) parts.Add(p.Modifier);
            parts.Add(p.Type);
            parts.Add(p.Name);
            var result = string.Join(' ', parts);
            if (p.DefaultValue is not null)
                result += $" = {p.DefaultValue}";
            return result;
        }));
    }
}
