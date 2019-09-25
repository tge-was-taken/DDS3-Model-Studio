using System;

namespace DDS3ModelStudio.GUI.TreeView
{
    /// <summary>
    /// Requires the underlying data object to be synchronized with the view model.
    /// </summary>
    [ AttributeUsage( AttributeTargets.Property ) ]
    public sealed class RequireSyncAttribute : Attribute
    {
        public RequireSyncAttribute()
        {
        }
    }
}