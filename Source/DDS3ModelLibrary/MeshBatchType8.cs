using System.Linq;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.Primitives;
using DDS3ModelLibrary.PS2.VIF;

namespace DDS3ModelLibrary
{
    public class MeshBatchType8 : IBinarySerializable
    {
        public short VertexCount => Positions != null ? ( short ) Positions.Length : ( short ) 0;

        public Vector3[] Positions { get; set; }

        public Vector3[] Normals { get; set; }

        public Vector2[] TexCoords { get; set; }

        public Color[] Colors { get; set; }

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public MeshBatchType8()
        {
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            var flags = ( MeshFlags )context;

            var headerPacket = reader.ReadObject<VifPacket>();
            headerPacket.Ensure( 0xFF, true, false, 1, VifUnpackElementFormat.Short, 2 );

            var vertexCount = headerPacket.ShortArrays[0][0];
            if ( headerPacket.ShortArrays[ 0 ][ 1 ] != 0 )
                throw new UnexpectedDataException();

            var positionsPacket = reader.ReadObject<VifPacket>();
            positionsPacket.Ensure( 0, true, false, vertexCount, VifUnpackElementFormat.Float, 3 );
            Positions = positionsPacket.Vector3s;

            // TODO: verify
            if ( flags.HasFlag( MeshFlags.Normal )  )
            {
                var normalsPacket = reader.ReadObject<VifPacket>();
                normalsPacket.Ensure( 0x18, true, false, vertexCount, VifUnpackElementFormat.Float, 3 );
                Normals = normalsPacket.Vector3s;
            }

            if ( flags.HasFlag( MeshFlags.TexCoord ) )
            {
                var texCoordPacket = reader.ReadObject<VifPacket>();
                texCoordPacket.Ensure( 0x30, true, false, vertexCount, VifUnpackElementFormat.Float, 2 );
                TexCoords = texCoordPacket.Vector2s;
            }

            //if ( flags.HasFlag( MeshFlags.Color ) )
            //{
            //    var colorPacket = reader.ReadObject<VifPacket>();
            //    // TODO: verify parameters properly
            //    if ( colorPacket.ElementFormat != VifUnpackElementFormat.Byte )
            //        throw new UnexpectedDataException();

            //    Colors = colorPacket.SignedByteArrays.Select( x => new Color( ( byte ) x[ 0 ], ( byte ) x[ 1 ], ( byte ) x[ 2 ], ( byte ) x[ 3 ] ) )
            //                        .ToArray();
            //}

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

            if ( Normals != null )
                vif.Unpack( Normals );

            if ( TexCoords != null )
                vif.Unpack( TexCoords );

            if ( Colors != null )
                vif.Unpack( Colors.Select( x => new[] { x.R, x.G, x.B, x.A } ) );

            vif.ActivateMicro( 0x16 );
            vif.FlushEnd();
        }
    }
}