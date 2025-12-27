using Lanz.MapWeaver.Abstraction.Attributes;
using Lanz.MapWeaver.Sample.Entities;

namespace Lanz.MapWeaver.Sample.Dtos;

public class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

[GenerateMapper]
public partial class AddressMapper
{
    [MapTypes(typeof(Address), typeof(AddressDto))]
    public partial AddressDto Map(Address source);

}