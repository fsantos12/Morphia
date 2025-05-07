using Microsoft.EntityFrameworkCore;
using Morphia.Core.Repositories;
using Morphia.Example.Context;
using Morphia.Example.Models;

namespace Morphia.Example.Repositories;

public class CompanyRepository : MorphRepository<Company, Guid>
{
    public CompanyRepository(ApiDbContext context) : base(context) {}

    public override IQueryable<Company> ApplyIncludes(IQueryable<Company> query)
    {
        return query.Include(x => x.Employees);
    }
}