using System.IO;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.PS2.VIF
{
    public class VifCode : IBinarySerializable
    {
        protected byte mCommand;
        protected ushort mImmediate;
        protected byte mCount;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public ushort Immediate
        {
            get => mImmediate;
            set => mImmediate = value;
        }

        public byte Count
        {
            get => mCount;
            set => mCount = value;
        }

        public virtual VifCommand Command => ( VifCommand )mCommand;

        public VifCode()
        {
        }

        public VifCode( ushort immediate, byte count, byte command )
        {
            Immediate = immediate;
            Count = count;
            mCommand = command;
        }

        public void Ensure( int immediate, int count, VifCommand command )
        {
            if ( Immediate != immediate )
                throw new InvalidDataException( $"Vifcode immediate value is not {immediate}" );

            if ( Count != count )
                throw new InvalidDataException( $"Vifcode count value is not {count}" );

            if ( Command != command )
                throw new InvalidDataException( $"Vifcode command type is not {command}" );
        }

        private void Read( EndianBinaryReader reader, VifTag tag )
        {
            if ( tag == null )
                tag = reader.ReadObject<VifTag>();

            mCommand = ( byte )tag.Command;
            mImmediate = tag.Immediate;
            mCount = tag.Count;
            ReadContent( reader );
        }

        protected virtual void ReadContent( EndianBinaryReader reader ) { }
        protected virtual void WriteContent( EndianBinaryWriter writer ) { }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Read( reader, context as VifTag );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.Write( mImmediate );
            writer.Write( mCount );
            writer.Write( mCommand );
            WriteContent( writer );
        }
    }
}
