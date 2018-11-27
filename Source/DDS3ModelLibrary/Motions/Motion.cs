using System;
using System.Collections.Generic;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.Motions.Internal;

namespace DDS3ModelLibrary.Motions
{
    /// <summary>
    /// A motion represents a list of node controllers performing modifications to node properties over time.
    /// </summary>
    public class Motion : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public int Duration { get; set; }

        public List<NodeController> Controllers { get; private set; }

        public Motion()
        {
            Controllers = new List<NodeController>();
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
                    switch ( controller.Type )
                    {
                        //case ControllerType.Position:
                        //    switch ( keyframe )
                        //    {
                        //        case Vector3Key vector3Key:
                        //            isDummy = vector3Key.Value == Vector3.Zero;
                        //            break;
                        //    }
                        //    break;

                        //case ControllerType.Rotation:
                        //    switch ( keyframe )
                        //    {
                        //        case QuaternionKey quaternionKey:
                        //            isDummy = quaternionKey.Value == Quaternion.Identity;
                        //            break;
                        //    }
                        //    break;

                        case ControllerType.Scale:
                            switch ( keyframe )
                            {
                                case Vector3Key vector3Key:
                                    isDummy = vector3Key.Value == Vector3.One;
                                    break;
                            }
                            break;
                    }
                }

                if ( !isDummy )
                    Controllers.Add( new NodeController( controller, keyframes ) );
            }
        }

        public void ScaleDuration( float multiplier )
        {
            Duration = ( short )Math.Max( Duration * multiplier, 1 );
            foreach ( var controller in Controllers )
            {
                foreach ( var keyframe in controller.Keys )
                {
                    keyframe.Time = ( short )( keyframe.Time * multiplier );
                    if ( keyframe.Time > Duration )
                        Duration = keyframe.Time;
                }
            }
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Duration = reader.ReadInt32();
            var controllerCount = reader.ReadInt32();
            Controllers = reader.ReadObjects<NodeController>( controllerCount );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteInt32( Duration );
            writer.WriteInt32( Controllers.Count );
            writer.WriteObjects( Controllers );
        }
    }
}