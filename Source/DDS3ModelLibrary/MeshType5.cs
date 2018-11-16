using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class MeshType5 : Mesh
    {
        public override MeshType Type => MeshType.Type5;

        public short BlendShapeCount => ( short )BlendShapes.Count;

        public short Material2Index { get; set; }

        public short TriangleCount => ( short )Triangles.Length;

        public short VertexCount => ( short ) ( BlendShapes.Count == 0 ? 0 : BlendShapes[ 0 ].Positions.Length );

        public MeshFlags Flags { get; set; }

        /// <summary>
        /// Most likely deprecated, used only in a few odd files.
        /// </summary>
        public short UsedNodeCount => ( short )NodeSplits.Count;

        /// <summary>
        /// Most likely deprecated, used only in a few odd files.
        /// </summary>
        public IEnumerable<short> UsedNodeIndices => NodeSplits.Select( x => x.NodeIndex );

        public Triangle[] Triangles { get; set; }

        public List<MeshType5BlendShape> BlendShapes { get; }

        public Vector2[] TexCoords { get; set; }

        public Vector2[] TexCoords2 { get; set; }

        /// <summary>
        /// Most likely deprecated, used only in a few odd files.
        /// </summary>
        public List<MeshType5NodeSplit> NodeSplits { get; }

        public MeshType5()
        {
            BlendShapes = new List<MeshType5BlendShape>();
            Flags = MeshFlags.Bit3 | MeshFlags.Bit5 | MeshFlags.Bit6 | MeshFlags.Bit21 | MeshFlags.Bit22 | MeshFlags.Bit24 | MeshFlags.Bit28;
            NodeSplits = new List<MeshType5NodeSplit>();
        }

        protected override void Read( EndianBinaryReader reader )
        {
            // Read header
            var field00 = reader.ReadInt16Expects( 0, "Field00 is not 0" );
            MaterialIndex = reader.ReadInt16();
            var shapeCount = reader.ReadInt16();
            Material2Index = reader.ReadInt16();
            var field08 = reader.ReadInt32Expects( 0, "Field08 is not 0" );
            var triangleCount = reader.ReadInt16();
            var vertexCount = reader.ReadInt16();
            var flags = Flags = ( MeshFlags )reader.ReadInt32();
            var usedNodeCount = reader.ReadInt16();
            var usedNodes = reader.ReadInt16Array( usedNodeCount );
            reader.Align( 16 );

            // Read triangles
            Triangles = new Triangle[triangleCount];
            for ( int i = 0; i < triangleCount; i++ )
                Triangles[i] = new Triangle( a: reader.ReadUInt16(), b: reader.ReadUInt16(), c: reader.ReadUInt16() );

            reader.Align( 16 );

            // Read blend shapes
            for ( int i = 0; i < shapeCount; i++ )
                BlendShapes.Add( reader.ReadObject<MeshType5BlendShape>( vertexCount ) );

            if ( flags.HasFlag( MeshFlags.TexCoord ) )
                TexCoords = reader.ReadVector2s( vertexCount );

            if ( flags.HasFlag( MeshFlags.TexCoord2 ) )
                TexCoords2 = reader.ReadVector2s( vertexCount );

            // TODO: where does this go?
            if ( usedNodeCount > 0 )
            {
                foreach ( var nodeIndex in usedNodes )
                    NodeSplits.Add( reader.ReadObject<MeshType5NodeSplit>( ( vertexCount, nodeIndex ) ) );
            }

            Debug.Assert( Flags == flags, "Flags doesn't match value read from file" );
        }

        protected override void Write( EndianBinaryWriter writer )
        {
            // Write header
            writer.Write( ( short )0 );
            writer.Write( MaterialIndex );
            writer.Write( BlendShapeCount );
            writer.Write( Material2Index );
            writer.Write( 0 );
            writer.Write( TriangleCount );
            writer.Write( VertexCount );
            writer.Write( ( int )Flags );
            writer.Write( UsedNodeCount );
            writer.Write( UsedNodeIndices );
            writer.Align( 16 );

            // Write triangles
            foreach ( var triangle in Triangles )
            {
                writer.Write( triangle.A );
                writer.Write( triangle.B );
                writer.Write( triangle.C );
            }

            writer.Align( 16 );

            // Write blend shapes
            writer.WriteObjects( BlendShapes );

            if ( Flags.HasFlag( MeshFlags.TexCoord ) )
                writer.Write( TexCoords );

            if ( Flags.HasFlag( MeshFlags.TexCoord2 ) )
                writer.Write( TexCoords2 );

            // TODO: where does this go?
            foreach ( var nodeBatch in NodeSplits )
                writer.WriteObject( nodeBatch );
        }
    }
}