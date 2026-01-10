namespace Lanz.MapWeaver.Abstraction.Attributes;

/// <summary>
/// Specifies a fallback value to use when the source property is null.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class MapWithFallbackAttribute : Attribute
{
    /// <summary>
    /// Gets the fallback value to use when the source property is null.
    /// </summary>
    public object? FallbackValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapWithFallbackAttribute"/> class.
    /// </summary>
    /// <param name="fallbackValue">The fallback value to use when the source property is null.</param>
    public MapWithFallbackAttribute(object? fallbackValue)
    {
        FallbackValue = fallbackValue;
    }
}
