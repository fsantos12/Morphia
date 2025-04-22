using Morphia.Core.Models;

namespace Morphia.Example.Models;

public class Employe : MorphModel<int>
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Position { get; set; } = "";

    public Guid CompanyId { get; set; }
    public virtual Company? Company { get; set; }

    public Employe() : this(0) {}

    public Employe(int id) : base(id)
    {
    }
}