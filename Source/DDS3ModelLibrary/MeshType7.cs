using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class MeshType7 : Mesh
    {
        public override MeshType Type => MeshType.Type7;

        public short Field00 { get; set; }

        public int Field04 { get; set; }

        public int Field08 { get; set; }

        public short TriangleCount => ( short )Triangles.Length;

        public short VertexCount { get; private set; }

        public MeshFlags Flags { get; set; }

        public short UsedNodeCount => ( short ) UsedNodeIds.Count();

        public IEnumerable<short> UsedNodeIds => Batches.SelectMany( x => x.NodeBatches ).Select( x => x.NodeId ).Distinct();

        public Triangle[] Triangles { get; private set; }

        public List<MeshBatch> Batches { get; private set; }

        public Vector2[] TexCoords2 { get; private set; }

        public MeshType7()
        {
            Batches = new List<MeshBatch>();
            Flags = MeshFlags.Bit3 | MeshFlags.TexCoords | MeshFlags.Normals | MeshFlags.Bit6 | MeshFlags.Bit21 | MeshFlags.Bit22 | MeshFlags.Bit23 |
                    MeshFlags.Bit24 | MeshFlags.FixShoes;
        }

        protected override void Read( EndianBinaryReader reader )
        {
            // Read header
            Field00    = reader.ReadInt16();
            MaterialId = reader.ReadInt16();
            Field04    = reader.ReadInt32();
            Field08    = reader.ReadInt32();
            var triangleCount = reader.ReadInt16();
            VertexCount = reader.ReadInt16();
            Flags       = ( MeshFlags )reader.ReadInt32();
            var usedNodeCount = reader.ReadInt16();
            var usedNodeIds = reader.ReadInt16Array( usedNodeCount );
            reader.Align( 16 );

            // Read triangles
            Triangles = new Triangle[triangleCount];
            for ( int i = 0; i < triangleCount; i++ )
                Triangles[ i ] = new Triangle( a: reader.ReadUInt16(), b: reader.ReadUInt16(), c: reader.ReadUInt16() );

            reader.Align( 16 );

            // Read batches
            var readVertexCount = 0;
            while ( readVertexCount < VertexCount )
            {
                var batch = reader.ReadObject<MeshBatch>( ( usedNodeIds, Flags ) );
                readVertexCount += batch.VertexCount;
                Batches.Add( batch );
            }

            if ( Flags.HasFlag( MeshFlags.TexCoord2 ) )
            {
                TexCoords2 = reader.ReadVector2s( VertexCount );
            }
        }

        protected override void Write( EndianBinaryWriter writer )
        {
            // Write header
            writer.Write( Field00 );
            writer.Write( MaterialId );
            writer.Write( Field04 );
            writer.Write( Field08 );
            writer.Write( TriangleCount );
            writer.Write( VertexCount );
            writer.Write( ( int ) Flags );
            writer.Write( UsedNodeCount );
            writer.Write( UsedNodeIds );
            writer.WriteAlignmentPadding( 16 );

            // Write triangles
            foreach ( var triangle in Triangles )
            {
                writer.Write( triangle.A );
                writer.Write( triangle.B );
                writer.Write( triangle.C );
            }

            writer.WriteAlignmentPadding( 16 );

            var vifCmd = new VifCommandBuffer();

            // Writer batches
            foreach ( var batch in Batches )
            {
                // TODO: change this
                writer.WriteObject( batch, vifCmd );
            }

            writer.WriteObject( vifCmd );

            if ( Flags.HasFlag( MeshFlags.TexCoord2 ) )
            {
                writer.Write( TexCoords2 );
            }
        }
    }
}