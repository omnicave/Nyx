using System.Reflection;

namespace Nyx.Utils;

public static class AssemblyManifestResourceReader
{
    public static StreamReader GetStreamReader(Assembly assembly, string path)
    {
        return new StreamReader(GetStream(assembly, path));
    }

    private static Stream GetStream(Assembly assembly, string path)
    {
        if (assembly == null) throw new ArgumentNullException(nameof(assembly));
        if (path == null) throw new ArgumentNullException(nameof(path));
        if (path.Trim().Length == 0) throw new ArgumentOutOfRangeException(nameof(path), "Path cannot be empty");

        var resource = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{path}");

        if (resource == null)
            throw new InvalidOperationException();
        return resource;
    }

    public static Stream GetStream<TInAssembly>(string path) => GetStream(typeof(TInAssembly).GetTypeInfo().Assembly, path);
    public static StreamReader GetStreamReader<TInAssembly>(string path) => GetStreamReader(typeof(TInAssembly).GetTypeInfo().Assembly, path);
}