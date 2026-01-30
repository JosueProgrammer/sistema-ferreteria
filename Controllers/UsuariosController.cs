using Microsoft.AspNetCore.Mvc;

namespace Sistema_Ferreteria.Controllers
{
    public class UsuariosController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
