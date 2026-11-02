using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GymClub.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMediator _m;
        public HomeController(IMediator m) => _m = m;

        public IActionResult Index() => View();
        public IActionResult About() => View();
        public IActionResult Contact() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}
