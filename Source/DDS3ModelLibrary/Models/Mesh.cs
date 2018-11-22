using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Models
{
    public abstract class Mesh : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public abstract MeshType Type { get; }

        public short MaterialIndex { get; set; }

        public abstract short VertexCount { get; }

        public abstract short TriangleCount { get; }

        protected abstract void Read( EndianBinaryReader reader );

        protected abstract void Write( EndianBinaryWriter writer );

        void IBinarySerializable.Read( EndianBinaryReader reader, object context ) => Read( reader );

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context ) => Write( writer );
    }
}