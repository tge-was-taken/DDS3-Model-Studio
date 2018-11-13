using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.PS2.VIF;

namespace DDS3ModelLibrary
{
    public class VifCommandBuffer : IBinarySerializable, IEnumerable<VifCode>
    {
        private readonly List<VifCode> mTags;

        public IReadOnlyList<VifCode> Tags => mTags;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the current emulated VU memory address.
        /// </summary>
        public int Address { get; set; }

        public VifCommandBuffer()
        {
            mTags = new List<VifCode>();
        }

        /// <summary>
        /// Code 0x65. Used to unpack the header contents to VU memory (special configuration).
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public VifCommandBuffer UnpackHeader( short value1, short value2 )
        {
            mTags.Add( new VifPacket( 0xFF, new[] { new[] { value1, value2 } } ) );
            return this;
        }

        /// <summary>
        /// Code 0x6X. Decompresses data and writes to VU memory.
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public VifCommandBuffer Unpack( dynamic elements )
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
        /// Code 0x14. Activates a microprogram.
        /// </summary>
        public VifCommandBuffer ActivateMicro()
        {
            mTags.Add( new VifCode( ( ushort ) VifCommand.ActMicro, 0, ( byte ) VifCommand.ActMicro ) );
            return this;
        }

        /// <summary>
        /// Code 0x17. Executes a microprogram continuously.
        /// </summary>
        public VifCommandBuffer ExecuteMicro()
        {
            mTags.Add( new VifCode( 0, 0, ( byte ) VifCommand.CntMicro ) );
            return this;
        }

        /// <summary>
        /// Code 0x10. Waits for the end of a microprogram.
        /// </summary>
        public VifCommandBuffer FlushEnd()
        {
            mTags.Add( new VifCode( 0, 0, ( byte )VifCommand.FlushEnd ) );
            Address = 0; // TODO: verify
            return this;
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            throw new NotImplementedException();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            foreach ( var tag in Tags )
            {
                writer.WriteObject( tag );

                if ( tag.Command == VifCommand.FlushEnd )
                    writer.WriteAlignmentPadding( 16 );
            }

            writer.WriteAlignmentPadding( 16 );
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