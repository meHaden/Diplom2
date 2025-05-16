using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutfitPlaner_Applcation.Data;
using OutfitPlaner_Applcation.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;

namespace OutfitPlaner_Applcation.Controllers
{
    [Authorize]
    public class WardrobeController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly WardrobeDbContext _context;
        private readonly ILogger<WardrobeController> _logger;

        public WardrobeController(
            IWebHostEnvironment environment,
            WardrobeDbContext context,
            ILogger<WardrobeController> logger)
        {
            _environment = environment;
            _context = context;
            _logger = logger;
        }

        [HttpGet("Category/{category}")]
        public async Task<IActionResult> Category(string category)
        {
            try
            {
                var userResult = await GetCurrentUser();
                if (!userResult.Success)
                    return Unauthorized();

                var items = await _context.Clothing
                    .Where(c => c.IdUser == userResult.User.Id && c.Type == category)
                    .OrderByDescending(c => c.AddedAt)
                    .ToListAsync();

                ViewBag.CategoryName = category;
                return View(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка в Category/{category}");
                return StatusCode(500);
            }
        }

        [HttpPost("/ProfileWardrobe/AddClothingItem")]
        [ValidateAntiForgeryToken]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddClothingItem([FromForm] ClothingItemRequest request)
        {
            try
            {
                _logger.LogInformation("Начало обработки AddClothingItem");
                _logger.LogInformation($"Полученные данные: {JsonSerializer.Serialize(request)}");
                _logger.LogInformation($"Файл получен: {request.ImageFile?.FileName}, размер: {request.ImageFile?.Length}");

                // Валидация модели
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Невалидная модель: " + string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)));

                    return BadRequest(new
                    {
                        success = false,
                        message = "Неверные данные",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                var fileResult = await SaveUploadedFile(request.ImageFile);
                if (!fileResult.Success)
                {
                    _logger.LogWarning($"Ошибка сохранения файла: {fileResult.Message}");
                    return BadRequest(new
                    {
                        success = false,
                        message = fileResult.Message
                    });
                }

                var userResult = await GetCurrentUser();
                if (!userResult.Success)
                {
                    _logger.LogWarning($"Пользователь не найден: {userResult.Message}");
                    return Unauthorized(new
                    {
                        success = false,
                        message = userResult.Message
                    });
                }

                // Создание новой записи
                var clothing = new Clothing
                {
                    IdUser = userResult.User.Id,
                    Type = request.ItemType,
                    ImageUrl = fileResult.FileUrl,
                    Color = request.Color,
                    Style = request.Style,
                    Material = request.Material,
                    Season = request.Season,
                    Condition = request.Condition,
                    AddedAt = DateTime.UtcNow
                };

                _logger.LogInformation($"Создана новая вещь: {JsonSerializer.Serialize(clothing)}");

                _context.Clothing.Add(clothing);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Вещь успешно сохранена с ID: {clothing.Id}");

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = clothing.Id,
                        imageUrl = clothing.ImageUrl,
                        type = clothing.Type
                    },
                    message = "Вещь успешно добавлена в гардероб"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении элемента одежды");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера",
                    detail = ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult Clothing()
        {
            return View();
        }

        private async Task<(bool Success, string FileUrl, string Message)> SaveUploadedFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return (false, null, "Файл не был загружен");

                if (file.Length > 5 * 1024 * 1024)
                    return (false, null, "Размер файла не должен превышать 5MB");

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                    return (false, null, "Допустимы только файлы изображений (JPG, JPEG, PNG, WEBP)");

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "clothing");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return (true, $"/uploads/clothing/{uniqueFileName}", "Файл успешно сохранен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении файла");
                return (false, null, $"Ошибка при сохранении файла: {ex.Message}");
            }
        }

        private async Task<(bool Success, User User, string Message)> GetCurrentUser()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                    return (false, null, "Email пользователя не найден в токене");

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return (false, null, "Пользователь не найден в базе данных");

                return (true, user, "Пользователь успешно найден");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении текущего пользователя");
                return (false, null, $"Ошибка при получении пользователя: {ex.Message}");
            }
        }
    }

    public class ClothingItemRequest
    {
        [Required(ErrorMessage = "Тип одежды обязателен")]
        public string ItemType { get; set; }

        [Required(ErrorMessage = "Изображение обязательно")]
        public IFormFile ImageFile { get; set; }

        [Required(ErrorMessage = "Цвет обязателен")]
        public string Color { get; set; }

        [Required(ErrorMessage = "Стиль обязателен")]
        public string Style { get; set; }

        [Required(ErrorMessage = "Материал обязателен")]
        public string Material { get; set; }

        [Required(ErrorMessage = "Сезон обязателен")]
        public string Season { get; set; }

        [Range(1, 5, ErrorMessage = "Состояние должно быть от 1 до 5")]
        public int Condition { get; set; }
    }
}