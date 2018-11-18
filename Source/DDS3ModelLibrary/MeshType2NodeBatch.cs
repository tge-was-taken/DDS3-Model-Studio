using System.Diagnostics;
using System.Linq;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.Primitives;
using DDS3ModelLibrary.PS2.VIF;

namespace DDS3ModelLibrary
{
    public class MeshType2NodeBatch : IBinarySerializable
    {
        private int mIndicesAddress, mPositionsAddress, mNormalsAddress, mTexCoordsAddress, mColorsAddress;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public short NodeIndex { get; set; }

        public short VertexCount => ( short )( Positions?.Length ?? 0 );

        public MeshFlags Flags { get; set; }

        public Vector4[] Positions { get; set; }

        public Vector3[] Normals { get; set; }

        public MeshType1BatchRenderMode RenderMode { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            var ctx = ( MeshType2NodeBatchContext ) context;
            NodeIndex = ctx.NodeIndex;

            // Read header
            var headerPacket = reader.ReadObject<VifPacket>();
            headerPacket.Ensure( 0, true, true, 1, VifUnpackElementFormat.Short, 4 );
            var triangleCount = headerPacket.ShortArrays[ 0 ][ 0 ];
            var vertexCount = headerPacket.ShortArrays[ 0 ][ 1 ];
            var flags         = Flags = ( MeshFlags )( ( ushort )headerPacket.ShortArrays[0][2] | ( ushort )headerPacket.ShortArrays[0][3] << 16 );

            if ( ctx.Last )
            {
                // Read triangles
                var indicesPacket = reader.ReadObject<VifPacket>();
                indicesPacket.Ensure( 1, true, true, triangleCount, VifUnpackElementFormat.Byte, 4 );
                ctx.Triangles = indicesPacket.SignedByteArrays.Select( x =>
                {
                    if ( x[3] != 0 )
                        throw new UnexpectedDataException();

                    return new Triangle( ( byte )x[0], ( byte )x[1], ( byte )x[2] );
                } ).ToArray();
                mIndicesAddress = indicesPacket.Address * 8;
            }

            var positionsPacket = reader.ReadObject<VifPacket>();
            positionsPacket.Ensure( !ctx.Last ? 1 : ( int? ) null, true, true, vertexCount, VifUnpackElementFormat.Float, 4 );
            Positions = positionsPacket.Vector4s;
            mPositionsAddress = positionsPacket.Address * 8;

            // Read normals
            if ( flags.HasFlag( MeshFlags.Normal ) )
            {
                var normalsPacket = reader.ReadObject<VifPacket>();
                normalsPacket.Ensure( null, true, true, vertexCount, VifUnpackElementFormat.Float, 3 );
                Normals = normalsPacket.Vector3s;
                mNormalsAddress = normalsPacket.Address * 8;
            }

            if ( ctx.Last )
            {
                if ( flags.HasFlag( MeshFlags.TexCoord ) )
                {
                    var texCoordsPacket = reader.ReadObject<VifPacket>();
                    mTexCoordsAddress = texCoordsPacket.Address * 8;

                    // Read texture coords
                    if ( !flags.HasFlag( MeshFlags.TexCoord2 ) )
                    {
                        texCoordsPacket.Ensure( null, true, true, vertexCount, VifUnpackElementFormat.Float, 2 );
                        ctx.TexCoords[0] = texCoordsPacket.Vector2s;
                    }
                    else
                    {
                        texCoordsPacket.Ensure( null, true, true, vertexCount, VifUnpackElementFormat.Float, 4 );
                        ctx.TexCoords[0] = texCoordsPacket.Vector4s.Select( x => new Vector2( x.X, x.Y ) ).ToArray();
                        ctx.TexCoords[1] = texCoordsPacket.Vector4s.Select( x => new Vector2( x.Z, x.W ) ).ToArray();
                    }
                }

                if ( flags.HasFlag( MeshFlags.Color ) )
                {
                    // Read colors
                    var colorsPacket = reader.ReadObject<VifPacket>();
                    colorsPacket.Ensure( null, true, true, vertexCount, VifUnpackElementFormat.Byte, 4 );
                    ctx.Colors = colorsPacket.SignedByteArrays.Select( x => new Color( ( byte )x[0], ( byte )x[1], ( byte )x[2], ( byte )x[3] ) )
                                         .ToArray();
                    mColorsAddress = colorsPacket.Address * 8;
                }
            }

            // Read activate command
            var activateCode = reader.ReadObject<VifCode>();
            if ( activateCode.Command != VifCommand.ActMicro || ( activateCode.Immediate != 0x0C && activateCode.Immediate != 0x10 ) )
                throw new UnexpectedDataException();

            // Not sure if this makes any difference yet
            RenderMode = activateCode.Immediate == 0x0C ? MeshType1BatchRenderMode.Mode1 : MeshType1BatchRenderMode.Mode2;

            Debug.Assert( Flags == flags );
        }

