namespace Nyx.Orleans.Host.Internal;

internal static class TypeExtensions
{
    internal static bool IsExceptionType(this Type t)
    {

        // some heuristical checking of type using conventions
        if (t?.FullName?.Contains("Exception", StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        
        // brute force checking of type
        var r = t;
        while (r != null)
        {
            if (r == typeof(Exception))
                return true;
            if (r == typeof(object))
                return false;
            r = r.BaseType;
        }
        return false;
    }
}