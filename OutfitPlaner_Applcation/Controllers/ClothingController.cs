using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutfitPlaner_Applcation.Data;
using OutfitPlaner_Applcation.Models;
using System.Security.Claims;

[Authorize]
[Route("ProfileWardrobe/Clothing")]
public class ClothingController : Controller
{
    private readonly WardrobeDbContext _context;
    private readonly ILogger<ClothingController> _logger;

    public ClothingController(
        WardrobeDbContext context,
        ILogger<ClothingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Route("")]
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Attempting to access Clothing page");

        try
        {
            // Получаем email 
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("Email claim not found");
                return Unauthorized();
            }

            _logger.LogInformation($"Found user email: {userEmail}");

            var dbUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (dbUser == null)
            {
                _logger.LogWarning("DB User not found for email: {Email}", userEmail);
                return Unauthorized("Пользователь не найден");
            }

            _logger.LogInformation($"Found DB User with ID: {dbUser.Id}");

            var clothingItems = await _context.Clothing
                .Where(c => c.IdUser == dbUser.Id)
                .OrderByDescending(c => c.AddedAt)
                .ToListAsync();

            return View("~/Views/ProfileWardrobe/Clothing.cshtml", clothingItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Clothing/Index");
            throw;
        }
    }
}