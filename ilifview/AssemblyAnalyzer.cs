using System.Reflection;

namespace ilifview;

static class AssemblyAnalyzer
{
    private static readonly Dictionary<string, string> TypeKeywords = new()
    {
        ["System.Void"] = "void",
        ["System.Boolean"] = "bool",
        ["System.Byte"] = "byte",
        ["System.SByte"] = "sbyte",
        ["System.Char"] = "char",
        ["System.Int16"] = "short",
        ["System.UInt16"] = "ushort",
        ["System.Int32"] = "int",
        ["System.UInt32"] = "uint",
        ["System.Int64"] = "long",
        ["System.UInt64"] = "ulong",
        ["System.Single"] = "float",
        ["System.Double"] = "double",
        ["System.Decimal"] = "decimal",
        ["System.String"] = "string",
        ["System.Object"] = "object",
        ["System.IntPtr"] = "nint",
        ["System.UIntPtr"] = "nuint",
    };

    private static readonly HashSet<string> HiddenAttributes =
    [
        "System.Runtime.CompilerServices.NullableContextAttribute",
        "System.Runtime.CompilerServices.NullableAttribute",
        "System.Runtime.CompilerServices.NullablePublicOnlyAttribute",
        "System.Runtime.CompilerServices.IsReadOnlyAttribute",
        "System.Runtime.CompilerServices.CompilerGeneratedAttribute",
    ];

    // --- Nullable annotation support ---

    private class NullableState
    {
        private readonly byte[] _flags;
        private int _pos;

        public NullableState(byte flag) => _flags = [flag];
        public NullableState(byte[] flags) => _flags = flags.Length > 0 ? flags : [0];

        public byte Take()
        {
            if (_flags.Length == 1) return _flags[0];
            return _pos < _flags.Length ? _flags[_pos++] : (byte)0;
        }
    }

