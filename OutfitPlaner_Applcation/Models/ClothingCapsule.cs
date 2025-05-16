using System;
using System.Collections.Generic;

namespace OutfitPlaner_Applcation.Models;

public partial class ClothingCapsule
{
    public int Id { get; set; }

    public int IdClothing { get; set; }

    public int IdCapsuleWardrobe { get; set; }

    public virtual CapsuleWardrobe IdCapsuleWardrobeNavigation { get; set; } = null!;

    public virtual Clothing IdClothingNavigation { get; set; } = null!;
}
