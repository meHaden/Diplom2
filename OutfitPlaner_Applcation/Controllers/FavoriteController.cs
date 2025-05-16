using Microsoft.AspNetCore.Mvc;

public class FavoriteController : Controller
{
    public IActionResult Favorite()
    {
        return View();
    }
}