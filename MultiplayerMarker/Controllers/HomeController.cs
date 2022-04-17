namespace MultiplayerMarker.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using MultiplayerMarker.Core;
    using MultiplayerMarker.DbModel;
    using System.Linq;
    using System.Text.Json;

    public class HomeController: Controller
    {
        private readonly ApplicationContext dbContext;

        public HomeController(ApplicationContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public IActionResult Index()
        {
            ViewData["ActionList"] = this.dbContext.UserActions.ToArray();
            return View();
        }

        public IActionResult ActionList()
        {
            return new JsonResult(this.dbContext.UserActions, new System.Text.Json.JsonSerializerOptions() {PropertyNamingPolicy = null});
        }
    }
}
