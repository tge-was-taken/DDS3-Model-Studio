using System.IO;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.PS2.VIF;

namespace DDS3ModelLibrary.Models
{
    public class MeshType8Batch : IBinarySerializable
    {
        public short VertexCount => Positions != null ? ( short ) Positions.Length : ( short ) 0;

        public Vector3[] Positions { get; set; }

        public Vector3[] Normals { get; set; }

        public Vector2[] TexCoords { get; set; }

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public MeshType8Batch()
        {
        }

        public (Vector3[] Positions, Vector3[] Normals) Transform( Matrix4x4 nodeWorldTransform )
        {
            var positions = new Vector3[VertexCount];
            var normals   = new Vector3[positions.Length];

            for ( int i = 0; i < Positions.Length; i++ )
            {
                positions[i] = Vector3.Transform( Positions[i], nodeWorldTransform );
                normals[i]   = Vector3.TransformNormal( Normals[i], nodeWorldTransform );
            }

            return (positions, normals);
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            var flags = ( MeshFlags )context;

            var headerPacket = reader.ReadObject<VifPacket>();
            headerPacket.Ensure( 0xFF, true, false, 1, VifUnpackElementFormat.Short, 2 );

            var vertexCount = headerPacket.ShortArrays[0][0];
            if ( headerPacket.ShortArrays[ 0 ][ 1 ] != 0 )
                throw new InvalidDataException();

            var positionsPacket = reader.ReadObject<VifPacket>();
            positionsPacket.Ensure( 0, true, false, vertexCount, VifUnpackElementFormat.Float, 3 );
            Positions = positionsPacket.Vector3s;

            var normalsPacket = reader.ReadObject<VifPacket>();
            normalsPacket.Ensure( 0x18, true, false, vertexCount, VifUnpackElementFormat.Float, 3 );
            Normals = normalsPacket.Vector3s;

            var texCoordPacket = reader.ReadObject<VifPacket>();
            texCoordPacket.Ensure( 0x30, true, false, vertexCount, VifUnpackElementFormat.Float, 2 );
            TexCoords = texCoordPacket.Vector2s;

            var activateTag = reader.ReadObject<VifCode>();
            activateTag.Ensure( 0x16, 0, VifCommand.ActMicro );

            var flushTag = reader.ReadObject<VifCode>();
            flushTag.Ensure( 0, 0, VifCommand.FlushEnd );
            reader.Align( 16 );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            var vif = ( VifCodeStreamBuilder )context;
            vif.UnpackHeader( VertexCount, 0 );
            vif.Unpack( Positions );
            vif.Unpack( Normals );
            vif.Unpack( TexCoords );
            vif.ActivateMicro( 0x16 );
            vif.FlushEnd();
        }
    }
}