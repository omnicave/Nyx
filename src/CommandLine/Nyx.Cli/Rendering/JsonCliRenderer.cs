using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Nyx.Cli.Rendering
{
    class JsonCliRenderer : BaseCliRenderer
    {
        private readonly JsonSerializer _serializer;
        public override OutputFormat Format => OutputFormat.json;

        public JsonCliRenderer()
        {
            _serializer = JsonSerializer.Create(new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
        }
        
        public override void Render<T>(IEnumerable<T> items, Expression<Func<T, object>>? selector = null)
        {
            if (selector != null)
            {
                var converted = items.Select(selector.Compile()).ToList();
                _serializer.Serialize(Console.Out, converted);
            }
            else
            {
                _serializer.Serialize(Console.Out, items);
            }
        }

        public override void Render<T>(T item, Expression<Func<T, object>>? selector = null)
        {
            if (selector != null)
            {
                var converter = selector.Compile();
                _serializer.Serialize(Console.Out, converter(item));
            }
            else
            {
                _serializer.Serialize(Console.Out, item);
            }
        }

        public override void RenderError(Exception e)
        {
            _serializer.Serialize(Console.Out, new
            {
                e.Message,
                e.Source,
                e.StackTrace,
                InnerException = e.InnerException != null ? new
                {
                    e.InnerException.Message,
                    e.InnerException.Source,
                    e.InnerException.StackTrace,
                } : null
            });
        }
    }
}