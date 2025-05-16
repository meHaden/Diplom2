using System;
using System.Collections.Generic;

namespace OutfitPlaner_Applcation.Models;

public partial class CapsuleWardrobe
{
    public int Id { get; set; }

    public int IdUser { get; set; }

    public string Name { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public bool? IsFavorite { get; set; }

    public virtual ICollection<ClothingCapsule> ClothingCapsules { get; set; } = new List<ClothingCapsule>();

    public virtual User IdUserNavigation { get; set; } = null!;
}