        private void Write( MeshType2NodeBatchContext context )
        {
            var vif = context.Vif;

            // Header
            vif.UnpackHeader( ( short ) context.Triangles.Length, VertexCount, ( uint ) Flags );

            var nextAddress = 8;

            if ( context.Last )
            {
                // Triangles
                Debug.Assert( nextAddress == mIndicesAddress );
                vif.Unpack( nextAddress, context.Triangles.Select( x => new sbyte[] { ( sbyte )x.A, ( sbyte )x.B, ( sbyte )x.C, 0 } ).ToArray() );
                nextAddress = AlignmentHelper.Align( nextAddress + ( ( context.Triangles.Length * 4 ) * 2 ), 8 );
            }

            // Positions
            Debug.Assert( nextAddress == mPositionsAddress );
            vif.Unpack( nextAddress, Positions );
            var effectiveVertexSize = ( int )( ( VertexCount * 12 ) / 1.5f );
            nextAddress = AlignmentHelper.Align( nextAddress + effectiveVertexSize, 8 );

            if ( Flags.HasFlag( MeshFlags.Normal ) )
            {
                // Normals
                Debug.Assert( nextAddress == mNormalsAddress );
                vif.Unpack( nextAddress, Normals );
                nextAddress = AlignmentHelper.Align( nextAddress + effectiveVertexSize, 8 );
            }

            if ( context.Last )
            {
                if ( Flags.HasFlag( MeshFlags.TexCoord ) )
                {
                    Debug.Assert( nextAddress == mTexCoordsAddress );
                    if ( !Flags.HasFlag( MeshFlags.TexCoord2 ) )
                    {
                        // Texcoord 1
                        vif.Unpack( nextAddress, context.TexCoords[0] );
                    }
                    else
                    {
                        // Texcoord 1 & 2
                        var mergedTexCoords = new Vector4[VertexCount];
                        for ( int i = 0; i < mergedTexCoords.Length; i++ )
                        {
                            mergedTexCoords[i].X = context.TexCoords[0][i].X;
                            mergedTexCoords[i].Y = context.TexCoords[0][i].Y;
                            mergedTexCoords[i].Z = context.TexCoords[1][i].X;
                            mergedTexCoords[i].W = context.TexCoords[1][i].Y;
                        }

                        vif.Unpack( nextAddress, mergedTexCoords );
                    }

                    nextAddress = AlignmentHelper.Align( nextAddress + ( context.TexCoords[0].Length * 8 ), 8 );
                }

                if ( Flags.HasFlag( MeshFlags.Color ) )
                {
                    Debug.Assert( nextAddress == mColorsAddress );
                    vif.Unpack( nextAddress,
                                context.Colors.Select( x => new[] { ( sbyte ) x.R, ( sbyte ) x.G, ( sbyte ) x.B, ( sbyte ) x.A } ).ToArray() );
                }
            }

            vif.ActivateMicro( ( ushort ) ( RenderMode == MeshType1BatchRenderMode.Mode1 ? 0x0C : 0x10 ) );

        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            Write( ( MeshType2NodeBatchContext ) context );
        }
    }
}