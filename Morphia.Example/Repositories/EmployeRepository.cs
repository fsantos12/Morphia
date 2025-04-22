using Microsoft.EntityFrameworkCore;
using Morphia.Core.Repositories;
using Morphia.Example.Context;
using Morphia.Example.Models;

namespace Morphia.Example.Repositories;

public class EmployeRepository : MorphRepository<Employe, int>
{
    public EmployeRepository(ApiDbContext context) : base(context) {}

    public override IQueryable<Employe> ApplyIncludes(IQueryable<Employe> query)
    {
        return query.Include(x => x.Company);
    }
}