using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutfitPlaner_Applcation.Data;
using OutfitPlaner_Applcation.Models;
using System.Security.Claims;

[Route("CapsuleWardrobe")]
[Authorize]
public class CapsuleWardrobeController : Controller
{
    private readonly WardrobeDbContext _context;
    private readonly ILogger<CapsuleWardrobeController> _logger;

    // Определения категорий на основе ваших данных
    private static readonly string[] TopTypes = { "Футболка", "Рубашка", "Блузка", "Платье" };
    private static readonly string[] BottomTypes = { "Брюки", "Джинсы", "Юбка" };
    private static readonly string[] OuterwearTypes = { "Верхняя одежда" };
    private static readonly string[] AccessoryTypes = { "Аксессуары" };
    private static readonly string[] ShoeTypes = { "Обувь" };

    public CapsuleWardrobeController(
        WardrobeDbContext context,
        ILogger<CapsuleWardrobeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("GenerateCapsules")]
    public async Task<IActionResult> GenerateCapsules()
    {
        try
        {
            var userId = GetCurrentUserId();
            var userClothes = await _context.Clothing
                .Where(c => c.IdUser == userId)
                .AsNoTracking()
                .ToListAsync();

            return View("~/Views/Capsule/GenerateCapsules.cshtml", userClothes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка в GenerateCapsules");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpPost("GenerateOutfits")]
    public async Task<IActionResult> GenerateOutfits()
    {
        try
        {
            var userId = GetCurrentUserId();
            var clothes = await _context.Clothing
                .Where(c => c.IdUser == userId &&
                           !string.IsNullOrEmpty(c.ImageUrl) &&
                           !string.IsNullOrEmpty(c.Type))
                .AsNoTracking()
                .ToListAsync();

            // Категоризация одежды
            var tops = clothes.Where(c => TopTypes.Contains(c.Type)).ToList();
            var bottoms = clothes.Where(c => BottomTypes.Contains(c.Type)).ToList();
            var outerwear = clothes.Where(c => OuterwearTypes.Contains(c.Type)).ToList();
            var accessories = clothes.Where(c => AccessoryTypes.Contains(c.Type)).ToList();
            var shoes = clothes.Where(c => ShoeTypes.Contains(c.Type)).ToList();

            if (!tops.Any() || (!bottoms.Any() && !tops.Any(t => t.Type == "Платье")))
            {
                return Json(new
                {
                    success = false,
                    error = "Недостаточно вещей для создания капсул",
                    details = new
                    {
                        TopsCount = tops.Count,
                        BottomsCount = bottoms.Count,
                        DressesCount = tops.Count(t => t.Type == "Платье"),
                        AllItems = clothes.Count
                    }
                });
            }

            var outfits = new List<dynamic>();

            // Генерация комплектов
            foreach (var top in tops)
            {
                if (top.Type == "Платье")
                {
                    // Обработка платьев
                    var dressOutfit = CreateOutfitObject(top, null, null, null, null, 80);
                    outfits.Add(dressOutfit);

                    // Варианты с верхней одеждой
                    foreach (var jacket in outerwear)
                    {
                        if (ColorsMatch(top.Color, jacket.Color))
                        {
                            outfits.Add(CreateOutfitObject(top, null, jacket, null, null, 90));
                        }
                    }
                    continue;
                }

                foreach (var bottom in bottoms)
                {
                    var matchScore = CalculateMatchScore(top, bottom);
                    if (matchScore < 30) continue;

                    // Базовый комплект
                    outfits.Add(CreateOutfitObject(top, bottom, null, null, null, matchScore));

                    // Варианты с верхней одеждой
                    foreach (var jacket in outerwear)
                    {
                        if (ColorsMatch(top.Color, jacket.Color) || ColorsMatch(bottom.Color, jacket.Color))
                        {
                            outfits.Add(CreateOutfitObject(top, bottom, jacket, null, null, matchScore + 15));
                        }
                    }
                }
            }

            if (!outfits.Any())
            {
                return Json(new
                {
                    success = false,
                    error = "Не найдено подходящих сочетаний",
                    details = "Попробуйте добавить вещи разных цветов и стилей"
                });
            }

            return Json(new
            {
                success = true,
                outfits = outfits.OrderByDescending(o => o.MatchScore).Take(50).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GenerateOutfits error");
            return Json(new
            {
                success = false,
                error = "Ошибка генерации комплектов",
                details = ex.Message
            });
        }
    }

    private dynamic CreateOutfitObject(Clothing top, Clothing bottom, Clothing outerwear, Clothing accessory, Clothing shoes, int score)
    {
        return new
        {
            Top = new
            {
                Id = top.Id,
                Image = GetImageUrl(top.ImageUrl),
                Type = top.Type,
                Color = top.Color,
                Style = top.Style,
                Season = top.Season,
                Material = top.Material
            },
            Bottom = bottom != null ? new
            {
                Id = bottom.Id,
                Image = GetImageUrl(bottom.ImageUrl),
                Type = bottom.Type,
                Color = bottom.Color,
                Style = bottom.Style,
                Season = bottom.Season,
                Material = bottom.Material
            } : null,
            Outerwear = outerwear != null ? new
            {
                Id = outerwear.Id,
                Image = GetImageUrl(outerwear.ImageUrl),
                Type = outerwear.Type,
                Color = outerwear.Color,
                Style = outerwear.Style,
                Season = outerwear.Season,
                Material = outerwear.Material
            } : null,
            MatchScore = score
        };
    }

    private int CalculateMatchScore(Clothing top, Clothing bottom)
    {
        int score = 0;

        // Сочетание цветов (50 баллов)
        if (ColorsMatch(top.Color, bottom.Color))
            score += 50;

        // Совпадение стилей (30 баллов)
        if (top.Style == bottom.Style)
            score += 30;

        // Совпадение сезонов (20 баллов)
        if (top.Season == bottom.Season)
            score += 20;

        return score;
    }

    private bool ColorsMatch(string color1, string color2)
    {
        if (string.IsNullOrWhiteSpace(color1) || string.IsNullOrWhiteSpace(color2))
            return false;

        color1 = color1.Trim();
        color2 = color2.Trim();

        // Мультиколор сочетается со всем
        if (color1 == "Мультиколор" || color2 == "Мультиколор")
            return true;

        // Если цвета одинаковые
        if (color1 == color2)
            return true;

        // Полная матрица сочетаний цветов по вашим требованиям
        var colorCombinations = new Dictionary<string, List<string>>
        {
            ["Белый"] = new List<string> { "Черный", "Красный", "Синий", "Зеленый", "Желтый", "Розовый", "Фиолетовый", "Серый", "Бежевый", "Коричневый" },
            ["Черный"] = new List<string> { "Белый", "Красный", "Розовый", "Фиолетовый", "Серый", "Бежевый", "Зеленый", "Желтый" },
            ["Красный"] = new List<string> { "Белый", "Черный", "Синий", "Зеленый", "Желтый", "Серый", "Бежевый", "Коричневый" },
            ["Синий"] = new List<string> { "Белый", "Красный", "Желтый", "Зеленый", "Серый", "Бежевый", "Коричневый" },
            ["Зеленый"] = new List<string> { "Белый", "Черный", "Красный", "Синий", "Желтый", "Коричневый", "Бежевый" },
            ["Желтый"] = new List<string> { "Белый", "Черный", "Красный", "Синий", "Зеленый", "Фиолетовый", "Коричневый" },
            ["Розовый"] = new List<string> { "Белый", "Черный", "Серый", "Бежевый", "Фиолетовый" },
            ["Фиолетовый"] = new List<string> { "Белый", "Черный", "Желтый", "Розовый", "Серый" },
            ["Серый"] = new List<string> { "Белый", "Черный", "Красный", "Розовый", "Фиолетовый", "Синий", "Зеленый" },
            ["Бежевый"] = new List<string> { "Белый", "Черный", "Красный", "Синий", "Зеленый", "Коричневый" },
            ["Коричневый"] = new List<string> { "Белый", "Черный", "Красный", "Зеленый", "Желтый", "Бежевый" }
        };

        return colorCombinations.TryGetValue(color1, out var matches) && matches.Contains(color2) ||
               colorCombinations.TryGetValue(color2, out matches) && matches.Contains(color1);
    }

    private string GetImageUrl(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
            return "/images/no-image.png";

        imagePath = imagePath.Trim();

        if (imagePath.StartsWith("/") || imagePath.StartsWith("http"))
            return imagePath;

        return $"/uploads/clothing/{imagePath}";
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    }
}