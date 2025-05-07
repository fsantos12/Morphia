using Microsoft.AspNetCore.Mvc;
using Morphia.Core.Controllers;
using Morphia.Example.DTOs;
using Morphia.Example.Models;
using Morphia.Example.Repositories;

namespace Morphia.Example.Controllers;

[ApiController]
[Route("/api/companies")]
public class CompanyController : MorphController<Company, Guid>
{
    public CompanyController(ILogger<MorphController<Company, Guid>> logger, CompanyRepository repository) : base(logger, repository)
    {
    }
}