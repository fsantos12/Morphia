using System.Linq.Expressions;
using System.Reflection;

namespace Morphia.Core.Repositories.Queries;

public class FilterDefinition<T>
{
    private readonly List<Expression<Func<T, bool>>> _predicates = [];

    public FilterDefinition() { }

    public FilterDefinition(IEnumerable<Expression<Func<T, bool>>> initialPredicates)
    {
        _predicates.AddRange(initialPredicates);
    }

    // Public method to add a pre-built filter expression if desired by consumers
    public FilterDefinition<T> AddCondition(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _predicates.Add(predicate);
        return this;
    }

    public FilterDefinition<T> Equal(string propertyPath, object? value)
    {
        var propertyExpression = GetPropertyExpressionLambda(propertyPath); // Renamed for clarity
        return Equal(propertyExpression, value);
    }

    public FilterDefinition<T> Equal(Expression<Func<T, object>> propertyExpression, object? value)
    {
        return AddComparison(propertyExpression, value, Expression.Equal);
    }

    public FilterDefinition<T> NotEqual(string propertyPath, object? value)
    {
        var propertyExpression = GetPropertyExpressionLambda(propertyPath);
        return NotEqual(propertyExpression, value);
    }

    public FilterDefinition<T> NotEqual(Expression<Func<T, object>> propertyExpression, object? value)
    {
        return AddComparison(propertyExpression, value, Expression.NotEqual);
    }

    public FilterDefinition<T> GreaterThan(string propertyPath, object? value)
    {
        var propertyExpression = GetPropertyExpressionLambda(propertyPath);
        return GreaterThan(propertyExpression, value);
    }

    public FilterDefinition<T> GreaterThan(Expression<Func<T, object>> propertyExpression, object? value)
    {
        return AddComparison(propertyExpression, value, Expression.GreaterThan);
    }

    public FilterDefinition<T> GreaterThanOrEqual(string propertyPath, object? value)
    {
        var propertyExpression = GetPropertyExpressionLambda(propertyPath);
        return GreaterThanOrEqual(propertyExpression, value);
    }

    public FilterDefinition<T> GreaterThanOrEqual(Expression<Func<T, object>> propertyExpression, object? value)
    {
        return AddComparison(propertyExpression, value, Expression.GreaterThanOrEqual);
    }

    public FilterDefinition<T> LessThan(string propertyPath, object? value)
    {
        var propertyExpression = GetPropertyExpressionLambda(propertyPath);
        return LessThan(propertyExpression, value);
    }

    public FilterDefinition<T> LessThan(Expression<Func<T, object>> propertyExpression, object? value)
    {
        return AddComparison(propertyExpression, value, Expression.LessThan);
    }

    public FilterDefinition<T> LessThanOrEqual(string propertyPath, object? value)
    {
        var propertyExpression = GetPropertyExpressionLambda(propertyPath);
        return LessThanOrEqual(propertyExpression, value);
    }

    public FilterDefinition<T> LessThanOrEqual(Expression<Func<T, object>> propertyExpression, object? value)
    {
        return AddComparison(propertyExpression, value, Expression.LessThanOrEqual);
    }

