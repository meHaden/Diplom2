using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutfitPlaner_Applcation.Data;
using OutfitPlaner_Applcation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[Route("CapsuleWardrobe")]
[Authorize]
public class GenerateThemedOutfitsController : Controller
{
    private readonly WardrobeDbContext _context;
    private readonly ILogger<GenerateThemedOutfitsController> _logger;

    public GenerateThemedOutfitsController(
        WardrobeDbContext context,
        ILogger<GenerateThemedOutfitsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("ThemedOutfits")]
    public async Task<IActionResult> ThemedOutfits()
    {
        try
        {
            var userId = GetCurrentUserId();
            var userClothes = await _context.Clothing
                .Where(c => c.IdUser == userId)
                .AsNoTracking()
                .ToListAsync();

            return View("~/Views/Capsule/ThemedOutfits.cshtml", userClothes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка в ThemedOutfits");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpPost("GenerateThemedOutfits")]
    public async Task<IActionResult> GenerateThemedOutfits([FromBody] ThemedOutfitRequest request)
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

            // Разделение по категориям с расширенным списком типов
            var tops = clothes
                .Where(c => new[] { "Футболка", "Рубашка", "Блузка", "Топ", "Свитер", "Кардиган", "Пиджак", "Кофта" }.Contains(c.Type))
                .ToList();

            var bottoms = clothes
                .Where(c => new[] { "Джинсы", "Брюки", "Юбка", "Шорты", "Платье", "Комбинезон" }.Contains(c.Type))
                .ToList();

            var shoes = clothes
                .Where(c => new[] { "Туфли", "Кроссовки", "Ботинки", "Сапоги", "Босоножки", "Лоферы" }.Contains(c.Type))
                .ToList();

            var accessories = clothes
                .Where(c => new[] { "Сумка", "Ремень", "Шарф", "Галстук", "Шляпа", "Украшения" }.Contains(c.Type))
                .ToList();

            if (!tops.Any() || !bottoms.Any())
            {
                return Json(new
                {
                    success = false,
                    error = "Недостаточно вещей для создания образов",
                    details = new
                    {
                        TopsCount = tops.Count,
                        BottomsCount = bottoms.Count,
                        ShoesCount = shoes.Count,
                        AccessoriesCount = accessories.Count
                    }
                });
            }

            var outfits = new List<dynamic>();

            foreach (var top in tops)
            {
                foreach (var bottom in bottoms)
                {
                    // Фильтрация по цвету, если выбран
                    if (request.Color != null && request.Color != "all")
                    {
                        if (!(top.Color?.Contains(request.Color) == true ||
                              bottom.Color?.Contains(request.Color) == true))
                        {
                            continue;
                        }
                    }

                    var matchScore = CalculateThemedMatchScore(top, bottom, request.Theme);
                    if (matchScore < 30) continue;

                    // Подбор обуви
                    var matchedShoes = shoes
                        .Where(s => s.Style == top.Style || s.Style == bottom.Style)
                        .OrderByDescending(s => CalculateShoesMatchScore(s, top, bottom))
                        .FirstOrDefault();

                    // Подбор аксессуаров (до 3)
                    var matchedAccessories = accessories
                        .Where(a => a.Style == top.Style || a.Style == bottom.Style)
                        .OrderByDescending(a => CalculateAccessoryMatchScore(a, top, bottom))
                        .Take(3)
                        .Select(a => a.Type)
                        .ToList();

                    outfits.Add(new
                    {
                        Top = new
                        {
                            Image = GetImageUrl(top.ImageUrl),
                            Type = top.Type,
                            Color = top.Color,
                            Style = top.Style,
                            Season = top.Season
                        },
                        Bottom = new
                        {
                            Image = GetImageUrl(bottom.ImageUrl),
                            Type = bottom.Type,
                            Color = bottom.Color,
                            Style = bottom.Style,
                            Season = bottom.Season
                        },
                        Shoes = matchedShoes != null ? new
                        {
                            Image = GetImageUrl(matchedShoes.ImageUrl),
                            Type = matchedShoes.Type,
                            Color = matchedShoes.Color
                        } : null,
                        Accessories = new
                        {
                            Items = matchedAccessories
                        },
                        MatchScore = matchScore,
                        ThemeDescription = GetThemeDescription(request.Theme, top, bottom),
                        Theme = request.Theme
                    });
                }
            }

            if (!outfits.Any())
            {
                return Json(new
                {
                    success = false,
                    error = "Не найдено подходящих образов для выбранных параметров",
                    details = "Попробуйте изменить критерии поиска"
                });
            }

            // Сортировка и ограничение количества
            var orderedOutfits = outfits
                .OrderByDescending(o => o.MatchScore)
                .Take(15)
                .ToList();

            return Json(new
            {
                success = true,
                outfits = orderedOutfits
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GenerateThemedOutfits error");
            return Json(new
            {
                success = false,
                error = "Ошибка генерации образов",
                details = ex.Message
            });
        }
    }

    private int CalculateThemedMatchScore(Clothing top, Clothing bottom, string theme)
    {
        int score = 0;

        // Базовые критерии (40%)
        if (ColorsMatch(top.Color, bottom.Color))
            score += 40;

        // Совпадение стилей (30%)
        if (!string.IsNullOrEmpty(top.Style) && top.Style == bottom.Style)
            score += 30;

        // Тематические критерии (30%)
        switch (theme)
        {
            case "all":
                // Для общего случая учитываем сезонность
                if (!string.IsNullOrEmpty(top.Season) && top.Season == bottom.Season)
                    score += 30;
                break;

            case "newyear":
                // Для нового года предпочтительнее праздничные цвета
                if (new[] { "Красный", "Золотой", "Серебряный", "Зеленый", "Белый" }.Contains(top.Color))
                    score += 20;
                if (new[] { "Вечерний", "Праздничный", "Гламурный" }.Contains(top.Style))
                    score += 10;
                break;

            case "romance":
                // Для романтики - пастельные и нежные цвета
                if (new[] { "Розовый", "Белый", "Лавандовый", "Голубой", "Персиковый" }.Contains(top.Color))
                    score += 20;
                if (new[] { "Романтический", "Женственный", "Нежный" }.Contains(top.Style))
                    score += 10;
                break;

            case "business":
                // Для бизнеса - строгие цвета
                if (new[] { "Черный", "Серый", "Синий", "Белый", "Бежевый", "Коричневый" }.Contains(top.Color))
                    score += 20;
                if (new[] { "Классический", "Офисный", "Деловой" }.Contains(top.Style))
                    score += 10;
                break;

            case "birthday":
                // Для дня рождения - яркие цвета
                if (new[] { "Ярко-розовый", "Желтый", "Бирюзовый", "Фиолетовый", "Красный" }.Contains(top.Color))
                    score += 20;
                if (new[] { "Повседневный", "Праздничный", "Яркий" }.Contains(top.Style))
                    score += 10;
                break;
        }

        return score;
    }

    private int CalculateShoesMatchScore(Clothing shoes, Clothing top, Clothing bottom)
    {
        int score = 0;

        // Совпадение стиля (50%)
        if (shoes.Style == top.Style || shoes.Style == bottom.Style)
            score += 50;

        // Сочетание цветов (30%)
        if (ColorsMatch(shoes.Color, top.Color) || ColorsMatch(shoes.Color, bottom.Color))
            score += 30;

        // Сезонность (20%)
        if (shoes.Season == top.Season || shoes.Season == bottom.Season)
            score += 20;

        return score;
    }

    private int CalculateAccessoryMatchScore(Clothing accessory, Clothing top, Clothing bottom)
    {
        int score = 0;

        // Совпадение стиля (60%)
        if (accessory.Style == top.Style || accessory.Style == bottom.Style)
            score += 60;

        // Сочетание цветов (40%)
        if (ColorsMatch(accessory.Color, top.Color) || ColorsMatch(accessory.Color, bottom.Color))
            score += 40;

        return score;
    }

    private string GetThemeDescription(string theme, Clothing top, Clothing bottom)
    {
        switch (theme)
        {
            case "newyear":
                return "Идеальный новогодний образ! Сочетание праздничных цветов и стиля сделает вас королевой вечера. " +
                       "Добавьте немного блеска с помощью аксессуаров для завершенного образа.";

            case "romance":
                return "Романтический образ для особенного свидания. Нежные цвета и женственные детали создадут нужное настроение. " +
                       "Дополните образ изящными украшениями и лёгким парфюмом.";

            case "business":
                return "Строгий деловой лук для важных встреч и переговоров. Классическое сочетание цветов подчеркнёт ваш профессионализм. " +
                       "Добавьте качественные аксессуары для завершённого образа.";

            case "birthday":
                return "Яркий и праздничный образ для дня рождения! Этот лук подчеркнёт вашу индивидуальность и создаст настроение праздника. " +
                       "Не бойтесь экспериментировать с аксессуарами!";

            default:
                return "Универсальный образ, который подойдёт для многих случаев. " +
                       "Вы можете дополнить его аксессуарами в зависимости от конкретного события.";
        }
    }

    private bool ColorsMatch(string color1, string color2)
    {
        if (string.IsNullOrWhiteSpace(color1) || string.IsNullOrWhiteSpace(color2))
            return false;

        color1 = color1.Trim();
        color2 = color2.Trim();

        var colorCombinations = new Dictionary<string, List<string>>
        {
            ["Белый"] = new List<string> { "Черный", "Синий", "Красный", "Серый", "Зеленый", "Розовый", "Бежевый" },
            ["Черный"] = new List<string> { "Белый", "Серый", "Красный", "Розовый", "Золотой", "Серебряный" },
            ["Серый"] = new List<string> { "Черный", "Белый", "Розовый", "Синий", "Желтый" },
            ["Синий"] = new List<string> { "Белый", "Бежевый", "Серый", "Голубой", "Желтый" },
            ["Бежевый"] = new List<string> { "Синий", "Коричневый", "Белый", "Черный", "Зеленый" },
            ["Красный"] = new List<string> { "Белый", "Черный", "Золотой", "Серый", "Джинсовый" },
            ["Розовый"] = new List<string> { "Белый", "Серый", "Черный", "Бежевый", "Голубой" },
            ["Зеленый"] = new List<string> { "Белый", "Бежевый", "Коричневый", "Золотой" },
            ["Коричневый"] = new List<string> { "Бежевый", "Бирюзовый", "Зеленый", "Кремовый" },
            ["Желтый"] = new List<string> { "Серый", "Синий", "Фиолетовый", "Белый" }
        };

        return (colorCombinations.ContainsKey(color1) && colorCombinations[color1].Contains(color2)) ||
               (colorCombinations.ContainsKey(color2) && colorCombinations[color2].Contains(color1));
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

public class ThemedOutfitRequest
{
    public string Theme { get; set; }
    public string Color { get; set; }
}