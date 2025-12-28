# Lanz.MapWeaver

Lanz.MapWeaver is a high-performance .NET source generator that automatically creates object mapping code at compile time. By using source generation, it avoids the performance overhead of reflection-based mappers and provides type safety, while seamlessly integrating with Dependency Injection.

## Features

- **Compile-time generation**: No runtime reflection overhead.
- **Type Safety**: Errors are caught at compile time.
- **Dependency Injection Support**: Automatically generates service collection extensions.
- **Unified Interface**: Implements `IMapper` for consistent usage.
- **Partial Classes & Methods**: Integrates seamlessly with your existing code structure.
- **Zero Boilerplate**: Just define the method signature, and the body is generated.
- **Explicit Member Mapping**: Use `[MapProperty]` and `[MapIgnore]` to override member matching rules.
- **Reverse Mapping**: Flip mappings in both directions by setting `Reverse = true` on `[MapTypes]`.

## Getting Started

### 1. Define your entities and DTOs

```csharp
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```

### 2. Create a Mapper Class

Create a `partial` class and decorate it with `[GenerateMapper]`. Then define a `partial` method for the mapping and decorate it with `[MapTypes]`.

```csharp
using Lanz.MapWeaver.Abstraction.Attributes;

[GenerateMapper]
public partial class UserMapper
{
    [MapTypes(typeof(User), typeof(UserDto), Reverse = true)]
    public partial UserDto Map(User source);
}
```

### 3. Usage

#### Option A: Dependency Injection (Recommended)

The generator automatically creates an extension method `AddMapWeaver()` for `IServiceCollection`. This registers all your generated mappers and a central `IMapper` orchestrator.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Lanz.MapWeaver.Extensions;
using Lanz.MapWeaver.Abstraction;

// 1. Register MapWeaver in your startup
var services = new ServiceCollection();
services.AddMapWeaver();
var provider = services.BuildServiceProvider();

// 2. Inject IMapper
var mapper = provider.GetRequiredService<IMapper>();

var user = new User { Id = 1, FirstName = "Antonio", LastName = "Lanzolla" };

// 3. Map objects
var dto = mapper.Map<UserDto>(user);

Console.WriteLine($"{dto.Id} - {dto.FirstName} {dto.LastName}");
```

#### Option B: Manual Instantiation

You can also instantiate the mapper class directly if you are not using DI.

```csharp
var user = new User { Id = 1, FirstName = "Antonio", LastName = "Lanzolla" };
var mapper = new UserMapper();

// Use the typed method directly
var dto = mapper.Map(user);

// Or use the generic IMapper interface
var dtoGeneric = mapper.Map<UserDto>(user);
```

## Explicit Member Mapping

By default Lanz.MapWeaver maps properties with the same name and type. You can customize individual destination members with attributes from `Lanz.MapWeaver.Abstraction.Attributes`:

```csharp
public class UserDto
{
    public int Id { get; set; }

    [MapProperty("FirstName")]
    public string Name { get; set; }

    [MapProperty("HomeAddress.City")]
    public string City { get; set; }

    [MapIgnore]
    public string? Nickname { get; set; }
}
```

## Explicit Member Mapping

By default Lanz.MapWeaver maps properties with the same name and type. You can customize individual destination members with attributes from `Lanz.MapWeaver.Abstraction.Attributes`:

```csharp
public class UserDto
{
    public int Id { get; set; }

    [MapProperty("FirstName")]
    public string Name { get; set; }

    [MapProperty("HomeAddress.City")]
    public string City { get; set; }

    [MapIgnore]
    public string? Nickname { get; set; }
}
```

- `[MapProperty]` accepts the source property name or a dotted path (e.g. `HomeAddress.City`). The generator produces null-safe accessors for nested paths.
- `[MapIgnore]` prevents the destination property from being assigned.

- `[MapProperty]` accepts the source property name or a dotted path (e.g. `HomeAddress.City`). The generator produces null-safe accessors for nested paths.
- `[MapIgnore]` prevents the destination property from being assigned.

## Reverse Mapping

Add `Reverse = true` (or pass `true` as the third constructor argument) on any `[MapTypes]` declaration to emit the inverse mapping method automatically:

```csharp
[MapTypes(typeof(User), typeof(UserDto), reverse: true)]
public partial UserDto Map(User source);
```

The generator produces both `Map(User source)` and the reverse `MapReverse(UserDto source)` (through the orchestrator) so the same mapper covers round-trips with no extra boilerplate.

## Mapping Rules

The generator follows these simple rules:
- **Property Matching**: Maps properties with the **same name** and **same type** unless overridden by `[MapProperty]`.
- **Public Properties**: Only maps `public` properties.
- **Ignored Members**: Ignores `static`, `read-only`, and `private` properties, or anything annotated with `[MapIgnore]`.
- **Missing Properties**: Properties present in one side but missing in the other are ignored (no exception is thrown).

## Project Structure

- **Lanz.MapWeaver.Generator**: The source generator project.
- **Lanz.MapWeaver.Abstraction**: Contains the attributes and interfaces (`IMapper`).
- **Lanz.MapWeaver.Sample**: A sample console application demonstrating usage.
- **Lanz.MapWeaver.Tests**: Unit tests for the generator.
