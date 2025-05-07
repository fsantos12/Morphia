using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
// Assuming you are using Entity Framework Core for ToListAsync
// If not, you might need to adjust the materialization step.
// For example, by adding 'using Microsoft.EntityFrameworkCore;'
// or by making the ToListAsync call conditional or passed in.
// For this example, I'll assume EF Core's ToListAsync is available.
using Microsoft.EntityFrameworkCore;

namespace Morphia.Core.Repositories.Queries;

/// <summary>
/// Defines a set of properties to include in a database projection.
/// This class transforms an IQueryable<T> by selecting only specified properties
/// from the database and converting them into dynamic ExpandoObjects.
/// </summary>
/// <typeparam name="T">The type of the source object.</typeparam>
public class ProjectionDefinition<T>
{
    private readonly HashSet<string> _includedPaths = new HashSet<string>(StringComparer.Ordinal);

    public ProjectionDefinition() { }

    public ProjectionDefinition(IEnumerable<string> propertyPaths)
    {
        ArgumentNullException.ThrowIfNull(propertyPaths);
        foreach (var path in propertyPaths)
        {
            Include(path);
        }
    }

    public ProjectionDefinition<T> Include(string propertyPath)
    {
        if (!string.IsNullOrWhiteSpace(propertyPath))
        {
            _includedPaths.Add(propertyPath.Trim());
        }
        return this;
    }

    public ProjectionDefinition<T> Include<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var path = GetPropertyPathFromExpression(expression.Body);
        return Include(path);
    }

    /// <summary>
    /// Applies the defined projection to an IQueryable source, fetching data from the database
    /// and converting it into a list of dynamic ExpandoObjects.
    /// </summary>
    /// <param name="query">The source IQueryable.</param>
    /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of dynamic ExpandoObjects.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a property path is invalid for type T.</exception>
    public async Task<List<dynamic>> ApplyProjectionAsync(IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Step 1: Prepare the database projection query
        var (projectedQuery, orderedPaths) = PrepareProjectedQuery(query);

        // Step 2: Materialize the minimized data from the database
        // This assumes EF Core's ToListAsync. If not using EF Core, this part needs adjustment.
        List<object?[]> resultsAsArrays = await projectedQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

        // Step 3: Convert the object arrays to ExpandoObjects (in-memory)
        return [.. ProjectionDefinition<T>.ConvertToObjectArraysToExpandos(resultsAsArrays, orderedPaths)];
    }

    /// <summary>
    /// Prepares an IQueryable for database-side projection by constructing a
    /// SELECT statement to fetch only the specified properties into an object array.
    /// This method does not execute the query.
    /// </summary>
    /// <param name="query">The source IQueryable.</param>
    /// <returns>A tuple containing the modified IQueryable that projects to object[] and the ordered list of paths corresponding to array elements.</returns>
    private (IQueryable<object?[]> Query, List<string> OrderedPaths) PrepareProjectedQuery(IQueryable<T> query)
    {
        var orderedPaths = _includedPaths.OrderBy(p => p).ToList();

        if (!orderedPaths.Any())
        {
            return (query.Select(e => new object?[0]), new List<string>());
        }

        var parameter = Expression.Parameter(typeof(T), "e");
        var propertyAccessExpressions = new List<Expression>();

        foreach (var path in orderedPaths)
        {
            Expression currentMemberAccess;
            try
            {
                currentMemberAccess = GetNestedPropertyBody(parameter, path);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException($"Projection path '{path}' is invalid for type '{typeof(T).FullName}'.", ex);
            }
            propertyAccessExpressions.Add(Expression.Convert(currentMemberAccess, typeof(object)));
        }

        var newArrayExpression = Expression.NewArrayInit(typeof(object), propertyAccessExpressions);
        var selectorLambda = Expression.Lambda<Func<T, object?[]>>(newArrayExpression, parameter);

        return (query.Select(selectorLambda), orderedPaths);
    }

    /// <summary>
    /// Converts a collection of object arrays (resulting from a database projection) into dynamic ExpandoObjects.
    /// </summary>
    private static IEnumerable<dynamic> ConvertToObjectArraysToExpandos(IEnumerable<object?[]> projectedData, List<string> orderedPaths)
    {
        // This method remains largely the same as before.
        if (!orderedPaths.Any())
        {
            foreach (var _ in projectedData)
            {
                yield return new ExpandoObject();
            }
            yield break;
        }

        foreach (var objectArray in projectedData)
        {
            if (objectArray == null)
            {
                yield return new ExpandoObject();
                continue;
            }

            if (objectArray.Length != orderedPaths.Count)
            {
                throw new ArgumentException("The number of values in the object array does not match the number of ordered paths.", "projectedData");
            }

            var expando = new ExpandoObject() as IDictionary<string, object?>;
            for (int i = 0; i < orderedPaths.Count; i++)
            {
                SetNestedValue(expando, orderedPaths[i], objectArray[i]);
            }
            yield return expando;
        }
    }

    // --- Private Helper Methods ---

    private static string GetPropertyPathFromExpression(Expression expression)
    {
        var stack = new Stack<string>();
        MemberExpression? memberExpr = expression as MemberExpression;

        while (memberExpr != null)
        {
            stack.Push(memberExpr.Member.Name);
            if (memberExpr.Expression is ParameterExpression) break;
            memberExpr = memberExpr.Expression as MemberExpression;
        }

        if (!stack.Any())
        {
            if (expression is ParameterExpression) return string.Empty;
            throw new ArgumentException("Expression is not a valid property path for projection.", nameof(expression));
        }
        return string.Join(".", stack);
    }

    private static Expression GetNestedPropertyBody(Expression parameter, string propertyPath)
    {
        Expression currentExpression = parameter;
        var parts = propertyPath.Split('.');

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) throw new ArgumentException($"Invalid property path segment in '{propertyPath}'. Each part must be a valid property name.");

            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance; // Case-sensitive
            PropertyInfo? propertyInfo = currentExpression.Type.GetProperty(part, flags);

            if (propertyInfo == null)
            {
                throw new ArgumentException($"Property '{part}' not found on type '{currentExpression.Type.FullName}' in path '{propertyPath}'. Ensure the property exists and check casing.");
            }
            currentExpression = Expression.Property(currentExpression, propertyInfo);
        }
        return currentExpression;
    }

    private static void SetNestedValue(IDictionary<string, object?> target, string path, object? value)
    {
        var parts = path.Split('.');
        var currentTarget = target;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];
            if (!currentTarget.TryGetValue(part, out object? nextTargetObj) || !(nextTargetObj is IDictionary<string, object?>))
            {
                nextTargetObj = new ExpandoObject();
                currentTarget[part] = nextTargetObj;
            }
            currentTarget = (IDictionary<string, object?>)nextTargetObj;
        }
        currentTarget[parts.Last()] = value;
    }
}