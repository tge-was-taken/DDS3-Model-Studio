using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.PS2.VIF;
using System.Diagnostics;

namespace DDS3ModelLibrary
{
    public class MeshType7 : Mesh
    {
        private Vector2[] mTexCoords2;

        public override MeshType Type => MeshType.Type7;

        public short Field00 { get; set; }

        public int Field04 { get; set; }

        public int Field08 { get; set; }

        public short TriangleCount => ( short )Triangles.Length;

        public short VertexCount { get; set; }

        public MeshFlags Flags { get; set; }

        public short UsedNodeCount => ( short ) UsedNodeIds.Count();

        public IEnumerable<short> UsedNodeIds => Batches.SelectMany( x => x.NodeBatches ).Select( x => x.NodeId ).Distinct();

        public Triangle[] Triangles { get; set; }

        public List<MeshBatchType7> Batches { get; private set; }

        public Vector2[] TexCoords2
        {
            get => mTexCoords2;
            set
            {
                if ( ( mTexCoords2 = value ) == null )
                    Flags &= ~MeshFlags.TexCoord2;
                else
                    Flags |= MeshFlags.TexCoord2;
            }
        }

        public MeshType7()
        {
            Batches = new List<MeshBatchType7>();
            Flags = MeshFlags.Bit3 | MeshFlags.TexCoord | MeshFlags.Bit5 | MeshFlags.Bit6 | MeshFlags.Bit21 | MeshFlags.Bit22 | MeshFlags.Bit23 |
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
            var flags = Flags = ( MeshFlags )reader.ReadInt32();
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
                var batch = reader.ReadObject<MeshBatchType7>( ( usedNodeIds, Flags ) );
                readVertexCount += batch.VertexCount;
                Batches.Add( batch );
            }

            if ( Flags.HasFlag( MeshFlags.TexCoord2 ) )
            {
                TexCoords2 = reader.ReadVector2s( VertexCount );
            }

            Debug.Assert( Flags == flags, "Flags doesn't match value read from file" );
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

            var vifCmd = new VifCodeStreamBuilder();

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