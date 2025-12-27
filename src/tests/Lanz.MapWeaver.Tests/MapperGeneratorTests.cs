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
        Assert.Single(generatedSources);

        var generatedCode = generatedSources[0];

        Assert.Contains("partial class UserMapper", generatedCode);
        // The generator uses FullyQualifiedFormat which includes global::
        Assert.Contains("public partial global::TestNamespace.UserDto MapToDto(global::TestNamespace.User source)", generatedCode);
        Assert.Contains("dest.Id = source.Id;", generatedCode);
        Assert.Contains("dest.Name = source.Name;", generatedCode);
        // Age is not in Dto, so it should not be mapped
        Assert.DoesNotContain("dest.Age", generatedCode);
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
        Assert.Single(generatedSources);

        var generatedCode = generatedSources[0];

        // Matching property should be mapped
        Assert.Contains("dest.Matching = source.Matching;", generatedCode);

        // Type mismatch should not be mapped
        Assert.DoesNotContain("dest.TypeMismatch", generatedCode);

        // Missing properties should not be mapped
        Assert.DoesNotContain("dest.MissingInSource", generatedCode);
        Assert.DoesNotContain("source.MissingInDest", generatedCode);

        // ReadOnly property should not be mapped
        Assert.DoesNotContain("dest.ReadOnly", generatedCode);
        
        // Private/Static properties should not be mapped (implicit by not being in srcProps/dstProps logic)
        // But we can check if they appear in assignment
        Assert.DoesNotContain("source.PrivateProp", generatedCode);
        Assert.DoesNotContain("source.StaticProp", generatedCode);
    }
}
