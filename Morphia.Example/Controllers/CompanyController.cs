using Microsoft.AspNetCore.Mvc;
using Morphia.Core.Controllers;
using Morphia.Example.DTOs;
using Morphia.Example.Models;
using Morphia.Example.Repositories;

namespace Morphia.Example.Controllers;

[ApiController]
[Route("/api/companies")]
public class CompanyController : MorphController<Company, Guid, CompanyDto>
{
    public CompanyController(ILogger<MorphController<Company, Guid, CompanyDto>> logger, CompanyRepository repository) : base(logger, repository)
    {
    }

    public override CompanyDto ConvertToDto(Company model)
    {
        var dto = new CompanyDto()
        {
            Id = model.ID,
            Name = model.Name
        };

        foreach (var employe in model.Employees)
        {
            dto.Employees.Add(new()
            {
                Id = employe.ID,
                Name = employe.Name,
                Email = employe.Email,
                Phone = employe.Phone,
                Position = employe.Position
            });
        }

        return dto;
    }

    public override Company ConvertToModel(CompanyDto dto)
    {
        return new(dto.Id)
        {
            Name = dto.Name
        };
    }
}