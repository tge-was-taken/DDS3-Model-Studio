using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                throw new UnexpectedDataException( $"Vifcode immediate value is not {immediate}" );

            if ( Count != count )
                throw new UnexpectedDataException( $"Vifcode count value is not {count}" );

            if ( Command != command )
                throw new UnexpectedDataException( $"Vifcode command type is not {command}" );
        }

        private void Read( EndianBinaryReader reader, VifCodeHeader header )
        {
            if ( header == null )
                header = reader.ReadObject<VifCodeHeader>();

            mCommand = ( byte )header.Command;
            mImmediate = header.Immediate;
            mCount = header.Count;
            ReadContent( reader );
        }

        protected virtual void ReadContent( EndianBinaryReader reader ) { }
        protected virtual void WriteContent( EndianBinaryWriter writer ) { }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Read( reader, context as VifCodeHeader );
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
