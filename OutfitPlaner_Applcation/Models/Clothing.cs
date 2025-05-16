using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OutfitPlaner_Applcation.Models;

public class Clothing
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required]
    [Column("ID_User")]
    public int IdUser { get; set; }

    [Required]
    [Column("Image_Url")]
    public string ImageUrl { get; set; } = null!;

    [Required]
    [Column("Type")]
    public string Type { get; set; } = null!;

    [Required]
    [Column("Color")]
    public string Color { get; set; } = null!;

    [Required]
    [Column("Style")]
    public string Style { get; set; } = null!;

    [Required]
    [Column("Material")]
    public string Material { get; set; } = null!;

    [Required]
    [Column("Season")]
    public string Season { get; set; } = null!;

    [Column("Condition")]
    public int? Condition { get; set; }

    [Column("Added_at")]
    public DateTime? AddedAt { get; set; }

    // Навигационное свойство
    public virtual User User { get; set; } = null!;

    public virtual ICollection<ClothingCapsule> ClothingCapsules { get; set; } = new List<ClothingCapsule>();
    public virtual ICollection<ClothingLook> ClothingLooks { get; set; } = new List<ClothingLook>();
}