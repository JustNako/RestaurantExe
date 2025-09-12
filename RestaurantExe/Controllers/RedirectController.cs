// Controllers/RedirectController.cs
using Microsoft.AspNetCore.Mvc;

public class RedirectController : Controller
{
    public IActionResult ToAdmin()
    {
        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }
}
