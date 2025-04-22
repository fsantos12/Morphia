using Microsoft.EntityFrameworkCore;
using Morphia.Core.Models;
using Morphia.Core.Repositories.Queries;
using Morphia.Core.Repositories.Exceptions;

namespace Morphia.Core.Repositories;

public class MorphRepository<T, K> where T : MorphModel<K> where K : IEquatable<K>
{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _set;

    public MorphRepository(DbContext context)
    {
        _context = context;
        _set = _context.Set<T>();
    }

    /*
     * Save Changes
     */
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

    /*
     * Add
     */
    public async Task<T> AddAsync(T model, CancellationToken cancellationToken = default) => await AfterAdd(await Add(await BeforeAdd(model, cancellationToken), cancellationToken), cancellationToken);
    protected virtual async Task<T> BeforeAdd(T model, CancellationToken cancellationToken = default) => await Task.FromResult(model);
    protected virtual async Task<T> Add(T model, CancellationToken cancellationToken = default)
    {
        model.CreatedAt = DateTime.UtcNow;
        await _set.AddAsync(model, cancellationToken);
        return model;
    }
    protected virtual async Task<T> AfterAdd(T model, CancellationToken cancellationToken = default) => await Task.FromResult(model);

    /*
     * Update
     */
    public async Task<T> UpdateAsync(T model, CancellationToken cancellationToken = default) => await AfterUpdate(await Update(await BeforeUpdate(model, cancellationToken), cancellationToken), cancellationToken);
    protected virtual async Task<T> BeforeUpdate(T model, CancellationToken cancellationToken = default) => await Task.FromResult(model);
    protected virtual async Task<T> Update(T model, CancellationToken cancellationToken = default)
    {
        if (!await _set.AnyAsync(x => x.ID.Equals(model.ID), cancellationToken)) NotFound();
        model.UpdatedAt = DateTime.UtcNow;
        _set.Update(model);
        return model;
    }
    protected virtual async Task<T> AfterUpdate(T model, CancellationToken cancellationToken = default) => await Task.FromResult(model);

    /*
     * Delete
     */
    public async Task<T> DeleteAsync(T model, CancellationToken cancellationToken = default) => await AfterDelete(await Delete(await BeforeDelete(model, cancellationToken), cancellationToken), cancellationToken);
    public async Task<T> DeleteAsync(K id, CancellationToken cancellationToken = default) => await DeleteAsync(NotFound(await _set.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync(cancellationToken)), cancellationToken);
    protected virtual async Task<T> BeforeDelete(T model, CancellationToken cancellationToken = default) => await Task.FromResult(model);
    protected virtual Task<T> Delete(T model, CancellationToken cancellationToken = default)
    {
        _set.Remove(model);
        return Task.FromResult(model);
    }
    protected virtual async Task<T> AfterDelete(T model, CancellationToken cancellationToken = default) => await Task.FromResult(model);

    /*
     * SoftDelete
     */
    public async Task<T> SoftDeleteAsync(T model, CancellationToken cancellationToken = default) => await AfterSoftDelete(await SoftDelete(await BeforeSoftDelete(model, cancellationToken), cancellationToken), cancellationToken);
    public async Task<T> SoftDeleteAsync(K id, CancellationToken cancellationToken = default) => await SoftDeleteAsync(NotFound(await _set.Where(x => x.ID.Equals(id)).FirstOrDefaultAsync(cancellationToken)), cancellationToken);
    protected virtual async Task<T> BeforeSoftDelete(T model, CancellationToken cancellationToken = default) => await Task.FromResult(model);
    protected virtual Task<T> SoftDelete(T model, CancellationToken cancellationToken = default)
    {
        model.DeletedAt = DateTime.UtcNow;
        _set.Update(model);
        return Task.FromResult(model);
    }
    protected virtual async Task<T> AfterSoftDelete(T model, CancellationToken cancellationToken = default) => await Task.FromResult(model);

    /*
     * ToQuery
     */
    public IQueryable<T> ToQuery() => _set.AsQueryable();
    public IQueryable<T> ApplyIncludes() => ApplyIncludes(ToQuery());
    public virtual IQueryable<T> ApplyIncludes(IQueryable<T> query) => query;

    /*
     * Find
     */
    public async Task<T> FindAsync(K id, CancellationToken cancellationToken = default) => NotFound(await ApplyIncludes().Where(x => x.ID.Equals(id)).FirstOrDefaultAsync(cancellationToken));
    public async Task<List<T>> FindAsync(IQueryable<T> query, CancellationToken cancellationToken = default) => await query.ToListAsync(cancellationToken);
    public async Task<List<T>> FindAsync(FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, ProjectionDefinition<T>? projection = null, int offset = 0, int limit = 0, CancellationToken cancellationToken = default)
    {
        var query = ToQuery();

        if (filter != null)
        {
            query = filter.ApplyFilter(query);
        }

        if (sort != null)
        {
            query = sort.ApplySort(query);
        }

        query = projection?.ApplyProjection(query) ?? ApplyIncludes(query);

        if (offset > 0)
        {
            query = query.Skip(offset);
        }

        if (limit > 0)
        {
            query = query.Take(limit);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /*
     * Count
     */
    public async Task<long> CountAsync(IQueryable<T> query, CancellationToken cancellationToken = default) => await query.CountAsync(cancellationToken);
    public async Task<long> CountAsync(CancellationToken cancellationToken = default) => await CountAsync(ToQuery(), cancellationToken);

    /*
     * Exception Handlers
     */
    protected void NotFound() => throw new NotFoundException();
    protected void NotFound(string message) => throw new NotFoundException(message);
    protected void NotFound(List<string> message) => throw new NotFoundException(message);

    protected Z NotFound<Z>(Z? model) where Z : class => model ?? throw new NotFoundException();
    protected Z NotFound<Z>(Z? model, string message) where Z : class => model ?? throw new NotFoundException(message);
    protected Z NotFound<Z>(Z? model, List<string> message) where Z : class => model ?? throw new NotFoundException(message);

    protected void Invalid() => throw new InvalidException();
    protected void Invalid(string message) => throw new InvalidException(message);
    protected void Invalid(List<string> message) => throw new InvalidException(message);

    protected void ModelInvalid() => throw new ModelInvalidException();
    protected void ModelInvalid(string message) => throw new ModelInvalidException(message);
    protected void ModelInvalid(List<string> message) => throw new ModelInvalidException(message);
}