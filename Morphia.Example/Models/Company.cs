using Morphia.Core.Models;

namespace Morphia.Example.Models;

public class Company : MorphModel<Guid>
{
    public string Name { get; set; } = "";
    
    public virtual ICollection<Employe> Employees { get; set; } = [];

    public Company() : this(Guid.NewGuid()) {}

    public Company(Guid id) : base(id == default ? Guid.NewGuid() : id)
    {
    }
}