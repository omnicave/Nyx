using System.Linq.Expressions;

namespace Nyx.Utils.Queryable;

public static class SelectQueryableBuilder
{
    public static IQueryable<IDictionary<string, object>> BuildDynamicSelect<TSource>(this IQueryable<TSource> source, IEnumerable<string> properties)
    {
        Expression GetPropertyAccessExpressionFromPath(ParameterExpression parameterExpression, string path)
        {
            var parts = path.Split('.');

            Expression result = parameterExpression;
            var sourceType = parameterExpression.Type;

            foreach (var part in parts)
            {
                var property = sourceType.GetProperty(part) ?? throw new InvalidOperationException($"Property '{part}' does not exist on type '{sourceType.Name}'.");
                sourceType = property.PropertyType;
                result = Expression.Property(result, property);
            }

            return Expression.Convert(result, typeof(object));
        }
        
        var propertyNames = properties.ToArray();
        
        // sanity checks
        if (propertyNames.Length == 0)
            throw new InvalidOperationException("No properties where supplied.");
        
        // setup expression parameter
        var parameterType = typeof(TSource);
        var parameterExpression = Expression.Parameter(parameterType, "x");

        // build property access expressions for the provided properties
        var fieldAccessorExpressions = propertyNames
            .Select(name =>
                ( 
                    name: name.Contains('.') ? name.Split('.').Last() : name,
                    fullName: name,
                    parentPropertyPath: name.Contains('.') ? string.Join('.', name.Split('.').SkipLast(1)) : string.Empty,
                    parentPropertyPathItems: name.Contains('.') ? name.Split('.').SkipLast(1).ToArray() : Enumerable.Empty<string>().ToArray(),
                    parentPropertyName: name.Contains('.') ? name.Split('.').SkipLast(1).Last() : string.Empty,
                    expr: GetPropertyAccessExpressionFromPath(parameterExpression, name)
                )
            )
            .ToArray();

        var resultType = typeof(Dictionary<string, object>);
        var addMethod = resultType.GetMethod("Add") ?? throw new InvalidOperationException($"Could not find 'Add' method in Dictionary<string, object>");

        var newExpression = Expression.New(resultType);
        var listInitExpressions = fieldAccessorExpressions.Select(
                fieldAccessor => Expression.ElementInit(addMethod, Expression.Constant(fieldAccessor.fullName), fieldAccessor.expr)
            )
            .ToArray();
        var listInit = Expression.ListInit(newExpression, listInitExpressions);

        var lambdaExpression = Expression.Lambda<Func<TSource, Dictionary<string, object>>>(
            listInit,
            parameterExpression
        );

        return source.Select(lambdaExpression);
    }
}