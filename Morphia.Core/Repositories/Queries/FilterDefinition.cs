using System.Linq.Expressions;
using System.Reflection;

namespace Morphia.Core.Repositories.Queries;

public class FilterDefinition<T> : List<Expression<Func<T, bool>>>
{
    public FilterDefinition<T> Equal(string property, object? value)
    {
        var propertyExpression = FilterDefinition<T>.GetPropertyExpression(property);
        return propertyExpression == null ? this : Equal(propertyExpression, value);
    }

    public FilterDefinition<T> Equal(Expression<Func<T, object>> property, object? value)
    {
        return Add(property, value, Expression.Equal);
    }


    public FilterDefinition<T> NotEqual(string property, object? value)
    {
        var propertyExpression = FilterDefinition<T>.GetPropertyExpression(property);
        return propertyExpression == null ? this : NotEqual(propertyExpression, value);
    }

    public FilterDefinition<T> NotEqual(Expression<Func<T, object>> property, object? value)
    {
        return Add(property, value, Expression.NotEqual);
    }


    public FilterDefinition<T> GreaterThan(string property, object? value)
    {
        var propertyExpression = FilterDefinition<T>.GetPropertyExpression(property);
        return propertyExpression == null ? this : GreaterThan(propertyExpression, value);
    }

    public FilterDefinition<T> GreaterThan(Expression<Func<T, object>> property, object? value)
    {
        return Add(property, value, Expression.GreaterThan);
    }


    public FilterDefinition<T> GreaterThanOrEqual(string property, object? value)
    {
        var propertyExpression = FilterDefinition<T>.GetPropertyExpression(property);
        return propertyExpression == null ? this : GreaterThanOrEqual(propertyExpression, value);
    }

    public FilterDefinition<T> GreaterThanOrEqual(Expression<Func<T, object>> property, object? value)
    {
        return Add(property, value, Expression.GreaterThanOrEqual);
    }


    public FilterDefinition<T> LessThan(string property, object? value)
    {
        var propertyExpression = FilterDefinition<T>.GetPropertyExpression(property);
        return propertyExpression == null ? this : LessThan(propertyExpression, value);
    }

    public FilterDefinition<T> LessThan(Expression<Func<T, object>> property, object? value)
    {
        return Add(property, value, Expression.LessThan);
    }


    public FilterDefinition<T> LessThanOrEqual(string property, object? value)
    {
        var propertyExpression = FilterDefinition<T>.GetPropertyExpression(property);
        return propertyExpression == null ? this : LessThanOrEqual(propertyExpression, value);
    }

    public FilterDefinition<T> LessThanOrEqual(Expression<Func<T, object>> property, object? value)
    {
        return Add(property, value, Expression.LessThanOrEqual);
    }


    private FilterDefinition<T> Add(Expression<Func<T, object>> property, object? value, Func<Expression, Expression, BinaryExpression> comparisonFunc)
    {
        if (value != null)
        {
            var parameter = property.Parameters[0];
            var body = property.Body is UnaryExpression unary ? unary.Operand : property.Body;

            var constant = Expression.Constant(value, body.Type);
            var comparison = comparisonFunc(body, constant);

            Add(Expression.Lambda<Func<T, bool>>(comparison, parameter));
        }
        return this;
    }


    public FilterDefinition<T> Contains(string property, string? value)
    {
        var propertyExpression = FilterDefinition<T>.GetPropertyExpression(property);
        return propertyExpression == null ? this : Contains(propertyExpression, value);
    }

    public FilterDefinition<T> Contains(Expression<Func<T, object>> property, string? value)
    {
        return AddStringFilter(property, value, "Contains");
    }


    public FilterDefinition<T> StartsWith(string property, string? value)
    {
        var propertyExpression = FilterDefinition<T>.GetPropertyExpression(property);
        return propertyExpression == null ? this : StartsWith(propertyExpression, value);
    }

    public FilterDefinition<T> StartsWith(Expression<Func<T, object>> property, string? value)
    {
        return AddStringFilter(property, value, "StartsWith");
    }


