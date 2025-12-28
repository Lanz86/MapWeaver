namespace Lanz.MapWeaver.Abstraction.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class GenerateMapperAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapTypesAttribute : Attribute
{
    public Type Source { get; }
    public Type Target { get; }

    public bool Reverse { get; set; } = false;

    public MapTypesAttribute(Type source, Type target)
    {
        Source = source;
        Target = target;
    }

    public MapTypesAttribute(Type source, Type target, bool reverse)
    {
        Source = source;
        Target = target;
        Reverse = reverse;
    }
}
