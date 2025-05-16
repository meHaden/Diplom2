using System;
using System.Collections.Generic;

namespace OutfitPlaner_Applcation.Models;

public partial class ThematicLook
{
    public int Id { get; set; }

    public int IdUser { get; set; }

    public string Name { get; set; } = null!;

    public string Theme { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public bool? IsFavorite { get; set; }

    public virtual ICollection<ClothingLook> ClothingLooks { get; set; } = new List<ClothingLook>();

    public virtual User IdUserNavigation { get; set; } = null!;
}
