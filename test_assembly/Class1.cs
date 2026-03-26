using System.Collections;

namespace TestAssembly;

// --- Type kinds ---

public class SimpleClass;

public abstract class AbstractClass
{
    public abstract void DoWork();
}

public sealed class SealedClass
{
    public int Value { get; }
}

public static class StaticClass
{
    public static int Add(int a, int b) => a + b;
    public static string Greet(string name) => $"Hello, {name}";
}

public struct SimpleStruct
{
    public int X;
    public int Y;
}

public readonly struct ReadOnlyPoint(int x, int y)
{
    public int X { get; } = x;
    public int Y { get; } = y;
}

public record SimpleRecord(string Name, int Value);

public record struct RecordPoint(int X, int Y);

public interface ISimpleInterface
{
    void Execute();
}

// --- Enums ---

public enum Color
{
    Red,
    Green,
    Blue,
}

[Flags]
public enum Permissions : byte
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
    All = Read | Write | Execute,
}

public enum LongEnum : long
{
    Min = long.MinValue,
    Zero = 0,
    Max = long.MaxValue,
}

// --- Delegates ---

public delegate void SimpleDelegate();
public delegate void EventCallback(object sender, string message);
public delegate TResult Converter<in TInput, out TResult>(TInput input);

// --- Generics with constraints ---

public interface IRepository<T> where T : class
{
    T? GetById(int id);
    IReadOnlyList<T> GetAll();
    void Save(T entity);
    bool Delete(int id);
}

public interface ICovariant<out T>
{
    T Value { get; }
}

public interface IContravariant<in T>
{
    void Accept(T value);
}

public class GenericService<T, TKey>
    where T : class, new()
    where TKey : struct, IEquatable<TKey>
{
    public T CreateDefault() => new();
    public T? FindByKey(TKey key) => null;
    public IReadOnlyDictionary<TKey, T> GetAll() => new Dictionary<TKey, T>();
}

// --- Nullable reference types ---

public class NullableDemo
{
    public string NonNullableString { get; set; } = "";
    public string? NullableString { get; set; }
    public int NonNullableInt { get; set; }
    public int? NullableInt { get; set; }

    public List<string> NonNullableList { get; set; } = [];
    public List<string>? NullableList { get; set; }
    public List<string?> ListWithNullableItems { get; set; } = [];
    public List<string?>? FullyNullableList { get; set; }

    public Dictionary<string, int?> DictWithNullableValue { get; set; } = [];

    public string? GetNullable(string? input) => input;
    public string GetNonNullable(string input) => input;

    public bool TryGet(string key, out string? value)
    {
        value = null;
        return false;
    }
}

// --- Members showcase ---

public class MemberShowcase
{
    // Fields
    public const int MaxValue = 100;
    public const string DefaultName = "default";
    public const bool IsEnabled = true;
    public const double Pi = 3.14159;
    public static readonly int[] EmptyArray = [];

    // Properties
    public int Id { get; }
    public string Name { get; set; } = "";
    public string Description { get; init; } = "";

    // Constructors
    public MemberShowcase() { }
    public MemberShowcase(int id, string name)
    {
        Id = id;
        Name = name;
    }

    // Events
    public event EventHandler? Changed;
    public event EventHandler<string>? NameChanged;

    // Methods
    public virtual void DoSomething() { }
    public virtual string Format() => Name;
    public override string ToString() => $"{Id}: {Name}";
}

// --- Inheritance ---

public class DerivedClass : MemberShowcase, ISimpleInterface
{
    public override void DoSomething() => base.DoSomething();
    public override string Format() => $"Derived: {Name}";
    public void Execute() { }
}

// --- Nested types ---

public class Container
{
    public class InnerClass
    {
        public string Value { get; set; } = "";
    }

    public enum InnerEnum
    {
        A,
        B,
    }

    public interface IInner
    {
        void Run();
    }
}

// --- Parameter modifiers ---

public class ParameterDemo
{
    public void RefMethod(ref int x) { x++; }
    public void OutMethod(out int result) { result = 42; }
    public void InMethod(in int value) { }
    public void ParamsMethod(params int[] values) { }
    public void ParamsSpanMethod(params ReadOnlySpan<int> values) { }
    public void DefaultValues(string name = "hello", int count = 10, bool flag = false, Color color = Color.Red) { }
    public void NullDefault(string? name = null, int? count = null) { }
}

// --- Attributes ---

[Obsolete("Use NewService instead")]
public class OldService
{
    [Obsolete("Deprecated")]
    public void LegacyMethod() { }
}

// --- Base types ---

public class CustomException : Exception
{
    public CustomException(string message) : base(message) { }
    public CustomException(string message, Exception inner) : base(message, inner) { }
}

// --- Interface with multiple type params ---

public interface IFormattable<T>
{
    string Format(T value);
    T Parse(string text);
}

// --- Abstract class with mix ---

public abstract class BaseEntity
{
    public int Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public abstract string GetDisplayName();
    public virtual bool IsValid() => Id > 0;
}

// --- Arrays and multi-dimensional ---

public class ArrayDemo
{
    public int[] OneDimensional { get; set; } = [];
    public int[,] TwoDimensional { get; set; } = new int[0, 0];
    public int[][] Jagged { get; set; } = [];
    public string?[]? NullableArray { get; set; }
}
