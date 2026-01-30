using Microsoft.AspNetCore.Mvc;

namespace Sistema_Ferreteria.Controllers{
    public class PosController : Controller{
        public IActionResult Index(){
            return View();
        }
    }
}