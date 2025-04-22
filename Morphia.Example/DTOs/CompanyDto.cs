namespace Morphia.Example.DTOs;

public class CompanyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<EmployeDto> Employees { get; set; } = [];
}