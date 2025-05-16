using System;
using System.Collections.Generic;

namespace OutfitPlaner_Applcation.Models;

public partial class ClothingLook
{
    public int Id { get; set; }

    public int IdClothing { get; set; }

    public int IdLook { get; set; }

    public virtual Clothing IdClothingNavigation { get; set; } = null!;

    public virtual ThematicLook IdLookNavigation { get; set; } = null!;
}
