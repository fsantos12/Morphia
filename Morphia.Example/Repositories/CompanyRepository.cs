using Microsoft.EntityFrameworkCore;
using Morphia.Core.Repositories;
using Morphia.Core.Repositories.Queries;
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

    protected override async Task<Company> BeforeAdd(Company model, CancellationToken cancellationToken = default)
    {
        var filter = new FilterDefinition<Company>().Contains(x => x.Name, "Delta");
        var sort = new SortDefinition<Company>().Descending("name").Ascending(x => x.CreatedAt);
        var projection = new ProjectionDefinition<Company>().Include(x => x.Name);

        var list = await FindAsync(filter, sort, projection, cancellationToken: cancellationToken);
        foreach (var c in list)
        {
            Console.WriteLine(c.Name);
            Console.WriteLine(c.ID);
            Console.WriteLine(c.CreatedAt);
        }

        return await base.BeforeAdd(model, cancellationToken);
    }
}