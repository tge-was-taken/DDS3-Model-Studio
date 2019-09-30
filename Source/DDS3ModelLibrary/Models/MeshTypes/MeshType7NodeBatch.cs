using System.Diagnostics;
using System.IO;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.PS2.VIF;

namespace DDS3ModelLibrary.Models
{
    public class MeshType7NodeBatch : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public short NodeIndex { get; set; }

        public short VertexCount => ( short ) ( Positions?.Length ?? 0 );

        public Vector4[] Positions { get; set; }
    
        public Vector3[] Normals { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            NodeIndex = ( short )context;

            var positionsPacket = reader.ReadObject<VifPacket>();
            VifValidationHelper.Ensure( positionsPacket, null, true, false, null, VifUnpackElementFormat.Float, 4 );
            Positions = positionsPacket.Vector4Array;

            // @NOTE(TGE): Not a single mesh type 7 mesh has the normals flag not set so I'm not sure if the flag is checked for.
            var normalsPacket = reader.ReadObject<VifPacket>();
            VifValidationHelper.Ensure( normalsPacket, null, true, false, VertexCount, VifUnpackElementFormat.Float, 3 );
            Normals = normalsPacket.Vector3Array;

            var cmdTag = reader.ReadObject<VifCode>();
            if ( cmdTag.Command == VifCommand.ActMicro )
            {
                VifValidationHelper.Ensure( cmdTag, 0x14, 0, VifCommand.ActMicro );
            }
            else if ( cmdTag.Command != VifCommand.CntMicro )
            {
                throw new InvalidDataException( "Expected activate or execute microprogram vif command" );
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
                        Positions = packet.Vector4Array;
                    }
                    else if ( packet.ElementFormat == VifUnpackElementFormat.Float && packet.ElementCount == 3 )
                    {
                        Normals = packet.Vector3Array;
                    }
                    else
                    {
                        throw new InvalidDataException( "Unexpected VIF packet" );
                    }
                }
                else
                {
                    var command = ( VifCommand )tag.Command;
                    if ( command != VifCommand.ActMicro && command != VifCommand.CntMicro )
                        throw new InvalidDataException( "Unexpected VIF tag command" );

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