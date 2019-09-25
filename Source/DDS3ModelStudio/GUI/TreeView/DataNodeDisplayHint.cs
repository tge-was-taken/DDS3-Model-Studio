using System;

namespace DDS3ModelStudio.GUI.TreeView
{
    [Flags]
    public enum DataNodeDisplayHint
    {
        /// <summary>
        /// The data will never have children attached.
        /// </summary>
        Leaf = 1 << 0,

        /// <summary>
        /// The data possible has children attached.
        /// </summary>
        Branch = 1 << 1,
    }
}