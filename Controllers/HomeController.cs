using Microsoft.AspNetCore.Mvc;

namespace PkKillTracker.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(bool isHello)
        {
            if (isHello)
            {
                return Json(new { msg = "Hello from PkKillTracker" });
            }
            else
            {
                return RedirectToAction("AllKills", "PkKills");
            }
        }
    }
}
