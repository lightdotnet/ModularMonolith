using Microsoft.AspNetCore.Mvc;

namespace StarterKit.Host.Controllers;

#if DEBUG
[Route("/hello")]
[ApiExplorerSettings(IgnoreApi = true)]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return Redirect("~/swagger");
    }
}
#endif
