namespace Morphia.Example.DTOs;

public class EmployeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Position { get; set; } = "";
    //
    public Guid CompanyId { get; set; }
    public CompanyDto? Company { get; set; }
}