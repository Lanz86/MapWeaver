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

    public MapTypesAttribute(Type source, Type target)
    {
        Source = source;
        Target = target;
    }
}
