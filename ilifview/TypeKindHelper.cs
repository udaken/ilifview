using System.Reflection;

namespace ilifview;

public enum TypeKind
{
    Class,
    StaticClass,
    Record,
    RecordStruct,
    Struct,
    Enum,
    Interface,
    Delegate,
}

static class TypeKindHelper
{
    public static TypeKind Determine(Type type)
    {
        if (type.IsEnum)
            return TypeKind.Enum;

        if (type.IsInterface)
            return TypeKind.Interface;

        if (type.BaseType?.FullName == "System.MulticastDelegate")
            return TypeKind.Delegate;

        var declaredMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        bool hasCloneMethod = declaredMethods.Any(m => m.Name == "<Clone>$");
        bool hasPrintMembers = declaredMethods.Any(m =>
            m.Name == "PrintMembers"
            && m.GetParameters().Length == 1
            && m.GetParameters()[0].ParameterType.FullName == "System.Text.StringBuilder");

        if (type.IsValueType)
            return hasPrintMembers ? TypeKind.RecordStruct : TypeKind.Struct;

        if (hasCloneMethod)
            return TypeKind.Record;

        if (type.IsAbstract && type.IsSealed)
            return TypeKind.StaticClass;

        return TypeKind.Class;
    }

    public static string ToKeyword(TypeKind kind) => kind switch
    {
        TypeKind.Class => "class",
        TypeKind.StaticClass => "static class",
        TypeKind.Record => "record",
        TypeKind.RecordStruct => "record struct",
        TypeKind.Struct => "struct",
        TypeKind.Enum => "enum",
        TypeKind.Interface => "interface",
        TypeKind.Delegate => "delegate",
        _ => "class",
    };
}
