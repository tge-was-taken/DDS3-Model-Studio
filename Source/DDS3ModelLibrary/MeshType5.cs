using System.Collections.Generic;
using System.Diagnostics;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public class MeshType5 : Mesh
    {
        public override MeshType Type => MeshType.Type5;

        public short TriangleCount => ( short )Triangles.Length;

        public short VertexCount => ( short ) ( BlendShapes.Count == 0 ? 0 : BlendShapes[ 0 ].Positions.Length );

        public MeshFlags Flags { get; set; }

        public Triangle[] Triangles { get; set; }

        public List<MeshType5BlendShape> BlendShapes { get; }

        public MeshType5()
        {
            BlendShapes = new List<MeshType5BlendShape>();
            Flags = MeshFlags.Bit3 | MeshFlags.TexCoord | MeshFlags.Bit5 | MeshFlags.Bit6 | MeshFlags.Bit21 | MeshFlags.Bit22 | MeshFlags.Normal |
                    MeshFlags.Bit24;
        }

        protected override void Read( EndianBinaryReader reader )
        {
            // Read header
            var field00 = reader.ReadInt16Expects( 0, "Field00 is not 0" );
            MaterialId = reader.ReadInt16();
            var shapeCount = reader.ReadInt16();
            var field06 = reader.ReadInt16();
            var field08 = reader.ReadInt32Expects( 0, "Field08 is not 0" );
            var triangleCount = reader.ReadInt16();
            var vertexCount = reader.ReadInt16();
            var flags = Flags = ( MeshFlags )reader.ReadInt32();
            reader.Align( 16 );

            // Read triangles
            Triangles = new Triangle[triangleCount];
            for ( int i = 0; i < triangleCount; i++ )
                Triangles[i] = new Triangle( a: reader.ReadUInt16(), b: reader.ReadUInt16(), c: reader.ReadUInt16() );

            reader.Align( 16 );

            // Read blend shapes
            for ( int i = 0; i < shapeCount; i++ )
            {
                var shape = new MeshType5BlendShape();
                shape.Positions = reader.ReadVector3s( vertexCount );
                reader.Align( 16 );
                shape.Normals = reader.ReadVector3s( vertexCount );
                reader.Align( 16 );
                BlendShapes.Add( shape );
            }

            for ( int i = 0; i < shapeCount; i++ )
            {
                BlendShapes[i].TexCoords = reader.ReadVector2s( vertexCount );
                reader.Align( 16 );
            }

            Debug.Assert( Flags == flags, "Flags doesn't match value read from file" );
        }

        protected override void Write( EndianBinaryWriter writer )
        {
            // Write header
            writer.Write( ( short )0 );
            writer.Write( MaterialId );
            writer.Write( ( short ) BlendShapes.Count );
            writer.Write( ( short ) 0 );
            writer.Write( 0 );
            writer.Write( TriangleCount );
            writer.Write( VertexCount );
            writer.Write( ( int )Flags );
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
            foreach ( var shape in BlendShapes )
            {
                writer.Write( shape.Positions );
                writer.Align( 16 );
                writer.Write( shape.Normals );
                writer.Align( 16 );
            }

            foreach ( var shape in BlendShapes )
            {
                writer.Write( shape.TexCoords );
                writer.Align( 16 );
            }
        }
    }
}