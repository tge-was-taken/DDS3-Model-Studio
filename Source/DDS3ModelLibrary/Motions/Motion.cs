using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Motions
{
    /// <summary>
    /// A motion represents the entire array of per-node controller tracks containing modifications of properties over time
    /// used in a single motion.
    /// </summary>
    public class Motion : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public int FrameCount { get; set; }

        public List<KeyframeTrack> Tracks { get; }

        public Motion()
        {
            Tracks = new List<KeyframeTrack>();
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            var controllers = ( List<MotionController> )context;

            FrameCount = reader.ReadInt32();
            foreach ( var controller in controllers )
                Tracks.Add( reader.ReadObject<KeyframeTrack>( controller ) );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteInt32( FrameCount );
            writer.WriteObjects( Tracks );
        }
    }
}