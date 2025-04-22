using System.Linq.Expressions;
using System.Reflection;

namespace Morphia.Core.Repositories.Queries;

public class ProjectionDefinition<T> : List<string>
{
    public ProjectionDefinition<T> Include(string propertyPath)
    {
        if (!string.IsNullOrWhiteSpace(propertyPath)) Add(propertyPath);
        return this;
    }

    public ProjectionDefinition<T> Include<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        var path = GetPropertyPath(expression.Body);
        return Include(path);
    }

    public IQueryable<T> ApplyProjection(IQueryable<T> query)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        var bindings = new List<MemberBinding>();

        foreach (var propPath in this)
        {
            var parts = propPath.Split('.');
            Expression current = parameter;
            PropertyInfo? lastProperty = null;

            foreach (var part in parts)
            {
                lastProperty = current.Type.GetProperty(part, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (lastProperty == null) throw new InvalidOperationException($"Property '{part}' not found on type '{current.Type.Name}'");
                current = Expression.Property(current, lastProperty);
            }

            if (lastProperty != null)
            {
                bindings.Add(Expression.Bind(lastProperty, current));
            }
        }

        var projection = Expression.MemberInit(Expression.New(typeof(T)), bindings);
        var selector = Expression.Lambda<Func<T, T>>(Expression.Convert(projection, typeof(T)), parameter);
        return query.Select(selector);
    }

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