using System.Collections.Generic;
using System.Linq;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.PS2.VIF;

namespace DDS3ModelLibrary.Models
{
    public class MeshType1 : Mesh
    {
        public override MeshType Type => MeshType.Type1;

        public override short VertexCount => ( short )Batches.Sum( x => x.VertexCount );

        public override short TriangleCount => ( short )Batches.Sum( x => x.TriangleCount );

        public List<MeshType1Batch> Batches { get; }

        public MeshType1()
        {
            Batches = new List<MeshType1Batch>();
        }

        protected override void Read( EndianBinaryReader reader )
        {
            var vifCodeStreamSize = reader.ReadInt16();
            MaterialIndex = reader.ReadInt16();
            reader.ReadOffset( () =>
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
                    Batches.Add( reader.ReadObject<MeshType1Batch>() );
                }
            });
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
                writer.Write( ( short ) ( vifCodeStreamSize / 16 ) );

                // Seek back to end of vif code stream
                writer.SeekBegin( vifCodeStreamEnd );
            });
            writer.Align( 16 );
        }
    }
}