using Microsoft.AspNetCore.Mvc;
using Morphia.Core.Controllers;
using Morphia.Example.Models;
using Morphia.Example.Repositories;

namespace Morphia.Example.Controllers;

[ApiController]
[Route("/api/employees")]
public class EmployeController : MorphController<Employe, int>
{
    public EmployeController(ILogger<MorphController<Employe, int>> logger, EmployeRepository repository) : base(logger, repository)
    {
    }
}