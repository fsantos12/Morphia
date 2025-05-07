using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Generic; // Required for List
using System.Linq; // Required for Queryable

namespace Morphia.Core.Repositories.Queries;

public class SortDefinition<T>
{
    private readonly List<(LambdaExpression keySelector, bool ascending)> _sortCriteria = new();

    // Optional: Constructor for initialization
    public SortDefinition() { }

    public SortDefinition(IEnumerable<(LambdaExpression keySelector, bool ascending)> initialCriteria)
    {
         _sortCriteria.AddRange(initialCriteria);
    }

    public SortDefinition<T> Ascending(string propertyPath)
    {
        var expression = GetPropertyExpressionLambda(propertyPath);
        return Ascending(expression);
    }

    public SortDefinition<T> Ascending(Expression<Func<T, object>> propertyExpression)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);
        _sortCriteria.Add((propertyExpression, true));
        return this;
    }

    public SortDefinition<T> Descending(string propertyPath)
    {
        var expression = GetPropertyExpressionLambda(propertyPath);
        return Descending(expression);
    }

    public SortDefinition<T> Descending(Expression<Func<T, object>> propertyExpression)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);
        _sortCriteria.Add((propertyExpression, false));
        return this;
    }

    public IQueryable<T> ApplySort(IQueryable<T> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        for (int i = 0; i < _sortCriteria.Count; i++)
        {
            var (keySelector, ascending) = _sortCriteria[i];

            // The keySelector stored is already Expression<Func<T, object>>
            // or it could be any LambdaExpression if added differently.
            // We need its body and parameters to build the correctly typed lambda for OrderBy/ThenBy.
            
            Expression body = keySelector.Body;
            if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            {
                body = unary.Operand; // Unbox if property was converted to object
            }

            var typedKeySelector = Expression.Lambda(body, keySelector.Parameters);
            var methodName = i == 0
                ? (ascending ? "OrderBy" : "OrderByDescending")
                : (ascending ? "ThenBy" : "ThenByDescending");

            // Find the appropriate OrderBy/ThenBy method
            // TSource is typeof(T), TKey is body.Type
            var methodInfo = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == methodName &&
                                     m.IsGenericMethodDefinition &&
                                     m.GetParameters().Length == 2 &&
                                     m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IQueryable<>) &&
                                     m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));
            
            if (methodInfo == null)
            {
                 throw new InvalidOperationException($"Could not find sorting method '{methodName}'. This is unexpected.");
            }

            var genericMethod = methodInfo.MakeGenericMethod(typeof(T), body.Type);
            query = (IQueryable<T>)genericMethod.Invoke(null, new object[] { query, typedKeySelector })!;
        }
        return query;
    }

    // This helper is identical to the one in FilterDefinition.
    // Consider moving to a shared internal static utility class if you have many such definition classes.
    private static Expression<Func<T, object>> GetPropertyExpressionLambda(string propertyPath)
    {
        if (string.IsNullOrWhiteSpace(propertyPath))
        {
            throw new ArgumentException("Property path cannot be null or whitespace.", nameof(propertyPath));
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var body = GetNestedPropertyBody(parameter, propertyPath);
        var convertedBody = Expression.Convert(body, typeof(object));
        return Expression.Lambda<Func<T, object>>(convertedBody, parameter);
    }

    private static Expression GetNestedPropertyBody(Expression parameter, string propertyPath)
    {
        Expression currentExpression = parameter;
        var parts = propertyPath.Split('.');

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) throw new ArgumentException($"Invalid property path segment in '{propertyPath}'.");

            // Default to case-sensitive.
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