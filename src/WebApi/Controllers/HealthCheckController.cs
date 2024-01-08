using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Route("api/[controller]")]
public class HealthCheckController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult Status()
    {
        //Incluir qualquer regra adicional que seja necessário
        //para verificar a saúde do serviço
        return Ok();
    }
}
