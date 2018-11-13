using System.IO;
using System.Text;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.IO
{
    public class ResourceWriter : EndianBinaryWriter
    {
        public ResourceWriter( Stream input, Endianness endianness ) : base( input, endianness )
        {
        }

        public ResourceWriter( string filepath, Endianness endianness ) : base( filepath, endianness )
        {
        }

        public ResourceWriter( Stream input, Encoding encoding, Endianness endianness ) : base( input, encoding, endianness )
        {
        }

        public ResourceWriter( Stream input, bool leaveOpen, Endianness endianness ) : base( input, leaveOpen, endianness )
        {
        }

        public ResourceWriter( Stream input, Encoding encoding, bool leaveOpen, Endianness endianness ) : base( input, encoding, leaveOpen, endianness )
        {
        }
    }
}
