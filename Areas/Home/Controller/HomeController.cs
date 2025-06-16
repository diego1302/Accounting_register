using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting_Register_BE.Areas.Home.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Area("home")]
    public class HomeController : Controller
    {
        [HttpGet]
        [Route("/[area]/[action]")]
        public string Index()
        {
            return "hellooooo";
        }
    }
}
