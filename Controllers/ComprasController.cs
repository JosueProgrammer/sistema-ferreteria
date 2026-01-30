using Microsoft.AspNetCore.Mvc;

namespace Sistema_Ferreteria.Controllers
{
    public class ComprasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
