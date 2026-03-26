namespace ilifview;

record AssemblyInfo(string Name, string? TargetFramework, List<NamespaceInfo> Namespaces);

record NamespaceInfo(string Name, List<TypeModel> Types);

record TypeModel(
    TypeKind Kind,
    string Name,
    string Modifiers,
    List<string> GenericParameters,
    string? BaseType,
    List<string> Interfaces,
    List<string> Constraints,
    List<FieldModel> Fields,
    List<ConstructorModel> Constructors,
    List<PropertyModel> Properties,
    List<EventModel> Events,
    List<MethodModel> Methods,
    List<EnumMemberModel> EnumMembers,
    MethodModel? DelegateInvoke,
    List<TypeModel> NestedTypes,
    List<string> Attributes);

record FieldModel(string Modifiers, string Type, string Name, string? Value);

record PropertyModel(string Modifiers, string Type, string Name, bool HasGet, bool HasSet, bool IsInit);

record MethodModel(
    string Modifiers,
    string ReturnType,
    string Name,
    List<string> GenericParameters,
    List<ParameterModel> Parameters,
    List<string> Constraints);

record ConstructorModel(string Modifiers, string TypeName, List<ParameterModel> Parameters);

record EventModel(string Modifiers, string Type, string Name);

record ParameterModel(string Modifier, string Type, string Name, string? DefaultValue);

record EnumMemberModel(string Name, string Value);
