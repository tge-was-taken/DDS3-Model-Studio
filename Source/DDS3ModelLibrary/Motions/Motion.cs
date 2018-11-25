using System.Collections.Generic;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.Motions.Internal;

namespace DDS3ModelLibrary.Motions
{
    /// <summary>
    /// A motion represents the entire array of per-node controller tracks containing modifications of properties over time
    /// used in a single motion.
    /// </summary>
    public class Motion : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public int Duration { get; set; }

        public List<MotionController> Controllers { get; private set; }

        public Motion()
        {
            Controllers = new List<MotionController>();
        }

        internal Motion( MotionDefinition motion, List<MotionControllerDefinition> controllers ) : this()
        {
            Duration = motion.Duration;
            for ( int i = 0; i < controllers.Count; i++ )
            {
                // Remove useless controllers
                var controller = controllers[ i ];
                var keyframes = motion.Tracks[i].Keyframes;
                var isDummy = false;
                if ( keyframes.Count == 1 && keyframes[0].Time == 0 )
                {
                    var keyframe = keyframes[ 0 ];
                    switch ( keyframe )
                    {
                        case TranslationKeyframeSize12 translationKey:
                            isDummy = translationKey.Translation == Vector3.Zero;
                            break;

                        case RotationKeyframeSize8 rotationKey:
                            isDummy = rotationKey.Rotation == Quaternion.Identity;
                            break;

                        case ScaleKeyframeSize12 scaleKey:
                            isDummy = scaleKey.Scale == Vector3.One;
                            break;
                    }
                }

                if ( !isDummy )
                    Controllers.Add( new MotionController( controller, keyframes ) );
            }
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Duration = reader.ReadInt32();
            var controllerCount = reader.ReadInt32();
            Controllers = reader.ReadObjects<MotionController>( controllerCount );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteInt32( Duration );
            writer.WriteInt32( Controllers.Count );
            writer.WriteObjects( Controllers );
        }
    }
}