using System.Diagnostics;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.PS2.VIF;

namespace DDS3ModelLibrary
{
    public class MeshNodeBatchType7 : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public short NodeId { get; set; }

        public int VertexCount => Positions?.Length ?? 0;

        public Vector4[] Positions { get; set; }
    
        public Vector3[] Normals { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            NodeId = ( short )context;

            var positionsPacket = reader.ReadObject<VifPacket>();
            positionsPacket.Ensure( null, true, false, null, VifUnpackElementFormat.Float, 4 );
            Positions = positionsPacket.Vector4s;

            var normalsPacket = reader.ReadObject<VifPacket>();
            normalsPacket.Ensure( null, true, false, VertexCount, VifUnpackElementFormat.Float, 3 );
            Normals = normalsPacket.Vector3s;

            var cmdTag = reader.ReadObject<VifCode>();
            if ( cmdTag.Command == VifCommand.ActMicro )
            {
                cmdTag.Ensure( 0x14, 0, VifCommand.ActMicro );
            }
            else if ( cmdTag.Command != VifCommand.CntMicro )
            {
                throw new UnexpectedDataException( "Expected activate or execute microprogram vif command" );
            }
        }

        private void ReadOld( EndianBinaryReader reader )
        {
            while ( true )
            {
                var tag = reader.ReadObject<VifTag>();

                if ( ( tag.Command > 0x20 && tag.Command < 0x60 ) || tag.Command > 0x70 )
                {
                    Debug.WriteLine( $"Hit invalid vif cmd: {tag.Command}" );
                    reader.SeekCurrent( -VifTag.SIZE );
                    reader.Align( 16 );
                    break;
                }

                if ( ( VifCommand )( tag.Command & 0xF0 ) == VifCommand.Unpack )
                {
                    var packet = reader.ReadObject<VifPacket>( tag );

                    // TODO: use flags for this
                    if ( packet.ElementFormat == VifUnpackElementFormat.Float && packet.ElementCount == 4 )
                    {
                        Positions = packet.Vector4s;
                    }
                    else if ( packet.ElementFormat == VifUnpackElementFormat.Float && packet.ElementCount == 3 )
                    {
                        Normals = packet.Vector3s;
                    }
                    else
                    {
                        throw new UnexpectedDataException( "Unexpected VIF packet" );
                    }
                }
                else
                {
                    var command = ( VifCommand )tag.Command;
                    if ( command != VifCommand.ActMicro && command != VifCommand.CntMicro )
                        throw new UnexpectedDataException( "Unexpected VIF tag command" );

                    break;
                }
            }
        }

        private void Write( EndianBinaryWriter writer, VifCodeStreamBuilder vif, bool first )
        {
            vif.Unpack( Positions );
            vif.Unpack( Normals );

            if ( first )
                vif.ActivateMicro( 0x14 );
            else
                vif.ExecuteMicro();

        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            var contextTuple = ((VifCodeStreamBuilder, bool))context;
            Write( writer, contextTuple.Item1, contextTuple.Item2);
        }
    }
}