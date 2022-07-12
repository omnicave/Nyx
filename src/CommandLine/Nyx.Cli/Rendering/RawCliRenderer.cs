using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Nyx.Cli.Rendering
{
    class RawCliRenderer : BaseCliRenderer
    {
        public override OutputFormat Format => OutputFormat.raw;

        public override void Render<T>(IEnumerable<T> items, Expression<Func<T, object>>? selector = null)
        {
            var structure = selector == null 
                ? PropertyReaderBuilder.GetMetadataFromReflection<T>()
                : PropertyReaderBuilder.GetMetadataFromExpression(selector);

            foreach (var item in items)
            {
                foreach (var s in structure)
                {
                    Console.Write($"{s.propertyFetcher(item)} ");
                }
                Console.WriteLine();
            }
        }

        public override void Render<T>(T item, Expression<Func<T, object>>? selector = null)
        {
            var structure = selector == null 
                ? PropertyReaderBuilder.GetMetadataFromReflection<T>()
                : PropertyReaderBuilder.GetMetadataFromExpression(selector);

            foreach (var p in structure)
            {

                var value = p.propertyFetcher(item) ?? "<null>";

                var stringFormat = value switch
                {
                    object[] { Length: > 0 } strArray => string.Join(", ", strArray),
                    object[] { Length: 0 } => "<empty>",
                    null => "<null>",
                    _ => value.ToString()
                };
                
                Console.WriteLine("{0}: {1}", p.propertyName, stringFormat);
            }
        }
    }
}