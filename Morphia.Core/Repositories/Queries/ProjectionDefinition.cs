using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace Morphia.Core.Repositories.Queries;

public class ProjectionDefinition<T> : List<string>
{
    // Add a property path to the projection list
    public ProjectionDefinition<T> Include(string propertyPath)
    {
        if (!string.IsNullOrWhiteSpace(propertyPath))
        {
            Add(propertyPath);
        }
        return this;
    }

    // Include a property using an expression
    public ProjectionDefinition<T> Include<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        var path = GetPropertyPath(expression.Body);
        return Include(path);
    }

    // Apply the projection to the IQueryable<T>
    public IQueryable<dynamic> ApplyProjection(IQueryable<T> query)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        var bindings = new List<MemberBinding>();

        foreach (var propPath in this)
        {
            var parts = propPath.Split('.');
            Expression current = parameter;

            foreach (var part in parts)
            {
                var prop = current.Type.GetProperty(part, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (prop == null)
                {
                    throw new InvalidOperationException($"Property '{part}' not found on type '{current.Type.Name}'");
                }
                current = Expression.Property(current, prop);
            }

            // Get the last property from the path
            var lastProperty = current.Type.GetProperty(parts.Last(), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            
            // Ensure lastProperty is not null before creating the binding
            if (lastProperty == null)
            {
                throw new InvalidOperationException($"Property '{parts.Last()}' not found on type '{current.Type.Name}'");
            }

            bindings.Add(Expression.Bind(lastProperty, current));  // Add the binding only if lastProperty is not null
        }

        var projection = Expression.MemberInit(Expression.New(typeof(ExpandoObject)), bindings);
        var selector = Expression.Lambda<Func<T, dynamic>>(projection, parameter);

        return query.Select(selector);
    }

    // Get the value from the object based on the property path
    private static object? GetValue(object obj, string path)
    {
        foreach (var part in path.Split('.'))
        {
            if (obj == null) return null;

            var prop = obj.GetType().GetProperty(part, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return null;

            var value = prop.GetValue(obj);
            if (value == null) return null;

            obj = value;
        }
        return obj;
    }

    // Convert an expression to a property path (e.g., x => x.Name -> "Name")
    private static string GetPropertyPath(Expression expression)
    {
        var stack = new Stack<string>();
        while (expression is MemberExpression memberExpr)
        {
            stack.Push(memberExpr.Member.Name);
            expression = memberExpr.Expression!;
        }
        return string.Join(".", stack);
    }
}