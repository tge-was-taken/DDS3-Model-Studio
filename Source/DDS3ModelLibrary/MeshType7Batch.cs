using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.PS2.VIF;

namespace DDS3ModelLibrary
{
    public class MeshType7Batch : IBinarySerializable
    {
        public short UsedNodeCount => ( short )NodeBatches.Count;

        public short VertexCount => ( short ) ( NodeBatches.Count > 0 ? NodeBatches[ 0 ].VertexCount : 0 );

        public List<MeshType7NodeBatch> NodeBatches { get; }

        public Vector2[] TexCoords { get; set; }

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public MeshType7Batch()
        {
            NodeBatches = new List<MeshType7NodeBatch>();
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            ( short[] usedNodeIds, MeshFlags flags ) = ( ( short[], MeshFlags ) )context;

            var headerPacket = reader.ReadObject<VifPacket>();
            headerPacket.Ensure( 0xFF, true, false, 1, VifUnpackElementFormat.Short, 2 );

            var usedNodeCount = headerPacket.ShortArrays[ 0 ][ 0 ];
            var vertexCount = headerPacket.ShortArrays[ 0 ][ 1 ];

            if ( usedNodeCount + 1 != usedNodeIds.Length )
                throw new UnexpectedDataException();

            foreach ( short nodeId in usedNodeIds )
                NodeBatches.Add( reader.ReadObject<MeshType7NodeBatch>( nodeId ) );

            var texCoordsPacket = reader.ReadObject<VifPacket>();
            texCoordsPacket.Ensure( null, true, false, vertexCount, VifUnpackElementFormat.Float, 2 );
            TexCoords         = texCoordsPacket.Vector2s;

            var texCoordsKickTag = reader.ReadObject<VifCode>();
            texCoordsKickTag.Ensure( 0, 0, VifCommand.CntMicro );

            var flushTag = reader.ReadObject<VifCode>();
            flushTag.Ensure( 0, 0, VifCommand.FlushEnd );
            reader.Align( 16 );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            var vif = ( VifCodeStreamBuilder )context;

            if ( UsedNodeCount == 1 )
                throw new InvalidOperationException( "Mesh batch must have at least 2 used nodes" );

            vif.UnpackHeader( ( short ) ( UsedNodeCount - 1 ), VertexCount );

            for ( var i = 0; i < NodeBatches.Count; i++ )
            {
                var batch = NodeBatches[ i ];
                writer.WriteObject( batch, ( vif, i == 0 ) );
            }

            if ( TexCoords != null )
            {
                vif.Unpack( TexCoords );
                vif.ExecuteMicro();
            }

            vif.FlushEnd();
        }
    }
}