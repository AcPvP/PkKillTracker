using Microsoft.AspNetCore.Mvc;

namespace PkKillTracker.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return Json(new { msg = "Hello from PkKillTracker" });
            //return RedirectToAction("AllKills", "PkKills");
        }
    }
}
