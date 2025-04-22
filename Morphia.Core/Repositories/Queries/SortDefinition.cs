using System.Linq.Expressions;
using System.Reflection;

namespace Morphia.Core.Repositories.Queries;

public class SortDefinition<T> : List<(LambdaExpression keySelector, bool ascending)>
{
    public SortDefinition<T> Ascending(string property)
    {
        var expression = GetPropertyExpression(property);
        return expression == null ? this : Ascending(expression);
    }

    public SortDefinition<T> Ascending(Expression<Func<T, object>> property)
    {
        Add((property, true));
        return this;
    }

    public SortDefinition<T> Descending(string property)
    {
        var expression = GetPropertyExpression(property);
        return expression == null ? this : Descending(expression);
    }

    public SortDefinition<T> Descending(Expression<Func<T, object>> property)
    {
        Add((property, false));
        return this;
    }

    public IQueryable<T> ApplySort(IQueryable<T> query)
    {
        for (int i = 0; i < Count; i++)
        {
            var (keySelector, ascending) = this[i];
            if (keySelector is not Expression<Func<T, object>> lambda) continue;

            var body = lambda.Body is UnaryExpression unary ? unary.Operand : lambda.Body;
            var keySelectorTyped = Expression.Lambda(body, lambda.Parameters);
            var methodName = i == 0 ? (ascending ? "OrderBy" : "OrderByDescending") : (ascending ? "ThenBy" : "ThenByDescending");
            var method = typeof(Queryable).GetMethods().First(m => m.Name == methodName && m.GetParameters().Length == 2).MakeGenericMethod(typeof(T), body.Type);

            query = (IQueryable<T>)method.Invoke(null, new object[] { query, keySelectorTyped })!;
        }
        return query;
    }

    private static Expression<Func<T, object>>? GetPropertyExpression(string propertyPath)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var body = GetNestedProperty(parameter, propertyPath);
        var converted = Expression.Convert(body, typeof(object));
        return Expression.Lambda<Func<T, object>>(converted, parameter);
    }

    private static Expression GetNestedProperty(Expression parameter, string propertyPath)
    {
        var parts = propertyPath.Split('.');
        Expression current = parameter;

        foreach (var part in parts)
        {
            var property = current.Type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null)
                throw new InvalidOperationException($"Property '{part}' not found on type '{current.Type.Name}'");
            current = Expression.Property(current, property);
        }

        return current;
    }
}