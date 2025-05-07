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
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _set = _context.Set<T>();
    }

    /*
     * Save Changes
     */
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    /*
     * Add
     */
    public async Task<T> AddAsync(T model, CancellationToken cancellationToken = default)
    {
        var modelToProcess = await BeforeAddAsync(model, cancellationToken).ConfigureAwait(false);
        var addedModel = await PerformAddAsync(modelToProcess, cancellationToken).ConfigureAwait(false);
        return await AfterAddAsync(addedModel, cancellationToken).ConfigureAwait(false);
    }

    protected virtual Task<T> BeforeAddAsync(T model, CancellationToken cancellationToken = default) => Task.FromResult(model);
    protected virtual async Task<T> PerformAddAsync(T model, CancellationToken cancellationToken = default)
    {
        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = null;
        model.DeletedAt = null;
        await _set.AddAsync(model, cancellationToken).ConfigureAwait(false);
        return model;
    }
    protected virtual Task<T> AfterAddAsync(T model, CancellationToken cancellationToken = default) => Task.FromResult(model);

    /*
     * Update
     */
    public async Task<T> UpdateAsync(T model, CancellationToken cancellationToken = default)
    {
        var modelToProcess = await BeforeUpdateAsync(model, cancellationToken).ConfigureAwait(false);
        var updatedModel = await PerformUpdateAsync(modelToProcess, cancellationToken).ConfigureAwait(false);
        return await AfterUpdateAsync(updatedModel, cancellationToken).ConfigureAwait(false);
    }

    protected virtual Task<T> BeforeUpdateAsync(T model, CancellationToken cancellationToken) => Task.FromResult(model);
    protected virtual async Task<T> PerformUpdateAsync(T model, CancellationToken cancellationToken = default)
    {
        // Ensure the entity is being tracked or exists.
        // If it's detached, AnyAsync might not be enough; consider Attach if not tracked.
        var entry = _context.Entry(model);
        if (entry.State == EntityState.Detached)
        {
                // Check if an entity with this ID already exists in the database
            bool exists = await _set.AnyAsync(x => x.ID.Equals(model.ID) && x.DeletedAt == null, cancellationToken).ConfigureAwait(false);
            if (!exists) NotFound($"Entity with ID '{model.ID}' not found for update.");
            _set.Update(model); // This will mark all properties as modified.
        }
        // If already tracked, EF Core handles changes.

        model.UpdatedAt = DateTime.UtcNow;
        return model;
    }
    protected virtual Task<T> AfterUpdateAsync(T model, CancellationToken cancellationToken) => Task.FromResult(model);

    /*
     * Delete
     */
    public async Task<T> DeleteAsync(T model, CancellationToken cancellationToken = default)
    {
        var modelToProcess = await BeforeDeleteAsync(model, cancellationToken).ConfigureAwait(false);
        var deletedModel = await PerformDeleteAsync(modelToProcess, cancellationToken).ConfigureAwait(false);
        return await AfterDeleteAsync(deletedModel, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> DeleteAsync(K id, CancellationToken cancellationToken = default)
    {
         var model = NotFound(await _set.FirstOrDefaultAsync(x => x.ID.Equals(id), cancellationToken).ConfigureAwait(false));
         return await DeleteAsync(model!, cancellationToken).ConfigureAwait(false);
    }

    protected virtual Task<T> BeforeDeleteAsync(T model, CancellationToken cancellationToken = default) => Task.FromResult(model);
    protected virtual Task<T> PerformDeleteAsync(T model, CancellationToken cancellationToken = default)
    {
        _set.Remove(model);
        return Task.FromResult(model);
    }
    protected virtual Task<T> AfterDeleteAsync(T model, CancellationToken cancellationToken = default) => Task.FromResult(model);

    /*
     * SoftDelete
     */
    public async Task<T> SoftDeleteAsync(T model, CancellationToken cancellationToken = default)
    {
        var modelToProcess = await BeforeSoftDeleteAsync(model, cancellationToken).ConfigureAwait(false);
        var deletedModel = await PerformSoftDeleteAsync(modelToProcess, cancellationToken).ConfigureAwait(false);
        return await AfterSoftDeleteAsync(deletedModel, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> SoftDeleteAsync(K id, CancellationToken cancellationToken = default)
    {
         var model = NotFound(await _set.FirstOrDefaultAsync(x => x.ID.Equals(id), cancellationToken).ConfigureAwait(false));
         return await SoftDeleteAsync(model!, cancellationToken).ConfigureAwait(false);
    }

    protected virtual Task<T> BeforeSoftDeleteAsync(T model, CancellationToken cancellationToken = default) => Task.FromResult(model);
    protected virtual Task<T> PerformSoftDeleteAsync(T model, CancellationToken cancellationToken = default)
    {
        model.DeletedAt = DateTime.UtcNow;
        _set.Update(model);
        return Task.FromResult(model);
    }
    protected virtual Task<T> AfterSoftDeleteAsync(T model, CancellationToken cancellationToken = default) => Task.FromResult(model);

    /*
     * ToQuery
     */
    public virtual IQueryable<T> ToQuery()
    {
        return _set.AsQueryable();
    }

    public virtual IQueryable<T> ApplyIncludes(IQueryable<T> query)
    {
        return query;
    }

    /*
     * Find
     */
    public async Task<T> FindAsync(K id, CancellationToken cancellationToken = default)
    {
        var query = ApplyIncludes(ToQuery());
        return NotFound(await query.FirstOrDefaultAsync(x => x.ID.Equals(id), cancellationToken).ConfigureAwait(false));
    }

    public async Task<List<dynamic>> FindAsync(FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null, ProjectionDefinition<T>? projection = null, int offset = 0, int limit = 0, CancellationToken cancellationToken = default)
    {
        var query = ToQuery();

        if (filter != null) query = filter.ApplyFilter(query);
        if (sort != null) query = sort.ApplySort(query);

        if (offset > 0) query = query.Skip(offset);
        if (limit > 0) query = query.Take(limit);

        if (projection != null)
        {
            // A projection is provided, apply it.
            // ApplyProjectionAsync handles database-side select and conversion to dynamic.
            return await projection.ApplyProjectionAsync(query, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // No projection provided, return full entities.
            // ApplyIncludes should be called here to ensure navigation properties are loaded if needed.
            query = ApplyIncludes(query);
            List<T> fullEntities = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
            // Cast List<T> to List<dynamic>. Each T instance will be treated as dynamic.
            return [.. fullEntities.Cast<dynamic>()];
        }
    }

    /*
     * Count
     */
    public async Task<long> CountAsync(FilterDefinition<T>? filter = null, CancellationToken cancellationToken = default)
    {
        var query = filter?.ApplyFilter(ToQuery()) ?? ToQuery();
        return await query.LongCountAsync(cancellationToken).ConfigureAwait(false);
    }

    /*
     * Any
     */
    public async Task<bool> AnyAsync(FilterDefinition<T>? filter = null, CancellationToken cancellationToken = default)
    {
        var query = filter?.ApplyFilter(ToQuery()) ?? ToQuery();
        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> AnyAsync(K id, CancellationToken cancellationToken = default)
    {
        return await _set.AnyAsync(x => x.ID.Equals(id), cancellationToken).ConfigureAwait(false);
    }

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