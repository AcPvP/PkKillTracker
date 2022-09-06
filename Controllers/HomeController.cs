using Microsoft.AspNetCore.Mvc;

namespace PkKillTracker.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("AllKills", "PkKills");
        }
    }
}
