namespace DDS3ModelLibrary.Data
{
    public static class ResourceStore
    {
        public static string Path => "resources\\";

        public static string GetPath(string path) => System.IO.Path.Combine(Path, path);
    }
}
