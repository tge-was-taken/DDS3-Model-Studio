namespace DDS3ModelLibrary.Models
{
    /// <summary>
    /// Describes the different supported mesh types.
    /// </summary>
    public enum MeshType
    {
        /// <summary>
        /// No weights.
        /// </summary>
        Type1 = 1,

        /// <summary>
        /// Weights.
        /// </summary>
        Type2 = 2,

        /// <summary>
        /// Morphers.
        /// </summary>
        Type3 = 3,

        /// <summary>
        /// No weights, no VIF tags.
        /// </summary>
        Type4 = 4,

        /// <summary>
        /// Morphers, no VIF tags.
        /// </summary>
        Type5 = 5,

        /// <summary>
        /// Weights.
        /// </summary>
        Type7 = 7,

        /// <summary>
        /// No weights.
        /// </summary>
        Type8 = 8,
    }
}