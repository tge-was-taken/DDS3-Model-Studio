using System;
using System.Diagnostics;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Models
{
    public class MeshType4 : Mesh
    {
        private Triangle[] mTriangles;
        private Vector3[]  mPositions;
        private Vector3[]  mNormals;

        public override MeshType Type => MeshType.Type4;

        public override short TriangleCount => ( short )Triangles.Length;

        public override short VertexCount => ( short )Positions.Length;

        public MeshFlags Flags { get; set; }

        public Triangle[] Triangles
        {
            get => mTriangles;
            set => mTriangles = value ?? throw new ArgumentNullException( nameof( value ) );
        }

        public Vector3[] Positions
        {
            get => mPositions;
            set => mPositions = value ?? throw new ArgumentNullException( nameof( value ) );
        }

        public Vector3[] Normals
        {
            get => mNormals;
            set => mNormals = value ?? throw new ArgumentNullException( nameof( value ) );
        }

        public MeshType4()
        {
            // 1 Bit0, SmoothShading, Bit5, Bit6, RequiredForField, Bit22, Normal
            // 7 SmoothShading, Bit5, Bit6, RequiredForField, Bit22, Normal
            Flags = MeshFlags.SmoothShading | MeshFlags.Bit5 | MeshFlags.Bit6 | MeshFlags.RequiredForField | MeshFlags.Bit22 | MeshFlags.Normal;
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

        protected override void Read( EndianBinaryReader reader )
        {
            // Read header
            var field00 = reader.ReadInt16Expects( 0, "Field00 is not 0" );
            MaterialIndex = reader.ReadInt16();
            var field04       = reader.ReadInt16Expects( 0, "Field04 is not 0" );
            var field06       = reader.ReadInt16Expects( 0, "Field06 is not 0" );
            var field08       = reader.ReadInt32Expects( 0, "Field08 is not 0" );
            var triangleCount = reader.ReadInt16();
            var vertexCount   = reader.ReadInt16();
            var flags         = Flags = ( MeshFlags )reader.ReadInt32();
            reader.Align( 16 );

            // Read triangles
            Triangles = new Triangle[triangleCount];
            for ( int i = 0; i < triangleCount; i++ )
                Triangles[i] = new Triangle( a: reader.ReadUInt16(), b: reader.ReadUInt16(), c: reader.ReadUInt16() );

            reader.Align( 16 );

            Positions = reader.ReadVector3s( vertexCount );
            reader.Align( 16 );

            Normals = reader.ReadVector3s( vertexCount );
            reader.Align( 16 );

            Debug.Assert( Flags == flags, "Flags doesn't match value read from file" );
        }

        protected override void Write( EndianBinaryWriter writer )
        {
            // Write header
            writer.Write( ( short )0 );
            writer.Write( MaterialIndex );
            writer.Write( ( short ) 0 );
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

            writer.Write( Positions );
            writer.Align( 16 );

            writer.Write( Normals );
            writer.Align( 16 );
        }
    }
}