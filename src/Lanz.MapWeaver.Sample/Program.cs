// See https://aka.ms/new-console-template for more information
using Lanz.MapWeaver.Abstraction;
using Lanz.MapWeaver.Extensions;
using Lanz.MapWeaver.Sample.Dtos;
using Lanz.MapWeaver.Sample.Entities;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Hello, World!");
var user = new User { Id = 1, FirstName = "Antonio", LastName = "Lanzolla", HomeAddress = new Address { Street = "Via Roma", City = "Roma" } };
user.PreviousAddresses.Add(new Address { Street = "Via Milano", City = "Milano" });
user.PreviousAddresses.Add(new Address { Street = "Via Napoli", City = "Napoli" });
var services = new ServiceCollection();
services.AddMapWeaver();

var provider = services.BuildServiceProvider();
var mapper = provider.GetRequiredService<IMapper>();

// 3. Chiamata generica
UserDto dto = mapper.Map<UserDto>(user);

Console.WriteLine($"UserDto: Id={dto.Id}, FirstName={dto.FirstName}, LastName={dto.LastName}, HomeAddress={dto.HomeAddress?.Street}");
foreach (var addr in dto.PreviousAddresses)
{
    Console.WriteLine($" Previous Address: {addr.Street}, {addr.City}");
}