using System;
using System.Collections.Generic;
using System.Text;

namespace Lanz.MapWeaver.Sample.Entities;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Address? HomeAddress { get; set; }

}
