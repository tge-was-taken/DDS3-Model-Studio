using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.PS2.VIF
{
    public class VifPacket : VifCode
    {
        private static BitField sAddressBitField = new BitField( 0,  8 );
        private static BitField sUnusedBitField  = new BitField( 9,  13 );
        private static BitField sSignBitField    = new BitField( 14, 14 );
        private static BitField sFlagBitField    = new BitField( 15, 15 );
        private static BitField sElementFormatBitField = new BitField( 0, 1 );
        private static BitField sElementCountBitField  = new BitField( 2, 3 );
        private static BitField sCommandBitField       = new BitField( 4, 7 );

        public int Address
        {
            get => sAddressBitField.Unpack( mImmediate );
            protected set => sAddressBitField.Pack( ref mImmediate, ( ushort )( value ) );
        }

        public bool Sign
        {
            get => sSignBitField.Unpack( mImmediate ) != 0;
            protected set => sSignBitField.Pack( ref mImmediate, value ? ( ushort ) 1 : ( ushort ) 0 );
        }

        public bool Flag
        {
            get => sFlagBitField.Unpack( mImmediate ) != 0;
            protected set => sFlagBitField.Pack( ref mImmediate, value ? ( ushort )1 : ( ushort )0 );
        }

        public VifUnpackElementFormat ElementFormat
        {
            get => ( VifUnpackElementFormat )sElementFormatBitField.Unpack( mCommand );
            protected set => sElementFormatBitField.Pack( ref mCommand, (byte)value );
        }

        public int ElementCount
        {
            get => sElementCountBitField.Unpack( mCommand ) + 1;
            protected set
            {
                if ( value == 0 || value > 4 )
                    throw new ArgumentException( "Element count value must be higher than 0 and lower or equal to 4", nameof( value ) );

                sElementCountBitField.Pack( ref mCommand, ( byte )( value - 1 ) );
            }
        }

        public int ElementSize
        {
            get
            {
                switch ( ElementFormat )
                {
                    case VifUnpackElementFormat.Float:
                        return sizeof( float );
                    case VifUnpackElementFormat.Short:
                    case VifUnpackElementFormat.RGBA5A1:
                        return sizeof( short );
                    case VifUnpackElementFormat.Byte:
                        return sizeof( byte );
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override VifCommand Command => VifCommand.Unpack;

        public object Elements { get; protected set; }

        public float[] SingleArray
        {
            get => ( float[] ) Elements;
            set => Elements = value;
        }

        public Vector2[] Vector2Array
        {
            get => ( Vector2[] )Elements;
            set => Elements = value;
        }

        public Vector3[] Vector3Array
        {
            get => ( Vector3[] )Elements;
            set => Elements = value;
        }

        public Vector4[] Vector4Array
        {
            get => ( Vector4[] )Elements;
            set => Elements = value;
        }

        public short[] Int16Array
        {
            get => ( short[] )Elements;
            set => Elements = value;
        }

        public short[][] Int16Arrays
        {
            get => ( short[][] )Elements;
            set => Elements = value;
        }

        public ushort[] UInt16Array
        {
            get => ( ushort[] )Elements;
            set => Elements = value;
        }

        public ushort[][] UInt16Arrays
        {
            get => ( ushort[][] )Elements;
            set => Elements = value;
        }

        public byte[] ByteArray
        {
            get => ( byte[] )Elements;
            set => Elements = value;
        }

        public byte[][] ByteArrays
        {
            get => ( byte[][] )Elements;
            set => Elements = value;
        }

        public sbyte[] SByteArray
        {
            get => ( sbyte[] )Elements;
            set => Elements = value;
        }

        public sbyte[][] SByteArrays
        {
            get => ( sbyte[][] )Elements;
            set => Elements = value;
        }

        public VifPacket()
        {
        }

        public VifPacket( ushort immediate, byte count, byte command ) : base( immediate, count, command )
        {        
        }

        public VifPacket( int address, float[] elements, bool flag = false )
        {
            Initialize( address, true, flag, VifUnpackElementFormat.Float, 1, elements.Length, elements );
        }

        public VifPacket( int address, Vector2[] elements, bool flag = false )
        {
            Initialize( address, true, flag, VifUnpackElementFormat.Float, 2, elements.Length, elements );
        }

        public VifPacket( int address, Vector3[] elements, bool flag = false )
        {
            Initialize( address, true, flag, VifUnpackElementFormat.Float, 3, elements.Length, elements );
        }

        public VifPacket( int address, Vector4[] elements, bool flag = false )
        {
            Initialize( address, true, flag, VifUnpackElementFormat.Float, 4, elements.Length, elements );
        }

        public VifPacket( int address, short[] elements, bool flag = false )
        {
            Initialize( address, true, flag, VifUnpackElementFormat.Short, 1, elements.Length, elements );
        }

        public VifPacket( int address, short[][] elements, bool flag = false )
        {
            Initialize( address, true, flag, VifUnpackElementFormat.Short, elements[ 0 ].Length, elements.Length, elements );
        }

        public VifPacket( int address, ushort[] elements, bool flag = false )
        {
            Initialize( address, false, flag, VifUnpackElementFormat.Short, 1, elements.Length, elements );
        }

        public VifPacket( int address, ushort[][] elements, bool flag = false )
        {
            Initialize( address, false, flag, VifUnpackElementFormat.Short, elements[0].Length, elements.Length, elements );
        }

        public VifPacket( int address, byte[] elements, bool flag = false )
        {
            Initialize( address, false, flag, VifUnpackElementFormat.Byte, 1, elements.Length, elements );
        }

        public VifPacket( int address, byte[][] elements, bool flag = false )
        {
            Initialize( address, false, flag, VifUnpackElementFormat.Byte, elements[0].Length, elements.Length, elements );
        }

        public VifPacket( int address, sbyte[] elements, bool flag = false )
        {
            Initialize( address, true, flag, VifUnpackElementFormat.Byte, 1, elements.Length, elements );
        }

        public VifPacket( int address, sbyte[][] elements, bool flag = false )
        {
            Initialize( address, true, flag, VifUnpackElementFormat.Byte, elements[0].Length, elements.Length, elements );
        }

        private void Initialize( int address, bool sign, bool flag, VifUnpackElementFormat format, int elementCount, int count, object elements )
        {
            mCommand      = ( byte )Command;
            Address       = address;
            Sign          = sign;
            Flag          = flag;
            ElementFormat = format;
            ElementCount  = elementCount;
            Count         = count > byte.MaxValue ? throw new ArgumentException(nameof(count)) : ( byte )count;
            Elements      = elements;
        }

        protected override void ReadContent( EndianBinaryReader reader )
        {
            switch ( ElementFormat )
            {
                case VifUnpackElementFormat.Float:
                    switch ( ElementCount )
                    {
                        case 1:
                            SingleArray = reader.ReadSingleArray( Count );
                            break;
                        case 2:
                            Vector2Array = reader.ReadVector2Array( Count );
                            break;
                        case 3:
                            Vector3Array = reader.ReadVector3Array( Count );
                            break;
                        case 4:
                            Vector4Array = reader.ReadVector4Array( Count );
                            break;
                    }
                    break;
                case VifUnpackElementFormat.Short:
                case VifUnpackElementFormat.RGBA5A1:
                    switch ( ElementCount )
                    {
                        case 1:
                            if ( Sign )
                                Int16Array = reader.ReadInt16Array( Count );
                            else
                                UInt16Array = reader.ReadUInt16Array( Count );
                            break;

                        default:
                            if ( Sign )
                                Int16Arrays = new short[Count][];
                            else
                                UInt16Arrays = new ushort[Count][];

                            for ( int i = 0; i < Count; i++ )
                            {
                                if ( Sign )
                                    Int16Arrays[i] = reader.ReadInt16Array( ElementCount );
                                else
                                    UInt16Arrays[i] = reader.ReadUInt16Array( ElementCount );
                            }
                            break;
                    }
                    break;
                case VifUnpackElementFormat.Byte:
                    switch ( ElementCount )
                    {
                        case 1:
                            if ( Sign )
                                SByteArray = reader.ReadSBytes( Count );
                            else
                                ByteArray = reader.ReadBytes( Count );
                            break;

                        default:
                            if ( Sign )
                                SByteArrays = new sbyte[Count][];
                            else
                                ByteArrays = new byte[Count][];

                            for ( int i = 0; i < Count; i++ )
                            {
                                if ( Sign )
                                    SByteArrays[i] = reader.ReadSBytes( ElementCount );
                                else
                                    ByteArrays[i] = reader.ReadBytes( ElementCount );
                            }
                            break;
                    }
                    break;
            }
        }

        protected override void WriteContent( EndianBinaryWriter writer )
        {
            switch ( ElementFormat )
            {
                case VifUnpackElementFormat.Float:
                    switch ( ElementCount )
                    {
                        case 1:
                            writer.Write( SingleArray );
                            break;
                        case 2:
                            writer.Write( Vector2Array );
                            break;
                        case 3:
                            writer.Write( Vector3Array );
                            break;
                        case 4:
                            writer.Write( Vector4Array );
                            break;
                    }
                    break;
                case VifUnpackElementFormat.Short:
                case VifUnpackElementFormat.RGBA5A1:
                    switch ( ElementCount )
                    {
                        case 1:
                            if ( Sign )
                                writer.Write( Int16Array );
                            else
                                writer.Write( UInt16Array );
                            break;

                        default:
                            for ( int i = 0; i < Count; i++ )
                            {
                                if ( Sign )
                                    writer.Write( Int16Arrays[i] );
                                else
                                    writer.Write( UInt16Arrays[i] );
                            }
                            break;
                    }
                    break;
                case VifUnpackElementFormat.Byte:
                    switch ( ElementCount )
                    {
                        case 1:
                            if ( Sign )
                                writer.Write( SByteArray );
                            else
                                writer.Write( ByteArray );
                            break;

                        default:
                            for ( int i = 0; i < Count; i++ )
                            {
                                if ( Sign )
                                    writer.Write( SByteArrays[i] );
                                else
                                    writer.Write( ByteArrays[i] );
                            }
                            break;
                    }
                    break;
            }
        }
    }

    internal class VifValidationHelper
    {
        [Conditional("DEBUG")]
        public static void Ensure( VifCode code, int immediate, int count, VifCommand command )
        {
            if ( code.Immediate != immediate )
                throw new InvalidDataException( $"Vifcode immediate value is not {immediate}" );

            if ( code.Count != count )
                throw new InvalidDataException( $"Vifcode count value is not {count}" );

            if ( code.Command != command )
                throw new InvalidDataException( $"Vifcode command type is not {command}" );
        }

        [Conditional( "DEBUG" )]
        public static void Ensure( VifPacket packet, int? address, bool sign, bool flag, int? count, VifUnpackElementFormat format, int elementCount )
        {
            if ( address.HasValue && packet.Address != address.Value )
                throw new InvalidDataException( $"Packet address is not {address}" );

            if ( packet.Sign != sign )
                throw new InvalidDataException( $"Packet sign flag is not {sign}" );

            if ( packet.Flag != flag )
                throw new InvalidDataException( $"Packet flag is not {flag}" );

            if ( count.HasValue && packet.Count != count )
                throw new InvalidDataException( $"Packet count is not {count}" );

            if ( packet.ElementFormat != format )
                throw new InvalidDataException( $"Packet element format is not {format}" );

            if ( packet.ElementCount != elementCount )
                throw new InvalidDataException( $"Packet element count is not {elementCount}" );
        }

        [Conditional( "DEBUG" )]
        public static void ValidateActivateMicro( VifCode code  )
        {
            if ( code.Command != VifCommand.ActMicro || ( code.Immediate != 0x0C && code.Immediate != 0x10 ) )
                throw new InvalidDataException( "Invalid ActMicro Vifcode" );
        }
    }
}