using System;
using System.IO;
using DDS3ModelLibrary.IO;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary
{
    public enum ResourceFileType : byte
    {
        Default = 1,
        Texture = 2,
        Model = 6,
        MotionPack = 8,
        TexturePack = 9,
        ModelPackEnd = 0xFF,
    }

    public enum ResourceIdentifier : uint
    {
        ModelPackInfo = 0x30424950,
        TexturePack = 0x30505854,
        Texture = 0x30584D54,
        Model = 0x3030444D,
        AnimationPack = 0x3030544D,
        ModelPackEnd = 0x30444E45,
        Particle = 0x00503344,
        Video = 0x00555049,
    }

    public class ResourceDescriptor
    {
        public ResourceFileType FileType { get; }

        public ResourceIdentifier Identifier { get; }

        public ResourceDescriptor( ResourceFileType fileType, ResourceIdentifier identifier )
        {
            FileType = fileType;
            Identifier = identifier;
        }
    }

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

        protected virtual void Write( EndianBinaryWriter writer, object context = null )
        {
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
            writer.WriteAlignmentPadding( 64 );
            writer.PopBaseOffset();
        }

        internal abstract void ReadContent( EndianBinaryReader reader, ResourceHeader header );

        internal abstract void WriteContent( EndianBinaryWriter writer, object context );

        protected void Read( EndianBinaryReader reader )
        {
            Read( reader, null );
        }

        private void Read( EndianBinaryReader reader, ResourceHeader header )
        {
            var start = reader.Position;

            if ( header == null )
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

            reader.PushBaseOffset( start );

            var end = start + header.FileSize;
            UserId = header.UserId;
            ReadContent( reader, header );
            reader.SeekBegin( end );

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
            Read( reader, context as ResourceHeader );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            Write( writer, context );
        }
    }
}