namespace Lanz.MapWeaver.Abstraction.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]

public class MapPropertyAttribute : Attribute
{
    public string SourcePropertyName { get; }
    public MapPropertyAttribute(string sourcePropertyName)
    {
        SourcePropertyName = sourcePropertyName;
    }
}
