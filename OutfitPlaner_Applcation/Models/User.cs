using System;
using System.Collections.Generic;

namespace OutfitPlaner_Applcation.Models;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Clothing> Clothing { get; set; } = new List<Clothing>();
}