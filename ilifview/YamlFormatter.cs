namespace ilifview;

class YamlFormatter : IOutputFormatter
{
    public void Write(AssemblyInfo assembly, TextWriter output)
    {
        output.WriteLine($"assembly: {assembly.Name}");
        if (assembly.TargetFramework is not null)
            output.WriteLine($"targetFramework: {assembly.TargetFramework}");
        output.WriteLine("namespaces:");

        foreach (var ns in assembly.Namespaces)
        {
            output.WriteLine($"  - name: {ns.Name}");
            if (ns.Types.Count == 0) continue;

            output.WriteLine("    types:");
            foreach (var type in ns.Types)
                WriteType(type, output, "      ");
        }
    }

    private static void WriteType(TypeModel type, TextWriter output, string indent)
    {
        output.WriteLine($"{indent}- kind: {TypeKindHelper.ToKeyword(type.Kind)}");
        output.WriteLine($"{indent}  name: {Quote(FormatNameWithGenerics(type.Name, type.GenericParameters))}");

        if (!string.IsNullOrEmpty(type.Modifiers))
            output.WriteLine($"{indent}  modifiers: {Quote(type.Modifiers)}");

        WriteStringList("genericParameters", type.GenericParameters, output, indent + "  ");

        if (type.BaseType is not null)
            output.WriteLine($"{indent}  baseType: {Quote(type.BaseType)}");

        WriteStringList("interfaces", type.Interfaces, output, indent + "  ");
        WriteStringList("constraints", type.Constraints, output, indent + "  ");
        WriteStringList("attributes", type.Attributes, output, indent + "  ");

        if (type.Kind == TypeKind.Delegate && type.DelegateInvoke is not null)
        {
            output.WriteLine($"{indent}  returnType: {Quote(type.DelegateInvoke.ReturnType)}");
            if (type.DelegateInvoke.Parameters.Count > 0)
            {
                output.WriteLine($"{indent}  parameters:");
                foreach (var p in type.DelegateInvoke.Parameters)
                    WriteParameter(p, output, indent + "    ");
            }
            return;
        }

        if (type.EnumMembers.Count > 0)
        {
            output.WriteLine($"{indent}  members:");
            foreach (var m in type.EnumMembers)
            {
                output.WriteLine($"{indent}    - name: {m.Name}");
                output.WriteLine($"{indent}      value: {m.Value}");
            }
        }

        if (type.Fields.Count > 0)
        {
            output.WriteLine($"{indent}  fields:");
            foreach (var f in type.Fields)
            {
                output.WriteLine($"{indent}    - name: {f.Name}");
                output.WriteLine($"{indent}      type: {Quote(f.Type)}");
                output.WriteLine($"{indent}      modifiers: {Quote(f.Modifiers)}");
                if (f.Value is not null)
                    output.WriteLine($"{indent}      value: {Quote(f.Value)}");
            }
        }

        if (type.Constructors.Count > 0)
        {
            output.WriteLine($"{indent}  constructors:");
            foreach (var c in type.Constructors)
            {
                output.WriteLine($"{indent}    - modifiers: {Quote(c.Modifiers)}");
                if (c.Parameters.Count > 0)
                {
                    output.WriteLine($"{indent}      parameters:");
                    foreach (var p in c.Parameters)
                        WriteParameter(p, output, indent + "        ");
                }
            }
        }

        if (type.Properties.Count > 0)
        {
            output.WriteLine($"{indent}  properties:");
            foreach (var p in type.Properties)
            {
                output.WriteLine($"{indent}    - name: {p.Name}");
                output.WriteLine($"{indent}      type: {Quote(p.Type)}");
                output.WriteLine($"{indent}      modifiers: {Quote(p.Modifiers)}");
                if (p.HasGet) output.WriteLine($"{indent}      get: true");
                if (p.HasSet && !p.IsInit) output.WriteLine($"{indent}      set: true");
                if (p.IsInit) output.WriteLine($"{indent}      init: true");
            }
        }

        if (type.Events.Count > 0)
        {
            output.WriteLine($"{indent}  events:");
            foreach (var e in type.Events)
            {
                output.WriteLine($"{indent}    - name: {e.Name}");
                output.WriteLine($"{indent}      type: {Quote(e.Type)}");
                output.WriteLine($"{indent}      modifiers: {Quote(e.Modifiers)}");
            }
        }

        if (type.Methods.Count > 0)
        {
            output.WriteLine($"{indent}  methods:");
            foreach (var m in type.Methods)
            {
                output.WriteLine($"{indent}    - name: {Quote(FormatNameWithGenerics(m.Name, m.GenericParameters))}");
                output.WriteLine($"{indent}      returnType: {Quote(m.ReturnType)}");
                if (!string.IsNullOrEmpty(m.Modifiers))
                    output.WriteLine($"{indent}      modifiers: {Quote(m.Modifiers)}");
                WriteStringList("genericParameters", m.GenericParameters, output, indent + "      ");
                WriteStringList("constraints", m.Constraints, output, indent + "      ");
                if (m.Parameters.Count > 0)
                {
                    output.WriteLine($"{indent}      parameters:");
                    foreach (var p in m.Parameters)
                        WriteParameter(p, output, indent + "        ");
                }
            }
        }

        if (type.NestedTypes.Count > 0)
        {
            output.WriteLine($"{indent}  nestedTypes:");
            foreach (var nested in type.NestedTypes)
                WriteType(nested, output, indent + "    ");
        }
    }

    private static void WriteParameter(ParameterModel param, TextWriter output, string indent)
    {
        output.WriteLine($"{indent}- name: {param.Name}");
        output.WriteLine($"{indent}  type: {Quote(param.Type)}");
        if (!string.IsNullOrEmpty(param.Modifier))
            output.WriteLine($"{indent}  modifier: {param.Modifier}");
        if (param.DefaultValue is not null)
            output.WriteLine($"{indent}  default: {Quote(param.DefaultValue)}");
    }

    private static void WriteStringList(string key, List<string> items, TextWriter output, string indent)
    {
        if (items.Count == 0) return;

        if (items.All(i => i.Length < 40 && !i.Contains(',')))
        {
            output.WriteLine($"{indent}{key}: [{string.Join(", ", items.Select(Quote))}]");
        }
        else
        {
            output.WriteLine($"{indent}{key}:");
            foreach (var item in items)
                output.WriteLine($"{indent}  - {Quote(item)}");
        }
    }

    private static string FormatNameWithGenerics(string name, List<string> genericParams)
    {
        if (genericParams.Count == 0) return name;
        var cleaned = genericParams.Select(g => g.Replace("in ", "").Replace("out ", ""));
        return $"{name}<{string.Join(", ", cleaned)}>";
    }

    private static string Quote(string value)
    {
        if (value.Contains(':') || value.Contains('#') || value.Contains('"') ||
            value.Contains('{') || value.Contains('}') || value.Contains('[') ||
            value.Contains(']') || value.Contains('<') || value.Contains('>') ||
            value.Contains('\'') || value.Contains('*') || value.Contains('?') ||
            value.Contains('|') || value.Contains('&') || value.StartsWith("- ") ||
            value.StartsWith(' ') || value.EndsWith(' ') ||
            value is "true" or "false" or "null" or "yes" or "no")
        {
            return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        }
        return value;
    }
}
