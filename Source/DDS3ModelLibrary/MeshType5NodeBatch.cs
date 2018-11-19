using System.Numerics;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class MeshType5NodeBatch : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public short NodeIndex { get; set; }

        public Vector4[] Positions { get; set; }

        public Vector3[] Normals { get; set; }

        public MeshType5NodeBatch()
        {
        }

        public MeshType5NodeBatch( Vector4[] positions, Vector3[] normals )
        {
            Positions = positions;
            Normals   = normals;
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            ( short vertexCount, short nodeIndex ) = ( (short, short) ) context;
            NodeIndex = nodeIndex;
            Positions = reader.ReadVector4Array( vertexCount );
            Normals   = reader.ReadVector3s( vertexCount );
            reader.Align( 16 );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( Positions );
            writer.Write( Normals );
            writer.Align( 16 );
        }
    }
}