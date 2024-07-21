using DDS3ModelLibrary.IO.Common;
using System.Collections.Generic;

namespace DDS3ModelLibrary.Motions.Internal
{
    internal class MotionDefinition : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public int Duration { get; set; } 

        public List<KeyframeTrack> Tracks { get; }

        public MotionDefinition()
        {
            Tracks = new List<KeyframeTrack>();
        }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            var controllerDefs = (List<MotionControllerDefinition>)context;

            Duration = reader.ReadInt32();
            foreach (var controllerDef in controllerDefs)
                Tracks.Add(reader.ReadObject<KeyframeTrack>(controllerDef.Type));
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            writer.WriteInt32(Duration);
            writer.WriteObjects(Tracks);
        }
    }
}