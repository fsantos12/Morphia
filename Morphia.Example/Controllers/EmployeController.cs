using Microsoft.AspNetCore.Mvc;
using Morphia.Core.Controllers;
using Morphia.Core.Repositories.Queries;
using Morphia.Example.DTOs;
using Morphia.Example.Models;
using Morphia.Example.Repositories;

namespace Morphia.Example.Controllers;

[ApiController]
[Route("/api/employees")]
public class EmployeController : MorphController<Employe, int, EmployeDto>
{
    public EmployeController(ILogger<MorphController<Employe, int, EmployeDto>> logger, EmployeRepository repository) : base(logger, repository)
    {
    }

    public override EmployeDto ConvertToDto(Employe model)
    {
        return new EmployeDto()
        {
            Id = model.ID,
            Name = model.Name,
            Email = model.Email,
            Phone = model.Phone,
            Position = model.Position,
            CompanyId = model.CompanyId,
            Company = new()
            {
                Id = model.CompanyId,
                Name = model.Company?.Name ?? ""
            }
        };
    }

    public override Employe ConvertToModel(EmployeDto dto)
    {
        return new(dto.Id)
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Position = dto.Position,
            CompanyId = dto.CompanyId
        };
    }
}