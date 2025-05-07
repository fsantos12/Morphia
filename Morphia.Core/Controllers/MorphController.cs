using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Morphia.Core.Models;
using Morphia.Core.Repositories;
using Morphia.Core.Repositories.Exceptions;
using Morphia.Core.Repositories.Queries;

namespace Morphia.Core.Controllers;

public class MorphController<T, K> : MorphController<T, K, T> where T : MorphModel<K> where K : IEquatable<K>
{
    public MorphController(ILogger<MorphController<T, K, T>> logger, MorphRepository<T, K> repository) : base(logger, repository)
    {
    }

    public override T ConvertToDto(T model)
    {
        return model;
    }

    public override T ConvertToModel(T dto)
    {
        return dto;
    }
}

public abstract class MorphController<T, K, Z> : ControllerBase where T : MorphModel<K> where K : IEquatable<K>
{
    public abstract T ConvertToModel(Z dto);
    public abstract Z ConvertToDto(T model);

    protected readonly ILogger<MorphController<T, K, Z>> _logger;
    protected readonly MorphRepository<T, K> _repository;

    public MorphController(ILogger<MorphController<T, K, Z>> logger, MorphRepository<T, K> repository) : base()
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpPost]
    public virtual async Task<IActionResult> Add(Z dto, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _repository.AddAsync(ConvertToModel(dto), cancellationToken);
            await _repository.SaveChangesAsync();
            return Ok(ConvertToDto(model));
        }
        catch (NotFoundException e)
        {
            return Problem(e.Message, statusCode: 404);
        }
        catch (InvalidOperationException e)
        {
            return Problem(e.Message, statusCode: 400);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Internal Server Error");
            return StatusCode(500);
        }
    }

    [HttpPut]
    public virtual async Task<IActionResult> Update(Z dto, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _repository.UpdateAsync(ConvertToModel(dto), cancellationToken);
            await _repository.SaveChangesAsync();
            return Ok(ConvertToDto(model));
        }
        catch (NotFoundException e)
        {
            return Problem(e.Message, statusCode: 404);
        }
        catch (InvalidOperationException e)
        {
            return Problem(e.Message, statusCode: 400);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Internal Server Error");
            return StatusCode(500);
        }
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(K id, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _repository.DeleteAsync(id, cancellationToken);
            await _repository.SaveChangesAsync();
            return Ok(ConvertToDto(model));
        }
        catch (NotFoundException e)
        {
            return Problem(e.Message, statusCode: 404);
        }
        catch (InvalidOperationException e)
        {
            return Problem(e.Message, statusCode: 400);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Internal Server Error");
            return StatusCode(500);
        }
    }

    [HttpGet("{id}")]
    public virtual async Task<IActionResult> Find(K id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(ConvertToDto(await _repository.FindAsync(id, cancellationToken)));
        }
        catch (NotFoundException e)
        {
            return Problem(e.Message, statusCode: 404);
        }
        catch (InvalidOperationException e)
        {
            return Problem(e.Message, statusCode: 400);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Internal Server Error");
            return StatusCode(500);
        }
    }

    [HttpGet()]
    public virtual async Task<IActionResult> Find(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _repository.FindAsync(GetFilterDefinition(HttpContext.Request.Query), GetSortDefinition(HttpContext.Request.Query), GetProjectionDefinition(HttpContext.Request.Query), GetOffsetDefinition(HttpContext.Request.Query), GetLimitDefinition(HttpContext.Request.Query), cancellationToken));
        }
        catch (NotFoundException e)
        {
            return Problem(e.Message, statusCode: 404);
        }
        catch (InvalidOperationException e)
        {
            return Problem(e.Message, statusCode: 400);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Internal Server Error");
            return StatusCode(500);
        }
    }

    protected virtual FilterDefinition<T>? GetFilterDefinition(IQueryCollection query)
    {
        return null;
    }

    protected virtual SortDefinition<T>? GetSortDefinition(IQueryCollection query)
    {
        return null;
    }

    protected virtual ProjectionDefinition<T>? GetProjectionDefinition(IQueryCollection query)
    {
        return null;
    }
    protected virtual int GetOffsetDefinition(IQueryCollection query)
    {
        return 0;
    }

    protected virtual int GetLimitDefinition(IQueryCollection query)
    {
        return 0;
    }
}
