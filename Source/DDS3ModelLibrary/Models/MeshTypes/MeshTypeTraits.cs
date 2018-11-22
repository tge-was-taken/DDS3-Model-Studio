namespace DDS3ModelLibrary.Models
{
    /// <summary>
    /// Utility classes for obtaining various traits of any given <see cref="MeshType"/>.
    /// </summary>
    public static class MeshTypeTraits
    {
        /// <summary>
        /// Gets if the type supports weights.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool HasWeights( MeshType type ) => type == MeshType.Type2 || type == MeshType.Type7;

        /// <summary>
        /// Gets if the type supports morph shapes.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool HasMorphers( MeshType type ) => type == MeshType.Type3 || type == MeshType.Type5;
    }
}