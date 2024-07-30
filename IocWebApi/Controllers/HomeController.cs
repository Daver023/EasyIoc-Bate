using Microsoft.AspNetCore.Mvc;

namespace IocWebApi.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
