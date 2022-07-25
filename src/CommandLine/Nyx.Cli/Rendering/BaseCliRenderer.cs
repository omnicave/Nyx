using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq.Expressions;

namespace Nyx.Cli.Rendering
{
    public abstract class BaseCliRenderer : ICliRendererWithFormat
    {
        public abstract OutputFormat Format { get; }

        public abstract void Render<T>(IEnumerable<T> items, Expression<Func<T, object>>? selector = null);
        public abstract void Render<T>(T item, Expression<Func<T, object>>? selector = null);
        public abstract void RenderError(Exception e);
    }
}