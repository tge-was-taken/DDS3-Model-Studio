using System;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.IO
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
                Write( writer, new IOContext( null, false, null ) );
            }
        }

        protected virtual void Write( EndianBinaryWriter writer, IOContext context = null )
        {
            if ( context == null )
                context = new IOContext();

            if ( context.IsFieldObject )
            {
                WriteContent( writer, context );
                return;
            }

            // Skip header
            writer.PushBaseOffset();
            var start = writer.Position;
            writer.SeekCurrent( 16 );

            // Write resource content
            WriteContent( writer, context );

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

        internal abstract void ReadContent( EndianBinaryReader reader, IOContext context );

        internal abstract void WriteContent( EndianBinaryWriter writer, IOContext context );

        protected void Read( EndianBinaryReader reader )
        {
            Read( reader, new IOContext( null, false, null ) );
        }

        private void Read( EndianBinaryReader reader, IOContext context )
        {
            var start = reader.Position;

            if ( context.Header == null && !context.IsFieldObject )
            {
                // Read the resource header and make sure that we're reading the right type of resource
                context.Header = reader.ReadObject<ResourceHeader>();
                if ( context.Header.FileType != ResourceDescriptor.FileType ||
                     context.Header.Identifier != ResourceDescriptor.Identifier )
                    throw new InvalidOperationException( "Resource header does not match resource type" );
            }
            else
            {
                // Account for the size of the header that was read
                start -= ResourceHeader.SIZE;
            }

            if ( !context.IsFieldObject )
                reader.PushBaseOffset( start );

            var end = 0L;
            if ( context.Header != null )
            {
                end = start + context.Header.FileSize;
                UserId = context.Header.UserId;
            }

            ReadContent( reader, context );

            // Some files have broken offsets & filesize in their texture pack (f021_aljira.PB)
            if ( context.Header != null && context.Header.Identifier != ResourceIdentifier.TexturePack )
                reader.SeekBegin( end );

            if ( !context.IsFieldObject )
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
            Read( reader, ( IOContext ) ( context ?? new IOContext() ) );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            Write( writer, ( IOContext )( context ?? new IOContext() ) );
        }

        public class IOContext
        {
            public ResourceHeader Header;
            public readonly bool IsFieldObject;
            public readonly object Context;

            public IOContext() { }

            public IOContext( ResourceHeader header, bool isFieldObject, object context )
            {
                Header = header;
                IsFieldObject = isFieldObject;
                Context = context;
            }

            public IOContext( object context ) => Context = context;

            public IOContext( bool isFieldObject ) => IsFieldObject = isFieldObject;
        }
    }
}