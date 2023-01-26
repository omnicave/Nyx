using KubeOpsOrleans.Services;
using Microsoft.AspNetCore.Mvc;

namespace KubeOpsOrleans.Controllers;

[ApiController]
[Route("[controller]")]
public class TemplateGeneratorController : Controller
{
    private readonly IHomerConfigFileGenerator _homerConfigFileGenerator;

    public TemplateGeneratorController(IHomerConfigFileGenerator homerConfigFileGenerator)
    {
        _homerConfigFileGenerator = homerConfigFileGenerator;
    }
    
    [Route("/homer")]
    [HttpGet]
    public async Task<IActionResult> Homer()
    {
        // var s = await _homerConfigFileGenerator.RenderTemplate();
        // return File(s, "application/yaml");

        throw new NotImplementedException();
    }
}