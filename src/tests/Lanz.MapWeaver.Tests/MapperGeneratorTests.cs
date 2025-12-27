using System.Linq;
using Xunit;

namespace Lanz.MapWeaver.Tests;

public class MapperGeneratorTests
{
    [Fact]
    public void ShouldGenerateMapperMethod()
    {
        var source = @"
using Lanz.MapWeaver.Abstraction.Attributes;
using System;

namespace TestNamespace
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    [GenerateMapper]
    public partial class UserMapper
    {
        [MapTypes(typeof(User), typeof(UserDto))]
        public partial UserDto MapToDto(User source);
    }
}";

        var (diagnostics, generatedSources) = TestHelper.GetGeneratedOutput(source);

        Assert.Empty(diagnostics);
        Assert.NotEmpty(generatedSources);

        var mapperSource = generatedSources.First(s => s.Contains("partial class UserMapper"));

        Assert.Contains("partial class UserMapper", mapperSource);
        // The generator uses FullyQualifiedFormat which includes global::
        Assert.Contains("public partial global::TestNamespace.UserDto MapToDto(global::TestNamespace.User source)", mapperSource);
        Assert.Contains("dest.Id = source.Id;", mapperSource);
        Assert.Contains("dest.Name = source.Name;", mapperSource);
        // Age is not in Dto, so it should not be mapped
        Assert.DoesNotContain("dest.Age", mapperSource);
    }

    [Fact]
    public void ShouldHandlePropertyMismatches()
    {
        var source = @"
using Lanz.MapWeaver.Abstraction.Attributes;
using System;

namespace TestNamespace
{
    public class Source
    {
        public int Matching { get; set; }
        public int TypeMismatch { get; set; }
        public int MissingInDest { get; set; }
        private int PrivateProp { get; set; }
        public static int StaticProp { get; set; }
    }

    public class Dest
    {
        public int Matching { get; set; }
        public string TypeMismatch { get; set; }
        public int MissingInSource { get; set; }
        public int ReadOnly { get; }
    }

    [GenerateMapper]
    public partial class MyMapper
    {
        [MapTypes(typeof(Source), typeof(Dest))]
        public partial Dest Map(Source source);
    }
}";

        var (diagnostics, generatedSources) = TestHelper.GetGeneratedOutput(source);

        Assert.Empty(diagnostics);
        Assert.NotEmpty(generatedSources);

        var mapperSource = generatedSources.First(s => s.Contains("partial class MyMapper"));

        // Matching property should be mapped
        Assert.Contains("dest.Matching = source.Matching;", mapperSource);

        // Type mismatch should not be mapped
        Assert.DoesNotContain("dest.TypeMismatch", mapperSource);

        // Missing properties should not be mapped
        Assert.DoesNotContain("dest.MissingInSource", mapperSource);
        Assert.DoesNotContain("source.MissingInDest", mapperSource);

        // ReadOnly property should not be mapped
        Assert.DoesNotContain("dest.ReadOnly", mapperSource);
        
        // Private/Static properties should not be mapped (implicit by not being in srcProps/dstProps logic)
        // But we can check if they appear in assignment
        Assert.DoesNotContain("source.PrivateProp", mapperSource);
        Assert.DoesNotContain("source.StaticProp", mapperSource);
    }

    [Fact]
    public void ShouldGenerateNestedMappingCallsForReferenceTypes()
    {
        var source = @"
using Lanz.MapWeaver.Abstraction.Attributes;

namespace TestNamespace
{
    public class Address
    {
        public string City { get; set; }
    }

    public class AddressDto
    {
        public string City { get; set; }
    }

    public class User
    {
        public Address Address { get; set; }
    }

    public class UserDto
    {
        public AddressDto Address { get; set; }
    }

    [GenerateMapper]
    public partial class UserMapper
    {
        [MapTypes(typeof(User), typeof(UserDto))]
        public partial UserDto Map(User source);
    }
}";
        var (diagnostics, generatedSources) = TestHelper.GetGeneratedOutput(source);

        Assert.Empty(diagnostics);

        var mapperSource = generatedSources.Single(s => s.Contains("partial class UserMapper"));

        Assert.Contains("if (source.Address is not null)", mapperSource);
        Assert.Contains("dest.Address = _sp.GetRequiredService<IMapper>().Map<global::TestNamespace.AddressDto>(source.Address);", mapperSource);
    }

    [Fact]
    public void ShouldGenerateServiceCollectionExtensionForRegisteredMappers()
    {
        var source = @"
using Lanz.MapWeaver.Abstraction.Attributes;

namespace TestNamespace
{
    public class User
    {
    }

    public class UserDto
    {
    }

    public class Address
    {
    }

    public class AddressDto
    {
    }

    [GenerateMapper]
    public partial class UserMapper
    {
        [MapTypes(typeof(User), typeof(UserDto))]
        public partial UserDto Map(User source);
    }

    [GenerateMapper]
    public partial class AddressMapper
    {
        [MapTypes(typeof(Address), typeof(AddressDto))]
        public partial AddressDto Map(Address source);
    }
}";
        var (diagnostics, generatedSources) = TestHelper.GetGeneratedOutput(source);

        Assert.Empty(diagnostics);

        var extensionSource = generatedSources.Single(s => s.Contains("MapWeaverServiceCollectionExtensions"));

        Assert.Contains("internal sealed class MapWeaverOrchestrator : IMapper", extensionSource);
        Assert.Contains("services.AddSingleton<global::TestNamespace.UserMapper>();", extensionSource);
        Assert.Contains("services.AddSingleton<global::TestNamespace.AddressMapper>();", extensionSource);
        Assert.Contains("services.AddSingleton<IMapper, MapWeaverOrchestrator>();", extensionSource);
        Assert.Contains("var profile = _serviceProvider.GetRequiredService<global::TestNamespace.UserMapper>();", extensionSource);
        Assert.Contains("var profile = _serviceProvider.GetRequiredService<global::TestNamespace.AddressMapper>();", extensionSource);
        Assert.Contains("if (srcType == typeof(global::TestNamespace.User) && dstType == typeof(global::TestNamespace.UserDto))", extensionSource);
        Assert.Contains("if (srcType == typeof(global::TestNamespace.Address) && dstType == typeof(global::TestNamespace.AddressDto))", extensionSource);
    }
}
