using Microsoft.AspNetCore.Mvc;

namespace Sistema_Ferreteria.Controllers
{
    public class InventarioController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
