using Microsoft.AspNetCore.Mvc;

namespace Sistema_Ferreteria.Controllers
{
    public class ReportesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
