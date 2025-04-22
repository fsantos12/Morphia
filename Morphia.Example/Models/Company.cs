using Morphia.Core.Models;

namespace Morphia.Example.Models;

public class Company : MorphModel<Guid>
{
    public string Name { get; set; } = "";
    
    public virtual ICollection<Employe> Employees { get; set; } = [];

    public Company() : this(default) {}

    public Company(Guid id) : base(id)
    {
    }
}