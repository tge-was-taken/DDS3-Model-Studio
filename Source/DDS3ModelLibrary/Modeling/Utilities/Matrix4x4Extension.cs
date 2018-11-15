using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DDS3ModelLibrary.Modeling.Utilities
{
    public static class Matrix4x4Extension
    {
        public static Matrix4x4 Inverted( this Matrix4x4 value )
        {
            Matrix4x4.Invert( value, out var inverted );
            return inverted;
        }
    }
}
