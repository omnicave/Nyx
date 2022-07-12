namespace Nyx.Utils;

public interface IConverter<in TSource, out TDest>
{
    TDest Convert(TSource source);
}

public interface IAsyncConverter<in TSource, TDest>
{
    Task<TDest> Convert(TSource source);
}

public class ConverterException : Exception
{
    public ConverterException(string message) : base(message)
    {
        
    }
}