    private static byte ReadNullableContext(IList<CustomAttributeData> attributes)
    {
        foreach (var attr in attributes)
        {
            if (attr.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute")
                return (byte)attr.ConstructorArguments[0].Value!;
        }
        return 0;
    }

    private static NullableState GetNullableState(IList<CustomAttributeData> attributes, byte defaultCtx)
    {
        foreach (var attr in attributes)
        {
            if (attr.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute")
            {
                var arg = attr.ConstructorArguments[0];
                if (arg.Value is byte b)
                    return new NullableState(b);
                if (arg.Value is IReadOnlyCollection<CustomAttributeTypedArgument> arr)
                    return new NullableState(arr.Select(a => (byte)a.Value!).ToArray());
            }
        }
        return new NullableState(defaultCtx);
    }

    private static IList<CustomAttributeData> SafeGetAttributes(MemberInfo member)
    {
        try { return member.GetCustomAttributesData(); }
        catch { return []; }
    }

    private static IList<CustomAttributeData> SafeGetAttributes(ParameterInfo param)
    {
        try { return param.GetCustomAttributesData(); }
        catch { return []; }
    }

    // --- Compiler-generated member filter ---

    private static bool IsCompilerGenerated(string name) => name.Contains('<') || name.Contains('$');

    // --- Main analysis ---

    public static AssemblyInfo Analyze(Assembly assembly)
    {
        Type[] allTypes;
        try { allTypes = assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { allTypes = ex.Types.Where(t => t is not null).ToArray()!; }

        var publicTypes = allTypes
            .Where(t => t.IsPublic)
            .OrderBy(t => t.Namespace ?? "")
            .ThenBy(t => t.Name);

        var namespaces = publicTypes
            .GroupBy(t => t.Namespace ?? "(global)")
            .OrderBy(g => g.Key)
            .Select(g => new NamespaceInfo(g.Key, g.Select(t =>
            {
                try { return AnalyzeType(t); }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Warning: Skipping type '{t.FullName}': {ex.Message}");
                    return null;
                }
            }).Where(t => t is not null).Cast<TypeModel>().ToList()))
            .Where(ns => ns.Types.Count > 0)
            .ToList();

        string? targetFramework = null;
        try
        {
            var tfAttr = assembly.GetCustomAttributesData()
                .FirstOrDefault(a => a.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute");
            if (tfAttr is not null)
                targetFramework = tfAttr.ConstructorArguments[0].Value as string;
        }
        catch { }

        return new AssemblyInfo(assembly.GetName().Name ?? "Unknown", targetFramework, namespaces);
    }

    private static TypeModel AnalyzeType(Type type)
    {
        var kind = TypeKindHelper.Determine(type);
        var modifiers = GetTypeModifiers(type, kind);
        var genericParams = GetGenericParameters(type);
        var constraints = GetGenericConstraints(type);
        var baseType = GetBaseTypeName(type, kind);
        var interfaces = GetInterfaces(type);
        var attributes = GetAttributes(type);
        var typeCtx = ReadNullableContext(SafeGetAttributes((MemberInfo)type));

        var fields = new List<FieldModel>();
        var constructors = new List<ConstructorModel>();
        var properties = new List<PropertyModel>();
        var events = new List<EventModel>();
        var methods = new List<MethodModel>();
        var enumMembers = new List<EnumMemberModel>();
        MethodModel? delegateInvoke = null;
        var nestedTypes = new List<TypeModel>();

        if (kind == TypeKind.Enum)
        {
            enumMembers = AnalyzeEnumMembers(type);
        }
        else if (kind == TypeKind.Delegate)
        {
            delegateInvoke = AnalyzeDelegateInvoke(type, typeCtx);
        }
        else
        {
            fields = AnalyzeFields(type, typeCtx);
            constructors = AnalyzeConstructors(type, typeCtx);
            properties = AnalyzeProperties(type, typeCtx);
            events = AnalyzeEvents(type, typeCtx);
            methods = AnalyzeMethods(type, typeCtx);
        }

        nestedTypes = type.GetNestedTypes(BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(t => !IsCompilerGenerated(t.Name))
            .Select(AnalyzeType)
            .ToList();

        var name = FormatTypeName(type);

        return new TypeModel(kind, name, modifiers, genericParams, baseType, interfaces, constraints,
            fields, constructors, properties, events, methods, enumMembers, delegateInvoke, nestedTypes, attributes);
    }

    private static string FormatTypeName(Type type)
    {
        var name = type.Name;
        int tick = name.IndexOf('`');
        if (tick >= 0)
            name = name[..tick];
        return name;
    }

    private static string GetTypeModifiers(Type type, TypeKind kind)
    {
        var parts = new List<string>();

        if (type.IsPublic || type.IsNestedPublic)
            parts.Add("public");
        else if (type.IsNestedFamily)
            parts.Add("protected");
        else if (type.IsNestedFamORAssem)
            parts.Add("protected internal");

        if (kind == TypeKind.Class)
        {
            if (type.IsAbstract)
                parts.Add("abstract");
            else if (type.IsSealed)
                parts.Add("sealed");
        }

        return string.Join(' ', parts);
    }

    private static List<string> GetGenericParameters(Type type)
    {
        if (!type.IsGenericTypeDefinition)
            return [];

        return type.GetGenericArguments().Select(arg =>
        {
            var attrs = arg.GenericParameterAttributes;
            var prefix = "";
            if (attrs.HasFlag(GenericParameterAttributes.Covariant))
                prefix = "out ";
            else if (attrs.HasFlag(GenericParameterAttributes.Contravariant))
                prefix = "in ";
            return prefix + arg.Name;
        }).ToList();
    }

    private static List<string> GetGenericConstraints(Type type)
    {
        if (!type.IsGenericTypeDefinition)
            return [];

        var result = new List<string>();
        foreach (var arg in type.GetGenericArguments())
        {
            var constraint = FormatConstraint(arg);
            if (constraint is not null)
                result.Add(constraint);
        }
        return result;
    }

    private static List<string> GetMethodGenericConstraints(MethodInfo method)
    {
        if (!method.IsGenericMethodDefinition)
            return [];

        var result = new List<string>();
        foreach (var arg in method.GetGenericArguments())
        {
            var constraint = FormatConstraint(arg);
            if (constraint is not null)
                result.Add(constraint);
        }
        return result;
    }

    private static string? FormatConstraint(Type arg)
    {
        var parts = new List<string>();
        var attrs = arg.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;

        if (attrs.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
            parts.Add("class");
        if (attrs.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
            parts.Add("struct");
        if (attrs.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint) &&
            !attrs.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
            parts.Add("new()");

        try
        {
            foreach (var ct in arg.GetGenericParameterConstraints())
            {
                if (ct.FullName == "System.ValueType")
                    continue;
                parts.Add(FormatTypeReference(ct));
            }
        }
        catch { /* MLC may fail to resolve constraint types */ }

        if (parts.Count == 0)
            return null;

        return $"where {arg.Name} : {string.Join(", ", parts)}";
    }

    private static string? GetBaseTypeName(Type type, TypeKind kind)
    {
        if (kind is TypeKind.Enum)
        {
            try
            {
                var underlyingName = FormatTypeReference(type.GetEnumUnderlyingType());
                return underlyingName == "int" ? null : underlyingName;
            }
            catch { return null; }
        }

        if (kind is TypeKind.Interface or TypeKind.Delegate or TypeKind.Struct or TypeKind.RecordStruct)
            return null;

        var baseType = type.BaseType;
        if (baseType is null || baseType.FullName is "System.Object" or "System.ValueType")
            return null;

        return FormatTypeReference(baseType);
    }

    private static List<string> GetInterfaces(Type type)
    {
        try
        {
            var declared = type.GetInterfaces();
            var inherited = type.BaseType?.GetInterfaces() ?? [];
            return declared.Except(inherited)
                .Select(i => FormatTypeReference(i))
                .OrderBy(n => n)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static List<string> GetAttributes(Type type)
    {
        try
        {
            return type.GetCustomAttributesData()
                .Where(a => a.AttributeType.IsPublic)
                .Where(a => !HiddenAttributes.Contains(a.AttributeType.FullName ?? ""))
                .Select(FormatAttribute)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string FormatAttribute(CustomAttributeData attr)
    {
        var name = FormatTypeReference(attr.AttributeType);
        if (name.EndsWith("Attribute"))
            name = name[..^9];

        var args = new List<string>();
        foreach (var ca in attr.ConstructorArguments)
            args.Add(FormatAttributeValue(ca));
        foreach (var na in attr.NamedArguments)
            args.Add($"{na.MemberName} = {FormatAttributeValue(na.TypedValue)}");

        return args.Count > 0 ? $"[{name}({string.Join(", ", args)})]" : $"[{name}]";
    }

    private static string FormatAttributeValue(CustomAttributeTypedArgument arg)
    {
        if (arg.Value is null)
            return "null";
        if (arg.ArgumentType.FullName == "System.String")
            return $"\"{arg.Value}\"";
        if (arg.ArgumentType.FullName == "System.Boolean")
            return (bool)arg.Value ? "true" : "false";
        if (arg.ArgumentType.FullName == "System.Type" && arg.Value is Type t)
            return $"typeof({FormatTypeReference(t)})";
        return arg.Value.ToString() ?? "null";
    }

    // --- Member analysis ---

    private static List<EnumMemberModel> AnalyzeEnumMembers(Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(f =>
            {
                string value;
                try { value = f.GetRawConstantValue()?.ToString() ?? "0"; }
                catch { value = "0"; }
                return new EnumMemberModel(f.Name, value);
            })
            .ToList();
    }

    private static MethodModel? AnalyzeDelegateInvoke(Type type, byte typeCtx)
    {
        var invoke = type.GetMethod("Invoke");
        if (invoke is null) return null;

        var methodCtx = ReadNullableContext(SafeGetAttributes(invoke));
        if (methodCtx == 0) methodCtx = typeCtx;

        var returnNs = GetNullableState(SafeGetAttributes(invoke.ReturnParameter), methodCtx);

        return new MethodModel(
            "",
            FormatTypeReference(invoke.ReturnType, returnNs),
            type.Name,
            GetGenericParameters(type),
            AnalyzeParameters(invoke.GetParameters(), methodCtx),
            []);
    }

    private static List<FieldModel> AnalyzeFields(Type type, byte typeCtx)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(f => !f.IsSpecialName && !IsCompilerGenerated(f.Name))
            .Select(f =>
            {
                var mods = GetFieldModifiers(f);
                string? value = null;
                if (f.IsLiteral)
                {
                    try
                    {
                        var raw = f.GetRawConstantValue();
                        value = FormatConstantValue(raw, f.FieldType);
                    }
                    catch { value = null; }
                }
                var ns = GetNullableState(SafeGetAttributes(f), typeCtx);
                return new FieldModel(mods, FormatTypeReference(f.FieldType, ns), f.Name, value);
            })
            .ToList();
    }

    private static string GetFieldModifiers(FieldInfo field)
    {
        var parts = new List<string>();
        if (field.IsPublic) parts.Add("public");
        else if (field.IsFamily) parts.Add("protected");
        else if (field.IsFamilyOrAssembly) parts.Add("protected internal");

        if (field.IsLiteral)
            parts.Add("const");
        else if (field.IsStatic)
        {
            parts.Add("static");
            if (field.IsInitOnly) parts.Add("readonly");
        }
        else if (field.IsInitOnly)
            parts.Add("readonly");

        return string.Join(' ', parts);
    }

    private static List<ConstructorModel> AnalyzeConstructors(Type type, byte typeCtx)
    {
        return type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(c =>
            {
                var mods = c.IsPublic ? "public" : c.IsFamily ? "protected" : "protected internal";
                var typeName = FormatTypeName(type);
                var methodCtx = ReadNullableContext(SafeGetAttributes(c));
                if (methodCtx == 0) methodCtx = typeCtx;
                return new ConstructorModel(mods, typeName, AnalyzeParameters(c.GetParameters(), methodCtx));
            })
            .ToList();
    }

    private static List<PropertyModel> AnalyzeProperties(Type type, byte typeCtx)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(p => !IsCompilerGenerated(p.Name))
            .Select(p =>
            {
                var getter = p.GetGetMethod();
                var setter = p.GetSetMethod();
                var accessor = getter ?? setter;
                if (accessor is null) return null;

                var mods = GetMethodModifiers(accessor, type);
                bool isInit = false;
                if (setter is not null)
                {
                    try
                    {
                        var retParam = setter.ReturnParameter;
                        isInit = retParam.GetRequiredCustomModifiers()
                            .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");
                    }
                    catch { }
                }

                var ns = GetNullableState(SafeGetAttributes(p), typeCtx);
                return new PropertyModel(mods, FormatTypeReference(p.PropertyType, ns), p.Name,
                    getter is not null, setter is not null, isInit);
            })
            .Where(p => p is not null)
            .Cast<PropertyModel>()
            .ToList();
    }

    private static List<EventModel> AnalyzeEvents(Type type, byte typeCtx)
    {
        return type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(e => !IsCompilerGenerated(e.Name))
            .Select(e =>
            {
                var addMethod = e.GetAddMethod();
                var mods = addMethod is not null ? GetMethodModifiers(addMethod, type) : "public";
                var ns = GetNullableState(SafeGetAttributes(e), typeCtx);
                return new EventModel(mods, FormatTypeReference(e.EventHandlerType!, ns), e.Name);
            })
            .ToList();
    }

    private static List<MethodModel> AnalyzeMethods(Type type, byte typeCtx)
    {
        var propertyAccessors = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .SelectMany(p => new[] { p.GetGetMethod(), p.GetSetMethod() })
            .Where(m => m is not null)
            .ToHashSet();

        var eventAccessors = type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .SelectMany(e => new[] { e.GetAddMethod(), e.GetRemoveMethod() })
            .Where(m => m is not null)
            .ToHashSet();

        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && !IsCompilerGenerated(m.Name))
            .Where(m => !propertyAccessors.Contains(m))
            .Where(m => !eventAccessors.Contains(m))
            .Select(m =>
            {
                var mods = GetMethodModifiers(m, type);
                var genericParams = m.IsGenericMethodDefinition
                    ? m.GetGenericArguments().Select(a => a.Name).ToList()
                    : new List<string>();
                var constraints = GetMethodGenericConstraints(m);

                var methodCtx = ReadNullableContext(SafeGetAttributes(m));
                if (methodCtx == 0) methodCtx = typeCtx;

                var returnNs = GetNullableState(SafeGetAttributes(m.ReturnParameter), methodCtx);
                return new MethodModel(mods, FormatTypeReference(m.ReturnType, returnNs), m.Name,
                    genericParams, AnalyzeParameters(m.GetParameters(), methodCtx), constraints);
            })
            .ToList();
    }

    private static string GetMethodModifiers(MethodInfo method, Type declaringType)
    {
        var parts = new List<string>();

        if (method.IsPublic) parts.Add("public");
        else if (method.IsFamily) parts.Add("protected");
        else if (method.IsFamilyOrAssembly) parts.Add("protected internal");

        if (declaringType.IsInterface)
            return string.Join(' ', parts);

        if (method.IsStatic)
        {
            parts.Add("static");
        }
        else if (method.IsAbstract)
        {
            parts.Add("abstract");
        }
        else if (method.IsVirtual)
        {
            bool isOverride = false;
            try { isOverride = method.GetBaseDefinition().DeclaringType != method.DeclaringType; }
            catch { /* MLC may not support GetBaseDefinition */ }

            if (isOverride)
                parts.Add("override");
            else if (!method.IsFinal)
                parts.Add("virtual");
        }

        return string.Join(' ', parts);
    }

    private static List<ParameterModel> AnalyzeParameters(ParameterInfo[] parameters, byte methodCtx)
    {
        return parameters.Select((p, i) =>
        {
            var modifier = "";
            if (p.IsOut) modifier = "out";
            else if (p.IsIn) modifier = "in";
            else if (p.ParameterType.IsByRef) modifier = "ref";

            try
            {
                if (i == parameters.Length - 1 && p.GetCustomAttributesData()
                    .Any(a => a.AttributeType.FullName == "System.ParamArrayAttribute"))
                    modifier = "params";
            }
            catch { }

            var paramType = p.ParameterType;
            if (paramType.IsByRef)
                paramType = paramType.GetElementType()!;

            string? defaultValue = null;
            try
            {
                if (p.HasDefaultValue)
                    defaultValue = FormatConstantValue(p.RawDefaultValue, paramType);
            }
            catch { }

            var ns = GetNullableState(SafeGetAttributes(p), methodCtx);
            return new ParameterModel(modifier, FormatTypeReference(paramType, ns), p.Name ?? $"arg{i}", defaultValue);
        }).ToList();
    }

    private static string FormatConstantValue(object? value, Type type)
    {
        if (value is null)
            return "null";
        if (type.FullName == "System.String")
            return $"\"{value}\"";
        if (type.FullName == "System.Boolean")
            return (bool)value ? "true" : "false";
        if (type.FullName == "System.Char")
            return $"'{value}'";
        if (type.FullName == "System.Single")
            return $"{value}f";
        if (type.FullName == "System.Double")
            return $"{value}d";
        if (type.FullName == "System.Decimal")
            return $"{value}m";
        if (type.FullName == "System.Int64")
            return $"{value}L";
        if (type.FullName == "System.UInt64")
            return $"{value}UL";
        return value.ToString() ?? "0";
    }

    // --- Type formatting ---

    public static string FormatTypeReference(Type type) => FormatTypeReference(type, nullable: null);

    private static string FormatTypeReference(Type type, NullableState? nullable)
    {
        if (type.IsByRef)
            return FormatTypeReference(type.GetElementType()!, nullable);

        if (type.IsPointer)
        {
            nullable?.Take();
            return FormatTypeReference(type.GetElementType()!, nullable) + "*";
        }

        if (type.IsArray)
        {
            var flag = nullable?.Take() ?? 0;
            var rank = type.GetArrayRank();
            var commas = rank > 1 ? new string(',', rank - 1) : "";
            var inner = FormatTypeReference(type.GetElementType()!, nullable);
            var result = $"{inner}[{commas}]";
            return flag == 2 ? result + "?" : result;
        }

        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def.FullName == "System.Nullable`1")
            {
                nullable?.Take(); // consume byte for Nullable<> wrapper itself
                var inner = type.GetGenericArguments()[0];
                return FormatTypeReference(inner, nullable) + "?";
            }

            var flag = nullable?.Take() ?? 0;

            var baseName = def.FullName ?? def.Name;
            int tick = baseName.IndexOf('`');
            if (tick >= 0)
                baseName = baseName[..tick];

            if (TypeKeywords.TryGetValue(baseName, out var kw))
                baseName = kw;
            else
                baseName = SimplifyNamespace(baseName);

            var args = type.GetGenericArguments().Select(a => FormatTypeReference(a, nullable));
            var suffix = (flag == 2) ? "?" : "";
            return $"{baseName}<{string.Join(", ", args)}>{suffix}";
        }

        if (type.IsGenericParameter)
        {
            var flag = nullable?.Take() ?? 0;
            return flag == 2 ? type.Name + "?" : type.Name;
        }

        var simpleFlag = nullable?.Take() ?? 0;
        var fullName = type.FullName ?? type.Name;
        if (TypeKeywords.TryGetValue(fullName, out var keyword))
        {
            // Only reference-type keywords can be nullable
            if (simpleFlag == 2 && keyword is "string" or "object")
                return keyword + "?";
            return keyword;
        }

        var result2 = SimplifyNamespace(fullName);
        if (simpleFlag == 2 && !type.IsValueType)
            return result2 + "?";
        return result2;
    }

    private static string SimplifyNamespace(string fullName)
    {
        return fullName.Replace('+', '.');
    }
}
