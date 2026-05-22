using Microsoft.AspNetCore.Mvc;

namespace E_learningProject.Web.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Public()
    {
        return View();
    }
}
