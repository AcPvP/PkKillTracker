using Microsoft.AspNetCore.Mvc;
using PkKillTracker.Models;
using PkKillTracker.DataAccess;
using System.Diagnostics;
using System.Text.Json;

namespace PkKillTracker.Controllers
{
    public class PkKillsController : Controller
    {
        private readonly ILogger<PkKillsController> _logger;
        private static DateTime? LastUpdatedPkKills;
        private static DateTime? LastUpdatedKillsDeathsByChar;
        private static DateTime? LastUpdatedKillsDeathsByClan;
        private static Dictionary<string, List<PkKill>>? CachedPkKills;
        private static List<Character>? CachedCharacters;
        private static List<Clan>? CachedClans;

        public PkKillsController(ILogger<PkKillsController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction("AllKills");
        }

        public IActionResult AllKills(string playerFilter = "", DateTime? monthDateFilter = null)
        {
            ViewBag.KillsData = FetchPkKillsData(playerFilter, monthDateFilter);
            return View();
        }

        public List<PkKill> FetchPkKillsData(string playerFilter = "", DateTime? monthDateFilter = null)
        {
            var monthDateFilterString = monthDateFilter?.ToString("MMMM yyyy") ?? "";

            bool fetchData = false;

            if (CachedPkKills == null)
            {
                CachedPkKills = new Dictionary<string, List<PkKill>>();
                fetchData = true;
            }

            List<PkKill> pkKills = CachedPkKills.ContainsKey(DateTime.Today.ToString("MMMM yyyy")) ? CachedPkKills[DateTime.Today.ToString("MMMM yyyy")] : new List<PkKill>();

            if (!LastUpdatedPkKills.HasValue)
            {
                LastUpdatedPkKills = DateTime.Now;
                fetchData = true;
            }

            if (!string.IsNullOrEmpty(playerFilter))
            {
                if (CachedPkKills.ContainsKey(playerFilter))
                {
                    pkKills = CachedPkKills[playerFilter];
                }
                else
                {
                    fetchData = true;
                }
            }
            else
            {
                if (CachedPkKills.ContainsKey(monthDateFilterString))
                {
                    pkKills = CachedPkKills[monthDateFilterString];
                }
                else
                {
                    fetchData = true;
                }
            }

            if (DateTime.Now.AddMinutes(-1) > (LastUpdatedPkKills ?? DateTime.Now.AddMinutes(-2)))
            {
                fetchData = true;
            }

            if (fetchData)
            {
                if (!string.IsNullOrEmpty(playerFilter))
                {
                    pkKills = PkKillsDataAccess.GetPkKillsByPlayer(playerFilter);
                    if (pkKills != null && pkKills.Count > 0)
                    {
                        CachedPkKills[playerFilter] = pkKills;
                        LastUpdatedPkKills = DateTime.Now;
                    }
                }
                else if (monthDateFilter.HasValue)
                {
                    pkKills = PkKillsDataAccess.GetPkKillsByMonth(monthDateFilter.Value);
                    CachedPkKills[monthDateFilterString] = pkKills;
                    LastUpdatedPkKills = DateTime.Now;
                }
                else if (CachedPkKills.Count == 0)
                {
                    pkKills = PkKillsDataAccess.GetPkKillsByMonth(DateTime.Today);
                    CachedPkKills[DateTime.Today.ToString("MMMM yyyy")] = pkKills;
                    ViewBag.FilterString = DateTime.Today.ToString("MMMM yyyy");
                    LastUpdatedPkKills = DateTime.Now;
                }
            }

            return pkKills;
        }

        public JsonResult FetchPkKillsGridData(string playerFilter = "", DateTime? monthDateFilter = null)
        {
            var pkKills = FetchPkKillsData(playerFilter, monthDateFilter);
            var result = JsonSerializer.Serialize(pkKills.ToArray());
            return Json(result);
        }

        public IActionResult KillsByChar()
        {            
            return View();
        }

        public JsonResult FetchKillsByCharGridData()
        {
            List<Character> pkKills = CachedCharacters ?? new List<Character>();
            var fetchData = DateTime.Now.AddMinutes(-1) > (LastUpdatedKillsDeathsByChar ?? DateTime.Now.AddMinutes(-2)) || CachedCharacters == null;
            if (fetchData)
            {
                pkKills = PkKillsDataAccess.GetKillsDeathsByCharacter();
                CachedCharacters = pkKills;
                LastUpdatedKillsDeathsByChar = DateTime.Now;
            }
            var result = JsonSerializer.Serialize(pkKills.ToArray());
            return Json(result);
        }

        public IActionResult KillsByClan()
        {            
            return View();
        }

        public JsonResult FetchKillsByClanGridData()
        {
            List<Clan> pkKills = CachedClans ?? new List<Clan>();
            var fetchData = DateTime.Now.AddMinutes(-1) > (LastUpdatedKillsDeathsByClan ?? DateTime.Now.AddMinutes(-2)) || CachedClans == null;
            if (fetchData)
            {
                pkKills = PkKillsDataAccess.GetKillsDeathsByClan();
                CachedClans = pkKills;
                LastUpdatedKillsDeathsByClan = DateTime.Now;
            }
            var result = JsonSerializer.Serialize(pkKills.ToArray());
            return Json(result);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}