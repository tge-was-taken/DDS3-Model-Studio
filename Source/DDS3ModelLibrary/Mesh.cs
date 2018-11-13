using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public abstract class Mesh : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public abstract MeshType Type { get; }

        public short MaterialId { get; set; }

        protected abstract void Read( EndianBinaryReader reader );

        protected abstract void Write( EndianBinaryWriter writer );

        void IBinarySerializable.Read( EndianBinaryReader reader, object context ) => Read( reader );

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context ) => Write( writer );
    }
}