    private FilterDefinition<T> AddComparison(Expression<Func<T, object>> propertyExpression, object? value, Func<Expression, Expression, BinaryExpression> comparisonFunc)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);

        // If value is null, this specific comparison might not make sense
        // unless it's Expression.Equal or Expression.NotEqual.
        // For other comparisons (GreaterThan, etc.), comparing with null is usually an error or undefined.
        // The original code added the filter only if value != null.
        // For Equals/NotEquals null, IsNull/IsNotNull are more explicit.
        if (value == null && !(comparisonFunc == Expression.Equal || comparisonFunc == Expression.NotEqual))
        {
            // Or throw an ArgumentNullException for 'value' if comparison with null is not allowed for this type of comparison.
            // For simplicity, we'll skip adding the filter like the original, but this behavior should be well-defined.
             return this;
        }

        var parameter = propertyExpression.Parameters[0];
        var body = propertyExpression.Body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert
            ? unary.Operand
            : propertyExpression.Body;

        // Ensure constant is of the same type as the property body, or handle nulls appropriately
        Expression constant;
        if (value == null)
        {
            constant = Expression.Constant(null, body.Type.IsValueType && Nullable.GetUnderlyingType(body.Type) == null ? typeof(object) : body.Type);
        }
        else
        {
            try
            {
                constant = Expression.Constant(Convert.ChangeType(value, body.Type), body.Type);
            }
            catch (InvalidCastException ex)
            {
                throw new ArgumentException($"Value '{value}' cannot be converted to the property type '{body.Type.Name}'. Property: {body}", nameof(value), ex);
            }
        }

        var comparison = comparisonFunc(body, constant);
        _predicates.Add(Expression.Lambda<Func<T, bool>>(comparison, parameter));
        return this;
    }

    public FilterDefinition<T> Contains(string propertyPath, string? value)
    {
        var propertyExpression = GetPropertyExpressionLambda(propertyPath);
        return Contains(propertyExpression, value);
    }

    public FilterDefinition<T> Contains(Expression<Func<T, object>> propertyExpression, string? value)
    {
        return AddStringFilter(propertyExpression, value, "Contains");
    }

    public FilterDefinition<T> StartsWith(string propertyPath, string? value)
    {
        var propertyExpression = GetPropertyExpressionLambda(propertyPath);
        return StartsWith(propertyExpression, value);
    }

    public FilterDefinition<T> StartsWith(Expression<Func<T, object>> propertyExpression, string? value)
    {
        return AddStringFilter(propertyExpression, value, "StartsWith");
    }

    public FilterDefinition<T> EndsWith(string propertyPath, string? value)
    {
        var propertyExpression = GetPropertyExpressionLambda(propertyPath);
        return EndsWith(propertyExpression, value);
    }

    public FilterDefinition<T> EndsWith(Expression<Func<T, object>> propertyExpression, string? value)
    {
        return AddStringFilter(propertyExpression, value, "EndsWith");
    }

    private FilterDefinition<T> AddStringFilter(Expression<Func<T, object>> propertyExpression, string? value, string methodName)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);
        if (string.IsNullOrEmpty(value))
        {
            return this; // Do not add filter for empty or null search strings
        }

        var parameter = propertyExpression.Parameters[0];
        var body = propertyExpression.Body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert
            ? unary.Operand
            : propertyExpression.Body;

        if (body.Type != typeof(string))
        {
            throw new ArgumentException($"Property '{body}' must be of type string to use method '{methodName}'. Actual type: {body.Type.Name}", nameof(propertyExpression));
        }

        var constant = Expression.Constant(value, typeof(string));
        var methodInfo = typeof(string).GetMethod(methodName, new[] { typeof(string) });

        if (methodInfo == null)
        {
            // This should ideally not happen for "Contains", "StartsWith", "EndsWith"
            throw new InvalidOperationException($"Could not find string method '{methodName}'.");
        }

        var methodCall = Expression.Call(body, methodInfo, constant);
        _predicates.Add(Expression.Lambda<Func<T, bool>>(methodCall, parameter));
        return this;
    }

    public FilterDefinition<T> Between(string propertyPath, object startValue, object endValue)
    {
        var propertyExpression = GetPropertyExpressionLambda(propertyPath);
        return Between(propertyExpression, startValue, endValue);
    }

    public FilterDefinition<T> Between(Expression<Func<T, object>> propertyExpression, object startValue, object endValue)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);
        ArgumentNullException.ThrowIfNull(startValue);
        ArgumentNullException.ThrowIfNull(endValue);

        var parameter = propertyExpression.Parameters[0];
        var body = propertyExpression.Body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert
            ? unary.Operand
            : propertyExpression.Body;

        Expression startConstant, endConstant;
        try
        {
            startConstant = Expression.Constant(Convert.ChangeType(startValue, body.Type), body.Type);
            endConstant = Expression.Constant(Convert.ChangeType(endValue, body.Type), body.Type);
        }
        catch (InvalidCastException ex)
        {
             throw new ArgumentException($"Start or end value cannot be converted to the property type '{body.Type.Name}'. Property: {body}", ex);
        }


        var greaterThanOrEqual = Expression.GreaterThanOrEqual(body, startConstant);
        var lessThanOrEqual = Expression.LessThanOrEqual(body, endConstant);

        var combined = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
        _predicates.Add(Expression.Lambda<Func<T, bool>>(combined, parameter));
        return this;
    }

    public FilterDefinition<T> In(string propertyPath, IEnumerable<object> values)
    {
        var propertyExpression = GetPropertyExpressionLambda(propertyPath);
        return In(propertyExpression, values);
    }

    public FilterDefinition<T> In(Expression<Func<T, object>> propertyExpression, IEnumerable<object> values)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);
        ArgumentNullException.ThrowIfNull(values);

        var valueList = values.ToList(); // Evaluate once
        if (!valueList.Any())
        {
            // No values to check against, some LINQ providers might error on empty IN.
            // You could choose to add a 'false' predicate or skip. For now, skip.
            return this;
        }

        var parameter = propertyExpression.Parameters[0];
        var body = propertyExpression.Body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert
            ? unary.Operand
            : propertyExpression.Body;

        // Ensure all values can be converted to the property type.
        List<object> convertedValues;
        try
        {
            convertedValues = valueList.Select(v => Convert.ChangeType(v, body.Type)).ToList();
        }
        catch(InvalidCastException ex)
        {
            throw new ArgumentException($"One or more values in the 'In' collection cannot be converted to property type '{body.Type.Name}'. Property: {body}", nameof(values), ex);
        }

        var containsMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(body.Type);

        // Create a constant expression from the list of appropriately typed values
        var listValuesConstant = Expression.Constant(convertedValues, typeof(List<>).MakeGenericType(body.Type));
        var containsCall = Expression.Call(null, containsMethod, listValuesConstant, body); // list.Contains(item.Property) -> item.Property is the second arg

        _predicates.Add(Expression.Lambda<Func<T, bool>>(containsCall, parameter));
        return this;
    }

    public FilterDefinition<T> IsNull(string propertyPath)
    {
        var propertyExpression = GetPropertyExpressionLambda(propertyPath);
        return IsNull(propertyExpression);
    }

    public FilterDefinition<T> IsNull(Expression<Func<T, object>> propertyExpression)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);

        var parameter = propertyExpression.Parameters[0];
        // For IsNull, we might not need to unbox if it's a reference type,
        // but it's safer to keep it consistent if propertyExpression is Func<T, object>
        var body = propertyExpression.Body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert 
            ? unary.Operand 
            : propertyExpression.Body;

        // Expression.Constant(null, body.Type) ensures that if body.Type is Nullable<V>, it gets a null Nullable.
        // If body.Type is a reference type, it gets a null reference.
        var nullConstant = Expression.Constant(null, body.Type);
        var nullCheck = Expression.Equal(body, nullConstant);
        _predicates.Add(Expression.Lambda<Func<T, bool>>(nullCheck, parameter));
        return this;
    }

     public FilterDefinition<T> IsNotNull(string propertyPath)
    {
        var propertyExpression = GetPropertyExpressionLambda(propertyPath);
        return IsNotNull(propertyExpression);
    }

    public FilterDefinition<T> IsNotNull(Expression<Func<T, object>> propertyExpression)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);

        var parameter = propertyExpression.Parameters[0];
        var body = propertyExpression.Body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert 
            ? unary.Operand 
            : propertyExpression.Body;
        
        var nullConstant = Expression.Constant(null, body.Type);
        var notNullCheck = Expression.NotEqual(body, nullConstant);
        _predicates.Add(Expression.Lambda<Func<T, bool>>(notNullCheck, parameter));
        return this;
    }


    public IQueryable<T> ApplyFilter(IQueryable<T> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        foreach (var predicate in _predicates)
        {
            query = query.Where(predicate);
        }
        return query;
    }

    private static Expression<Func<T, object>> GetPropertyExpressionLambda(string propertyPath)
    {
        if (string.IsNullOrWhiteSpace(propertyPath))
        {
            throw new ArgumentException("Property path cannot be null or whitespace.", nameof(propertyPath));
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var body = GetNestedPropertyBody(parameter, propertyPath); // Renamed for clarity
        var convertedBody = Expression.Convert(body, typeof(object)); // Convert to object for Func<T, object>
        return Expression.Lambda<Func<T, object>>(convertedBody, parameter);
    }

    // Consider making this a shared utility if SortDefinition also uses an identical version.
    private static Expression GetNestedPropertyBody(Expression parameter, string propertyPath)
    {
        Expression currentExpression = parameter;
        var parts = propertyPath.Split('.');

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) throw new ArgumentException($"Invalid property path segment in '{propertyPath}'.");

            // Default to case-sensitive. Change BindingFlags if case-insensitivity is desired.
            // BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            PropertyInfo? propertyInfo = currentExpression.Type.GetProperty(part, flags);

            if (propertyInfo == null)
            {
                throw new ArgumentException($"Property '{part}' not found on type '{currentExpression.Type.FullName}' in path '{propertyPath}'.");
            }
            currentExpression = Expression.Property(currentExpression, propertyInfo);
        }
        return currentExpression;
    }
}