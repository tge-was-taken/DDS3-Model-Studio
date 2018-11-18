using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.PS2.VIF;

namespace DDS3ModelLibrary
{
    public class MeshType2 : Mesh
    {
        public override MeshType Type => MeshType.Type2;

        public override short VertexCount => ( short )Batches.Sum( x => x.VertexCount );

        public override short TriangleCount => ( short )Batches.Sum( x => x.TriangleCount );

        public short UsedNodeCount => Batches.Count == 0 ? ( short )0 : ( short )Batches[0].NodeBatches.Count;

        public IEnumerable<short> UsedNodeIndices =>
            Batches.Count == 0 ? Enumerable.Empty<short>() : Batches[0].NodeBatches.Select( x => x.NodeIndex );

        public List<MeshType2Batch> Batches { get; private set; }

        public MeshType2()
        {
            Batches = new List<MeshType2Batch>();
        }

        protected override void Read( EndianBinaryReader reader )
        {
            var vifCodeStreamSize = reader.ReadInt16();
            MaterialIndex = reader.ReadInt16();
            var vifCodeStreamOffset = reader.ReadInt32();
            var usedNodeCount = reader.ReadInt16();
            var field0A = reader.ReadInt16Expects( 0, "Field0A is not 0" );
            var usedNodes = reader.ReadInt16Array( usedNodeCount );
            Trace.Assert( usedNodeCount > 0 && usedNodeCount <= 4 );
            reader.ReadAtOffset( vifCodeStreamOffset, () =>
            {
                var vifCodeStreamEnd = reader.Position + ( vifCodeStreamSize * 16 );
                while ( reader.Position < vifCodeStreamEnd )
                {
                    var vifTag = reader.ReadObject<VifTag>();
                    if ( vifTag.Command == 0 && AlignmentHelper.Align( reader.Position, 16 ) == vifCodeStreamEnd )
                    {
                        // Stream padding, stop reading
                        break;
                    }

                    reader.SeekCurrent( -VifTag.SIZE );
                    Batches.Add( reader.ReadObject<MeshType2Batch>( usedNodes ) );
                }
            } );
            reader.Align( 16 );
        }

        protected override void Write( EndianBinaryWriter writer )
        {
            var start = writer.Position;
            writer.SeekCurrent( 2 );
            writer.Write( MaterialIndex );
            writer.ScheduleWriteOffset( () =>
            {
                // Build vif code stream
                var vif = new VifCodeStreamBuilder();
                foreach ( var batch in Batches )
                    writer.WriteObject( batch, vif );

                // Write vif code stream
                var vifCodeStreamStart = writer.Position;
                writer.WriteObject( vif );
                var vifCodeStreamEnd = writer.Position;

                // Calculate and write vif code stream size in the header
                var vifCodeStreamSize = vifCodeStreamEnd - vifCodeStreamStart;
                writer.SeekBegin( start );
                writer.Write( ( short )( vifCodeStreamSize / 16 ) );

                // Seek back to end of vif code stream
                writer.SeekBegin( vifCodeStreamEnd );
            } );
            writer.Write( UsedNodeCount );
            writer.Write( ( short ) 0 );
            writer.Write( UsedNodeIndices );
            writer.Align( 16 );
        }
    }
}