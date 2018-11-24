﻿using System.IO;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.IO
{
    public abstract class AbstractResource<TIOContext> : IBinarySerializable where TIOContext : class, new()
    {
        public void Save( string filePath )
        {
            using ( var writer = new EndianBinaryWriter( new MemoryStream(), Endianness.Little ) )
            {
                Write( writer );
                using ( var fileStream = File.Create( filePath ) )
                {
                    writer.BaseStream.Position = 0;
                    writer.BaseStream.CopyTo( fileStream );
                }
            }
        }

        public void Save( Stream stream, bool leaveOpen = true )
        {
            using ( var writer = new EndianBinaryWriter( stream, leaveOpen, Endianness.Little ) )
                Write( writer );
        }

        public MemoryStream Save()
        {
            var stream = new MemoryStream();
            Save( stream );
            stream.Position = 0;
            return stream;
        }

        protected abstract void Write( EndianBinaryWriter writer, TIOContext context = null );

        protected abstract void Read( EndianBinaryReader reader, TIOContext context = null );

        // IBinarySerializable
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context ) => Read( reader, ( TIOContext ) ( context ?? new TIOContext() ) );

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context ) => Write( writer, ( TIOContext ) ( context ?? new TIOContext() ) );
    }
}