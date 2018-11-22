using System.Numerics;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Models
{
    public class MeshType5BlendShape : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public Vector3[] Positions { get; set; }

        public Vector3[] Normals { get; set; }

        public MeshType5BlendShape()
        {
        }

        public MeshType5BlendShape( Vector3[] positions, Vector3[] normals )
        {
            Positions = positions;
            Normals = normals;
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            var vertexCount = ( short )context;
            Positions = reader.ReadVector3s( vertexCount );
            reader.Align( 16 );
            Normals = reader.ReadVector3s( vertexCount );
            reader.Align( 16 );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( Positions );
            writer.Align( 16 );
            writer.Write( Normals );
            writer.Align( 16 );
        }
    }
}