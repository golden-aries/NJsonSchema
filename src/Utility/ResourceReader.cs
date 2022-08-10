
using System.Reflection;

public static class ResourceReader
{
    /// <summary>
    /// Returns resource stream from assembly
    /// </summary>
    /// <param name="asm">Assembly to get resource from</param>
    /// <param name="path">Resource path</param>
    /// <returns>Resource stream from assembly</returns>
    public static Stream GetResource(this Assembly asm, string path)
    {
        return asm.GetManifestResourceStream(asm.GetName().Name + "." + path.Replace('/', '.'))
            ?? throw new Exception($"Unable to find embedded resource! {path}");
    }

    /// <summary>
    /// Get source string
    /// </summary>
    /// <param name="asm"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string GetResourceString(this Assembly asm, string path)
    {
        using var stream = asm.GetResource(path);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}