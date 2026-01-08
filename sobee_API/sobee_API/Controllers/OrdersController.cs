using Microsoft.AspNetCore.Mvc;

namespace sobee_API.Controllers
{
    public class OrdersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
