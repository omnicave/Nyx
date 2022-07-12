using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Nyx.Cli.Rendering
{
    internal static class PropertyReaderBuilder
    {
        public static List<(string propertyName, Func<T, object?> propertyFetcher)> GetMetadataFromReflection<T>()
        {
            bool AllowedPropertyType(Type propertyType)
            {
                if (propertyType.IsArray)
                {
                    var arrayElementType = propertyType.GetElementType();
                    if (arrayElementType == null)
                        throw new InvalidOperationException();
                    
                    return AllowedPropertyType(arrayElementType);
                }
                
                if (!propertyType.IsValueType && propertyType != typeof(string)) 
                    return false;

                return true;
            }
            
            var typeInfo = typeof(T).GetTypeInfo();
            
            var target = Expression.Parameter(typeInfo);

            
            var result = new List<(string propertyName, Func<T, object?> propertyFetcher)>();


            foreach (var propertyInfo in typeInfo.GetProperties())
            {
                if (!AllowedPropertyType(propertyInfo.PropertyType))
                    continue;

                var memberAccess = Expression.MakeMemberAccess(target, propertyInfo);
                var convertMemberAccessReturnType = Expression.Convert(memberAccess, typeof(object));
                
                var lambda = Expression.Lambda<Func<T, object?>>(convertMemberAccessReturnType, target);
                
                var fetcher = lambda.Compile();
                
                result.Add((propertyInfo.Name, fetcher));
            }

            return result;
        }
        
        public static List<(string propertyName, Func<T, object?> propertyFetcher)> GetMetadataFromExpression<T>(
            Expression<Func<T, object>> e)
        {
            ParameterExpression GetParameterExpression(Expression expr)
            {
                // if (expr is MethodCallExpression mce)
                // {
                //     GetParameterExpression(mce.Object )
                //     return Expression.Parameter(mce.Method.DeclaringType!);
                // }
                
                return expr switch
                {
                    ParameterExpression expression => expression,
                    MemberExpression memberExpression when memberExpression.Expression != null =>
                        GetParameterExpression(memberExpression.Expression),
                    MethodCallExpression mce when mce.Object != null => GetParameterExpression(mce.Object),
                    _ => throw new InvalidOperationException($"{expr.GetType()} is not valid.")
                };
            }

            Expression EnsureExpressionReturnsObjectCompatibleValue(Expression expr)
            {
                // if (expr is )
                // {
                //     return 
                // }
                
                return expr switch
                {
                    UnaryExpression uE => uE,
                    MemberExpression me => Expression.Convert(me, typeof(object)),
                    MethodCallExpression mce => Expression.Convert(mce, typeof(object)), 
                    _ => throw new InvalidOperationException($"{expr.GetType()} is not valid for object conversion.")
                };
            }
            
            var newExpr = (NewExpression)e.Body;
            var map = new Dictionary<MemberInfo, Expression>();

            if (newExpr.Members == null)
                throw new InvalidOperationException();
            
            for (var i = 0; i < newExpr.Arguments.Count; i++)
            {
                map.Add(newExpr.Members[i], newExpr.Arguments[i] );
            }

            var result = new List<(string propertyName, Func<T, object?> propertyFetcher)>();

            foreach (var item in map)
            {
                var parameter = GetParameterExpression(item.Value);

                //var parameter = Expression.Parameter(typeof(T), "issue");
                var lambda = Expression.Lambda<Func<T, object?>>(EnsureExpressionReturnsObjectCompatibleValue(item.Value), parameter);
                var fetcher = lambda.Compile();
                
                result.Add((item.Key.Name, fetcher));
            }

            return result;
        }
    }
}