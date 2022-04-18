namespace MultiplayerMarker.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using MultiplayerMarker.DbModel;
    using System.Linq;

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

        /// <summary>
        /// Получить список пользоавтельских действий.
        /// TODO: отправлять изменения по мере их наступления
        /// </summary>
        /// <returns></returns>
        public IActionResult ActionList()
        {
            return new JsonResult(this.dbContext.UserActions, new System.Text.Json.JsonSerializerOptions() {PropertyNamingPolicy = null});
        }
    }
}
