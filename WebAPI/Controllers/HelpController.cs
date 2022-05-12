using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("[controller]")]
    public class HelpController : Controller
    {
        [HttpGet("health")]
        public IActionResult Status() => Ok();
    }
}
