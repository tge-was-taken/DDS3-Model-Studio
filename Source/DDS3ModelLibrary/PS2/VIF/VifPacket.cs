using System;
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

        public float[] Floats
        {
            get => ( float[] ) Elements;
            set => Elements = value;
        }

        public Vector2[] Vector2s
        {
            get => ( Vector2[] )Elements;
            set => Elements = value;
        }

        public Vector3[] Vector3s
        {
            get => ( Vector3[] )Elements;
            set => Elements = value;
        }

        public Vector4[] Vector4s
        {
            get => ( Vector4[] )Elements;
            set => Elements = value;
        }

        public short[] Shorts
        {
            get => ( short[] )Elements;
            set => Elements = value;
        }

        public short[][] ShortArrays
        {
            get => ( short[][] )Elements;
            set => Elements = value;
        }

        public ushort[] UnsignedShorts
        {
            get => ( ushort[] )Elements;
            set => Elements = value;
        }

        public ushort[][] UnsignedShortArrays
        {
            get => ( ushort[][] )Elements;
            set => Elements = value;
        }

        public byte[] Bytes
        {
            get => ( byte[] )Elements;
            set => Elements = value;
        }

        public byte[][] ByteArrays
        {
            get => ( byte[][] )Elements;
            set => Elements = value;
        }

        public sbyte[] SignedBytes
        {
            get => ( sbyte[] )Elements;
            set => Elements = value;
        }

        public sbyte[][] SignedByteArrays
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

        public void Ensure( int? address, bool sign, bool flag, int? count, VifUnpackElementFormat format, int elementCount  )
        {
            if ( address.HasValue && Address != address.Value )
                throw new UnexpectedDataException( $"Packet address is not {address}" );

            if ( Sign != sign )
                throw new UnexpectedDataException( $"Packet sign flag is not {sign}" );

            if ( Flag != flag )
                throw new UnexpectedDataException( $"Packet flag is not {flag}" );

            if ( count.HasValue && Count != count )
                throw new UnexpectedDataException( $"Packet count is not {count}" );

            if ( ElementFormat != format )
                throw new UnexpectedDataException( $"Packet element format is not {format}" );

            if ( ElementCount != elementCount )
                throw new UnexpectedDataException( $"Packet element count is not {elementCount}" );
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
                            Floats = reader.ReadSingles( Count );
                            break;
                        case 2:
                            Vector2s = reader.ReadVector2s( Count );
                            break;
                        case 3:
                            Vector3s = reader.ReadVector3s( Count );
                            break;
                        case 4:
                            Vector4s = reader.ReadVector4s( Count );
                            break;
                    }
                    break;
                case VifUnpackElementFormat.Short:
                case VifUnpackElementFormat.RGBA5A1:
                    switch ( ElementCount )
                    {
                        case 1:
                            if ( Sign )
                                Shorts = reader.ReadInt16Array( Count );
                            else
                                UnsignedShorts = reader.ReadUInt16s( Count );
                            break;

                        default:
                            if ( Sign )
                                ShortArrays = new short[Count][];
                            else
                                UnsignedShortArrays = new ushort[Count][];

                            for ( int i = 0; i < Count; i++ )
                            {
                                if ( Sign )
                                    ShortArrays[i] = reader.ReadInt16Array( ElementCount );
                                else
                                    UnsignedShortArrays[i] = reader.ReadUInt16s( ElementCount );
                            }
                            break;
                    }
                    break;
                case VifUnpackElementFormat.Byte:
                    switch ( ElementCount )
                    {
                        case 1:
                            if ( Sign )
                                SignedBytes = reader.ReadSBytes( Count );
                            else
                                Bytes = reader.ReadBytes( Count );
                            break;

                        default:
                            if ( Sign )
                                SignedByteArrays = new sbyte[Count][];
                            else
                                ByteArrays = new byte[Count][];

                            for ( int i = 0; i < Count; i++ )
                            {
                                if ( Sign )
                                    SignedByteArrays[i] = reader.ReadSBytes( ElementCount );
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
                            writer.Write( Floats );
                            break;
                        case 2:
                            writer.Write( Vector2s );
                            break;
                        case 3:
                            writer.Write( Vector3s );
                            break;
                        case 4:
                            writer.Write( Vector4s );
                            break;
                    }
                    break;
                case VifUnpackElementFormat.Short:
                case VifUnpackElementFormat.RGBA5A1:
                    switch ( ElementCount )
                    {
                        case 1:
                            if ( Sign )
                                writer.Write( Shorts );
                            else
                                writer.Write( UnsignedShorts );
                            break;

                        default:
                            for ( int i = 0; i < Count; i++ )
                            {
                                if ( Sign )
                                    writer.Write( ShortArrays[i] );
                                else
                                    writer.Write( UnsignedShortArrays[i] );
                            }
                            break;
                    }
                    break;
                case VifUnpackElementFormat.Byte:
                    switch ( ElementCount )
                    {
                        case 1:
                            if ( Sign )
                                writer.Write( SignedBytes );
                            else
                                writer.Write( Bytes );
                            break;

                        default:
                            for ( int i = 0; i < Count; i++ )
                            {
                                if ( Sign )
                                    writer.Write( SignedByteArrays[i] );
                                else
                                    writer.Write( ByteArrays[i] );
                            }
                            break;
                    }
                    break;
            }
        }
    }
}