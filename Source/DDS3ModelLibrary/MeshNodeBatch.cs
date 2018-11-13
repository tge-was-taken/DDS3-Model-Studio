using System.Diagnostics;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.PS2.VIF;

namespace DDS3ModelLibrary
{
    public class MeshNodeBatch : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public short NodeId { get; set; }

        public int VertexCount => Positions.Length;

        public int PositionsAddress;
        public Vector4[] Positions { get; private set; }

        public int NormalsAddress;
        public Vector3[] Normals { get; private set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            NodeId = ( short )context;

            while ( true )
            {
                var header = reader.ReadObject<VifCodeHeader>();

                if ( ( header.Command > 0x20 && header.Command < 0x60 ) || header.Command > 0x70 )
                {
                    Debug.WriteLine( $"Hit invalid vif cmd: {header.Command}" );
                    reader.SeekCurrent( -VifCodeHeader.SIZE );
                    reader.Align( 16 );
                    break;
                }

                if ( ( VifCommand ) ( header.Command & 0xF0 ) == VifCommand.Unpack )
                {
                    var packet = reader.ReadObject<VifPacket>( header );

                    // TODO: use flags for this
                    if ( packet.ElementFormat == VifUnpackElementFormat.Float && packet.ElementCount == 4 )
                    {
                        PositionsAddress = packet.Address * 8;
                        Positions = packet.Vector4s;
                    }
                    else if ( packet.ElementFormat == VifUnpackElementFormat.Float && packet.ElementCount == 3 )
                    {
                        NormalsAddress = packet.Address * 8;
                        Normals = packet.Vector3s;
                    }
                    else
                    {
                        throw new UnexpectedDataException( "Unexpected VIF packet" );
                    }
                }
                else
                {
                    var command = ( VifCommand ) header.Command;
                    if ( command != VifCommand.ActMicro && command != VifCommand.CntMicro )
                        throw new UnexpectedDataException( "Unexpected VIF tag command" );

                    break;
                }
            }
        }

        private void Write( EndianBinaryWriter writer, VifCommandBuffer vifCmd, bool first )
        {
            vifCmd.Unpack( Positions );

            if ( Normals != null )
            {
                vifCmd.Unpack( Normals );
            }

            // TODO: verify
            if ( first )
                vifCmd.ActivateMicro();
            else
                vifCmd.ExecuteMicro();

        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            var contextTuple = ((VifCommandBuffer, bool))context;
            Write( writer, contextTuple.Item1, contextTuple.Item2);
        }
    }
}