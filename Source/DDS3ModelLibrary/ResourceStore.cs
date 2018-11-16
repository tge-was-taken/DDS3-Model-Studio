using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDS3ModelLibrary
{
    public static class ResourceStore
    {
        public static string Path => "resources\\";

        public static string GetPath( string path ) => System.IO.Path.Combine( Path, path );
    }
}
