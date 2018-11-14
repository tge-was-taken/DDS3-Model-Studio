using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.PS2.VIF
{
    /// <summary>
    /// Formats and builds vif code into a stream. Write only.
    /// </summary>
    public class VifCodeStreamBuilder : IBinarySerializable, IEnumerable<VifCode>
    {
        private readonly List<VifCode> mTags;

        public IReadOnlyList<VifCode> Tags => mTags;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the current emulated VU memory address.
        /// </summary>
        public int Address { get; set; }

        public VifCodeStreamBuilder()
        {
            mTags = new List<VifCode>();
        }


        /// <summary>
        /// Code 0x65. Used to unpack the header contents to VU memory (special configuration).
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public VifCodeStreamBuilder UnpackHeader( short value1, short value2, uint value3 )
        {
            mTags.Add( new VifPacket( 0, new[] { new[] { value1, value2, ( short ) value3, ( short )( value3 >> 16 ) } }, true ) );
            return this;
        }

        /// <summary>
        /// Code 0x65. Used to unpack the header contents to VU memory (special configuration).
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public VifCodeStreamBuilder UnpackHeader( short value1, short value2 )
        {
            mTags.Add( new VifPacket( 0xFF, new[] { new[] { value1, value2 } } ) );
            return this;
        }

        /// <summary>
        /// Code 0x6X. Decompresses data and writes to VU memory.
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public VifCodeStreamBuilder Unpack( dynamic elements )
        {
            Debug.Assert( Address % 8 == 0 );
            var packet       = new VifPacket( Address / 8, elements, false );
            //var unpackedSize = ( packet.Count * AlignmentHelper.Align( packet.ElementCount * packet.ElementSize, 16 ) ) * usedNodeCount;
            //mAddress += unpackedSize;

            Address += 0xC0;
            if ( Address > 0x240 )
                Address = 0;

            mTags.Add( packet );
            return this;
        }

        /// <summary>
        /// Code 0x6X. Decompresses data and writes to VU memory.
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public VifCodeStreamBuilder Unpack( int address, dynamic elements )
        {
            Address = address;
            Debug.Assert( Address % 8 == 0 );
            var packet = new VifPacket( Address / 8, elements, true );
            mTags.Add( packet );
            return this;
        }

        /// <summary>
        /// Code 0x14. Activates a microprogram.
        /// </summary>
        public VifCodeStreamBuilder ActivateMicro( ushort id )
        {
            mTags.Add( new VifCode( id, 0, ( byte ) VifCommand.ActMicro ) );
            return this;
        }

        /// <summary>
        /// Code 0x17. Executes a microprogram continuously.
        /// </summary>
        public VifCodeStreamBuilder ExecuteMicro()
        {
            mTags.Add( new VifCode( 0, 0, ( byte ) VifCommand.CntMicro ) );
            return this;
        }

        /// <summary>
        /// Code 0x10. Waits for the end of a microprogram.
        /// </summary>
        public VifCodeStreamBuilder FlushEnd()
        {
            mTags.Add( new VifCode( 0, 0, ( byte )VifCommand.FlushEnd ) );
            Address = 0; // TODO: verify
            return this;
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            throw new NotSupportedException();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            foreach ( var tag in Tags )
            {
                writer.WriteObject( tag );

                if ( tag.Command == VifCommand.FlushEnd )
                    writer.Align( 16 );
            }

            writer.Align( 16 );
        }

        public IEnumerator<VifCode> GetEnumerator()
        {
            return ( ( IEnumerable<VifCode> )mTags ).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ( ( IEnumerable<VifCode> )mTags ).GetEnumerator();
        }
    }
}