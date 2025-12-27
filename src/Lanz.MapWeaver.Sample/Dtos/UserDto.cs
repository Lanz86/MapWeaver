using Lanz.MapWeaver.Abstraction.Attributes;
using Lanz.MapWeaver.Sample.Entities;

namespace Lanz.MapWeaver.Sample.Dtos {

    public class UserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public AddressDto? HomeAddress { get; set; }

        public List<AddressDto> PreviousAddresses { get; set; } = new List<AddressDto>();
    }

    [GenerateMapper]
    public partial class UserMapper
    {
        [MapTypes(typeof(User), typeof(UserDto))]
        public partial UserDto Map(User source);

    }
}