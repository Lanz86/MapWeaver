// See https://aka.ms/new-console-template for more information
using Lanz.MapWeaver.Abstraction;
using Lanz.MapWeaver.Extensions;
using Lanz.MapWeaver.Sample.Dtos;
using Lanz.MapWeaver.Sample.Entities;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Hello, World!");
var user = new User { Id = 1, 
                        FirstName = "Antonio", 
                        LastName = "Lanzolla",
                        HomeAddress = new Address { Street = "Via Roma", City = "Roma" },
                        Nickname = "Lanz"
};
user.PreviousAddresses.Add(new Address { Street = "Via Milano", City = "Milano" });
user.PreviousAddresses.Add(new Address { Street = "Via Napoli", City = "Napoli" });
var services = new ServiceCollection();
services.AddMapWeaver();

var provider = services.BuildServiceProvider();
var mapper = provider.GetRequiredService<IMapper>();

// 3. Chiamata generica
UserDto dto = mapper.Map<UserDto>(user);

Console.WriteLine($"UserDto: Id={dto.Id}, FirstName={dto.FirstName}, LastName={dto.LastName}, HomeAddress={dto.HomeAddress?.Street}");
Console.WriteLine($" Name: {dto.Name}");
Console.WriteLine($" Surname: {dto.Surname}");
Console.WriteLine($" ComputedValue (should be empty): '{dto.ComputedValue}'");
Console.WriteLine($"NickName: {dto.Nickname}");
Console.WriteLine($" City: {dto.City}");
foreach (var addr in dto.PreviousAddresses)
{
    Console.WriteLine($" Previous Address: {addr.Street}, {addr.City}");
}

Console.WriteLine("Mapping back to User entity...");
User mappedUser = mapper.Map<User>(dto);
Console.WriteLine($"Mapped User: Id={mappedUser.Id}, FirstName={mappedUser.FirstName}, LastName={mappedUser.LastName}, HomeAddress={mappedUser.HomeAddress?.Street}");