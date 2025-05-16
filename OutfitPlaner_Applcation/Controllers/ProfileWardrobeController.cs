using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OutfitPlaner_Applcation.Data;
using OutfitPlaner_Applcation.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OutfitPlaner_Applcation.Controllers
{
    public class ProfileWardrobeController : Controller
    {
        private readonly WardrobeDbContext _context;

        public ProfileWardrobeController(WardrobeDbContext context)
        {
            _context = context;
        }

        public IActionResult MyProfile()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userId, out int userIdInt))
                return View("Error", "Ошибка данных пользователя");

            var user = _context.Users.FirstOrDefault(u => u.Id == userIdInt);

            if (user == null)
                return View("Error", "Пользователь не найден");

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userId, out int userIdInt))
            {
                TempData["ErrorMessage"] = "Неверный ID пользователя.";
                return RedirectToAction("MyProfile");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userIdInt);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Пользователь не найден.";
                return RedirectToAction("MyProfile");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await HttpContext.SignOutAsync();
                return RedirectToAction("Register", "Account");
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Ошибка при удалении аккаунта.";
                return RedirectToAction("MyProfile");
            }
        }

        public IActionResult MyWardrobe()
        {
            return View();
        }
    }
}