using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Nyx.Cli.Rendering
{
    public interface ICliRenderer
    {
        void Render<T>(IEnumerable<T> items, Expression<Func<T, object>>? selector = null);

        void Render<T>(T item, Expression<Func<T, object>>? selector = null);

        void RenderError(Exception e);
    }
    
    public interface ICliRendererWithFormat : ICliRenderer
    {
        OutputFormat Format { get; }
    }
    //
    // class CliRenderer : ICliRenderer
    // {
    //     private readonly JsonSerializer _serializer;
    //     private readonly object? _format;
    //
    //     public CliRenderer(InvocationContext invocationContext)
    //     {
    //         _serializer = JsonSerializer.Create(new JsonSerializerSettings()
    //         {
    //             Formatting = Formatting.Indented,
    //             ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    //         });
    //         
    //         var optFormat = invocationContext.ParseResult.CommandResult.Children
    //             .Where(c => c.Symbol is Option<OutputFormat>)
    //             .ToList();
    //         
    //         _format = ((OptionResult)optFormat[0]).GetValueOrDefault<OutputFormat>();
    //     }
    //
    //     public void Render<T>(IEnumerable<T> items, Expression<Func<T, object>>? selector = null)
    //     {
    //         switch (_format)
    //         {
    //             case OutputFormat.json:
    //                 _serializer.Serialize(Console.Out, items);
    //                 break;
    //
    //             case OutputFormat.raw:
    //                 RenderRawTable(items, selector);
    //                 break;
    //
    //             case OutputFormat.table:
    //                 RenderTable(items, selector);
    //                 break;
    //         }
    //     }
    //
    //     public void Render<T>(T item, Expression<Func<T, object>>? selector = null)
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     private void RenderRawTable<T>(IEnumerable<T> items, Expression<Func<T, object>>? expression)
    //     {
    //         if (expression != null)
    //         {
    //             var structure = BuildRawOutputFromExpression(expression);
    //
    //             foreach (var item in items)
    //             {
    //                 foreach (var s in structure)
    //                 {
    //                     Console.Write($"{s.propertyFetcher(item)} ");
    //                 }
    //                 Console.WriteLine();
    //             }
    //
    //             return;
    //         }
    //         
    //         foreach (var item in items)
    //         {
    //             Console.WriteLine(item?.ToString() ?? "<null>");
    //         }
    //     }
    //
    //     private List<(TableColumn column, Func<T, object?> propertyFetcher)> BuildTableStructureFromReflection<T>()
    //     {
    //         (TableColumn column, Func<T, object?> propertyFetcher) BuildTableColumnFromPropInfo(PropertyInfo p)
    //         {
    //             return (new TableColumn(p.Name), arg => p.GetValue(arg) );
    //         }
    //         
    //         var type = typeof(T);
    //         var properties = type.GetProperties()
    //             .Select( BuildTableColumnFromPropInfo )
    //             .ToList();
    //
    //         return properties;
    //     }
    //
    //     private List<(string name, Func<T, object?> propertyFetcher)> BuildRawOutputFromExpression<T>(
    //         Expression<Func<T, object>> e)
    //     {
    //         ParameterExpression GetParameterExpression(Expression expr)
    //         {
    //             return expr switch
    //             {
    //                 ParameterExpression expression => expression,
    //                 MemberExpression memberExpression when memberExpression.Expression != null =>
    //                     GetParameterExpression(memberExpression.Expression),
    //                 _ => throw new InvalidOperationException()
    //             };
    //         }
    //         
    //         var newExpr = (NewExpression)e.Body;
    //         var map = new Dictionary<MemberInfo, MemberExpression>();
    //
    //         if (newExpr.Members == null)
    //             throw new InvalidOperationException();
    //         
    //         for (var i = 0; i < newExpr.Arguments.Count; i++)
    //         {
    //             map.Add(newExpr.Members[i], (MemberExpression)newExpr.Arguments[i]);
    //         }
    //
    //         var result = new List<(string, Func<T, object?> propertyFetcher)>();
    //
    //         foreach (var item in map)
    //         {
    //             var parameter = GetParameterExpression(item.Value);
    //
    //             var lambda = Expression.Lambda<Func<T, object?>>(item.Value, parameter);
    //             var fetcher = lambda.Compile();
    //             
    //             result.Add((item.Key.Name, fetcher));
    //         }
    //
    //         return result;
    //     }
    //     
    //     private List<(TableColumn column, Func<T, object?> propertyFetcher)> BuildTableStructureFromExpression<T>(
    //         Expression<Func<T, object>> e)
    //     {
    //         ParameterExpression GetParameterExpression(Expression expr)
    //         {
    //             return expr switch
    //             {
    //                 ParameterExpression expression => expression,
    //                 MemberExpression memberExpression when memberExpression.Expression != null =>
    //                     GetParameterExpression(memberExpression.Expression),
    //                 _ => throw new InvalidOperationException()
    //             };
    //         }
    //         
    //         var newExpr = (NewExpression)e.Body;
    //         var map = new Dictionary<MemberInfo, MemberExpression>();
    //
    //         if (newExpr.Members == null)
    //             throw new InvalidOperationException();
    //         
    //         for (var i = 0; i < newExpr.Arguments.Count; i++)
    //         {
    //              map.Add(newExpr.Members[i], (MemberExpression)newExpr.Arguments[i]);
    //         }
    //
    //         var result = new List<(TableColumn column, Func<T, object?> propertyFetcher)>();
    //
    //         foreach (var item in map)
    //         {
    //             var tableColumn = new TableColumn(item.Key.Name);
    //
    //             var parameter = GetParameterExpression(item.Value);
    //
    //             //var parameter = Expression.Parameter(typeof(T), "issue");
    //             var lambda = Expression.Lambda<Func<T, object?>>(item.Value, parameter);
    //             var fetcher = lambda.Compile();
    //             
    //             result.Add((tableColumn, fetcher));
    //         }
    //
    //         return result;
    //     }
    //
    //     private void RenderTable<T>(IEnumerable<T> items, Expression<Func<T, object>>? expression)
    //     {
    //
    //         var properties = expression == null
    //             ? BuildTableStructureFromReflection<T>()
    //             : BuildTableStructureFromExpression(expression);
    //         
    //         var table = new Table();
    //
    //         table.AddColumns(properties.Select(x=>x.column).ToArray());
    //
    //         foreach (var item in items)
    //         {
    //             var cells = properties.Select(x => x.propertyFetcher(item))
    //                 .Select(value => new Text(value?.ToString() ?? "<null>", Style.Plain))
    //                 .Cast<IRenderable>()
    //                 .ToList();
    //
    //             table.AddRow(cells);
    //             table.AddEmptyRow();
    //         }
    //         
    //         AnsiConsole.Write(table);
    //     }
    // }
}