    public FilterDefinition<T> EndsWith(string property, string? value)
    {
        var propertyExpression = FilterDefinition<T>.GetPropertyExpression(property);
        return propertyExpression == null ? this : EndsWith(propertyExpression, value);
    }

    public FilterDefinition<T> EndsWith(Expression<Func<T, object>> property, string? value)
    {
        return AddStringFilter(property, value, "EndsWith");
    }


    // Helper method to add string-based filters (Contains, StartsWith, EndsWith)
    private FilterDefinition<T> AddStringFilter(Expression<Func<T, object>> property, string? value, string methodName)
    {
        if (string.IsNullOrEmpty(value))
        {
            return this; // Avoid adding a filter for empty or null values
        }

        var parameter = property.Parameters[0];
        var body = property.Body is UnaryExpression unary ? unary.Operand : property.Body;
        var constant = Expression.Constant(value, typeof(string));

        // Get the appropriate method (Contains, StartsWith, or EndsWith)
        var method = typeof(string).GetMethod(methodName, new[] { typeof(string) });
        if (method == null)
        {
            return this;
        }

        var methodCall = Expression.Call(body, method, constant);
        Add(Expression.Lambda<Func<T, bool>>(methodCall, parameter));

        return this;
    }


    public FilterDefinition<T> Between(string property, object startValue, object endValue)
    {
        var propertyExpression = FilterDefinition<T>.GetPropertyExpression(property);
        return propertyExpression == null ? this : Between(propertyExpression, startValue, endValue);
    }

    public FilterDefinition<T> Between(Expression<Func<T, object>> property, object startValue, object endValue)
    {
        var parameter = property.Parameters[0];
        var body = property.Body is UnaryExpression unary ? unary.Operand : property.Body;

        var start = Expression.Constant(startValue, body.Type);
        var end = Expression.Constant(endValue, body.Type);

        var greaterThanOrEqual = Expression.GreaterThanOrEqual(body, start);
        var lessThanOrEqual = Expression.LessThanOrEqual(body, end);

        var combined = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
        Add(Expression.Lambda<Func<T, bool>>(combined, parameter));

        return this;
    }


    public FilterDefinition<T> In(string property, IEnumerable<object> values)
    {
        var propertyExpression = GetPropertyExpression(property);
        return propertyExpression == null ? this : In(propertyExpression, values);
    }

    public FilterDefinition<T> In(Expression<Func<T, object>> property, IEnumerable<object> values)
    {
        if (values != null && values.Any())
        {
            var parameter = property.Parameters[0];
            var body = property.Body is UnaryExpression unary ? unary.Operand : property.Body;

            var constants = values.Select(v => Expression.Constant(v, body.Type));
            var containsMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(body.Type);
            var containsCall = Expression.Call(containsMethod, Expression.Constant(values.ToList()), body);

            Add(Expression.Lambda<Func<T, bool>>(containsCall, parameter));
        }
        return this;
    }


    public FilterDefinition<T> IsNull(string property)
    {
        var propertyExpression = GetPropertyExpression(property);
        return propertyExpression == null ? this : IsNull(propertyExpression);
    } 

    public FilterDefinition<T> IsNull(Expression<Func<T, object>> property)
    {
        var parameter = property.Parameters[0];
        var body = property.Body is UnaryExpression unary ? unary.Operand : property.Body;

        var nullCheck = Expression.Equal(body, Expression.Constant(null, body.Type));
        Add(Expression.Lambda<Func<T, bool>>(nullCheck, parameter));
        return this;
    }


    public IQueryable<T> ApplyFilter(IQueryable<T> query)
    {
        foreach (var filter in this)
        {
            query = query.Where(filter);
        }
        return query;
    }

    private static Expression<Func<T, object>>? GetPropertyExpression(string propertyPath)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        try
        {
            var body = GetNestedProperty(parameter, propertyPath);
            var converted = Expression.Convert(body, typeof(object));
            return Expression.Lambda<Func<T, object>>(converted, parameter);
        }
        catch
        {
            return null;
        }
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