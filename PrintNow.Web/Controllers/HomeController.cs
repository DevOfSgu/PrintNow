using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PrintNow.Web.Models;

namespace PrintNow.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            if (User.IsInRole("ShopOwner")) return RedirectToAction("Index", "ShopAdmin");
            return RedirectToAction("Index", "Customer");
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
