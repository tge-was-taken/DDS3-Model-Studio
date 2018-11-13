using System.IO;
using System.Text;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.IO
{
    public class ResourceReader : EndianBinaryReader
    {
        public ResourceReader( Stream input, Endianness endianness ) : base( input, endianness )
        {
        }

        public ResourceReader( string filepath, Endianness endianness ) : base( filepath, endianness )
        {
        }

        public ResourceReader( Stream input, Encoding encoding, Endianness endianness ) : base( input, encoding, endianness )
        {
        }

        public ResourceReader( Stream input, bool leaveOpen, Endianness endianness ) : base( input, leaveOpen, endianness )
        {
        }

        public ResourceReader( Stream input, Encoding encoding, bool leaveOpen, Endianness endianness ) : base( input, encoding, leaveOpen, endianness )
        {
        }
    }
}
