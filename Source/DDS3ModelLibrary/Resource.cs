using System;
using System.IO;
using DDS3ModelLibrary.IO;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public abstract class Resource : IBinarySerializable
    {
        public abstract ResourceDescriptor ResourceDescriptor { get; }

        public ushort UserId { get; set; }

        protected Resource( ushort userId )
        {
            UserId = userId;
        }

        protected Resource()
        {
        }

        public static Resource Load( string path )
        {
            //using ( var reader = new EndianBinaryReader( path, Endianness.Little ) )
            //{
            //    var resourceHeader = reader.ReadObject<ResourceHeader>();

            //    switch ( resourceHeader.Identifier )
            //    {
            //        case ResourceIdentifier.ModelPackInfo:
            //            return new ModelPack( resourceHeader.UserId );

            //        case ResourceIdentifier.Model:
            //            return new Model( resourceHeader.UserId );

            //        case ResourceIdentifier.TexturePack:
            //            {
            //                var resourceExt = Path.GetExtension( path );
            //                if ( resourceExt?.ToLowerInvariant() == ".pb" )
            //                {
            //                    // PB file with missing header
            //                    return new ModelPack( resourceHeader.UserId );
            //                }
            //                else
            //                {
            //                    return new TexturePack( resourceHeader.UserId );
            //                }
            //            }
            //    }
            //}
            return null;
        }

        public void Save( string path )
        {
            using ( var writer = new EndianBinaryWriter( path, Endianness.Little ) )
            {
                Write( writer );
            }
        }

        protected virtual void Write( EndianBinaryWriter writer, bool isFieldObj = false )
        {
            if ( isFieldObj )
            {
                WriteContent( writer, isFieldObj );
                return;
            }

            // Skip header
            writer.PushBaseOffset();
            var start = writer.Position;
            writer.SeekCurrent( 16 );

            // Write resource content
            WriteContent( writer, isFieldObj );

            // Calculate content size 
            var end = writer.Position;
            var size = end - start;

            // Seek back to write the header
            writer.SeekBegin( start );
            writer.Write( ( byte ) ResourceDescriptor.FileType );
            writer.Write( ( byte ) 0 );
            writer.Write( UserId );
            writer.Write( ( uint ) size );
            writer.Write( ( uint )ResourceDescriptor.Identifier );
            writer.Write( 0 );

            // Seek back to the end and align to 64 bytes
            writer.SeekBegin( end );
            writer.Align( 64 );
            writer.PopBaseOffset();
        }

        internal abstract void ReadContent( EndianBinaryReader reader, ResourceHeader header );

        internal abstract void WriteContent( EndianBinaryWriter writer, object context );

        protected void Read( EndianBinaryReader reader )
        {
            Read( reader, null, false );
        }

        private void Read( EndianBinaryReader reader, ResourceHeader header, bool isFieldObj )
        {
            var start = reader.Position;

            if ( header == null && !isFieldObj )
            {
                // Read the resource header and make sure that we're reading the right type of resource
                header = reader.ReadObject<ResourceHeader>();
                if ( header.FileType != ResourceDescriptor.FileType ||
                     header.Identifier != ResourceDescriptor.Identifier )
                    throw new InvalidOperationException( "Resource header does not match resource type" );
            }
            else
            {
                // Account for the size of the header that was read
                start -= ResourceHeader.SIZE;
            }

            if ( !isFieldObj )
                reader.PushBaseOffset( start );

            var end = 0L;
            if ( header != null )
            {
                end = start + header.FileSize;
                UserId = header.UserId;
            }

            ReadContent( reader, header );

            // Some files have broken offsets & filesize in their texture pack (f021_aljira.PB)
            if ( header != null && header.Identifier != ResourceIdentifier.TexturePack )
                reader.SeekBegin( end );

            if ( !isFieldObj )
                reader.PopBaseOffset();
        }

        // IBinarySerializable
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"><see cref="ResourceHeader"/>, if null then it will be read from the stream.</param>
        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            if ( context != null )
            {
                ( var header, var isFieldObj ) = ( (ResourceHeader, bool) ) context;
                Read( reader, header, isFieldObj );
            }
            else
            {
                Read( reader, null, false );
            }
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            Write( writer, ( bool ) context );
        }
    }
}