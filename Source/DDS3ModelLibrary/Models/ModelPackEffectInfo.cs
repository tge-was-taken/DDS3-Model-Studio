using DDS3ModelLibrary.IO.Common;
using System.Collections.Generic;
using System.Diagnostics;

namespace DDS3ModelLibrary.Models
{
    public class ModelPackEffectInfo : IBinarySerializable
    {
        private const int HEADER_SIZE = 8;

        public int Id { get; set; }

        public List<short> Fields { get; private set; }

        // IBinarySerializable implementation
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            Id = reader.ReadInt32();
            var size = reader.ReadInt32();
            Trace.Assert((size % sizeof(short)) == 0, "ModelPackEffectInfo data size is not even");
            Fields = reader.ReadInt16List((size - HEADER_SIZE) / sizeof(short));
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            var size = HEADER_SIZE + Fields.Count * sizeof(short);
            writer.Write(Id);
            writer.Write(size);
            writer.Write(Fields);
        }
    }
}