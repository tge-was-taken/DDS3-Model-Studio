using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.PS2.VIF;

namespace DDS3ModelLibrary
{
    public class MeshBatchType7 : IBinarySerializable
    {
        // For debugging
        private int mTexCoordsAddress;

        public short UsedNodeCount => ( short )NodeBatches.Count;

        public short VertexCount { get; set; }

        public List<MeshNodeBatchType7> NodeBatches { get; private set; }

        public Vector2[] TexCoords { get; set; }

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public MeshBatchType7()
        {
            NodeBatches = new List<MeshNodeBatchType7>();
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            ( short[] usedNodeIds, MeshFlags flags ) = ( ( short[], MeshFlags ) )context;

            var headerPacket = reader.ReadObject<VifPacket>();
            headerPacket.Ensure( 0xFF, true, false, 1, VifUnpackElementFormat.Short, 2 );

            var usedNodeCount = headerPacket.ShortArrays[ 0 ][ 0 ];
            VertexCount = headerPacket.ShortArrays[ 0 ][ 1 ];

            if ( usedNodeCount + 1 != usedNodeIds.Length )
                throw new UnexpectedDataException();

            foreach ( short nodeId in usedNodeIds )
                NodeBatches.Add( reader.ReadObject<MeshNodeBatchType7>( nodeId ) );

            if ( flags.HasFlag( MeshFlags.TexCoord ) )
            {
                var texCoordsPacket = reader.ReadObject<VifPacket>();
                texCoordsPacket.Ensure( null, true, false, VertexCount, VifUnpackElementFormat.Float, 2 );
                TexCoords = texCoordsPacket.Vector2s;
                mTexCoordsAddress = texCoordsPacket.Address * 8;

                var texCoordsKickTag = reader.ReadObject<VifCode>();
                texCoordsKickTag.Ensure( 0, 0, VifCommand.CntMicro );
            }

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