using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Nyx.Cli.Rendering
{
    class TabularCliRenderer : BaseCliRenderer
    {
        public override OutputFormat Format => OutputFormat.table;

        public override void Render<T>(IEnumerable<T> items, Expression<Func<T, object>>? selector = null)
        {
            var properties = selector == null
                ? PropertyReaderBuilder.GetMetadataFromReflection<T>()
                : PropertyReaderBuilder.GetMetadataFromExpression(selector);
            
            var table = new Table();

            table.AddColumns(
                properties
                    .Select(
                        x => new TableColumn(x.propertyName)
                    )
                    .ToArray()
            );

            foreach (var item in items)
            {
                var cells = properties.Select(x => x.propertyFetcher(item))
                    .Select(value => new Text(value?.ToString() ?? "<null>", Style.Plain))
                    .Cast<IRenderable>()
                    .ToList();

                table.AddRow(cells);
                table.AddEmptyRow();
            }
            
            AnsiConsole.Write(table);
        }

        public override void Render<T>(T item, Expression<Func<T, object>>? selector = null)
        {
            Render(new[] { item }.AsEnumerable());
        }

        public override void RenderError(Exception e)
        {
            AnsiConsole.WriteException(e);
        }
    }
}