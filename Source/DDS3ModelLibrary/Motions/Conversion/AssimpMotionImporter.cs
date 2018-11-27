using System.Diagnostics;
using System.Linq;
using System.Numerics;
using DDS3ModelLibrary.Models.Utilities;

namespace DDS3ModelLibrary.Motions.Conversion
{
    public partial class AssimpMotionImporter : MotionImporter<AssimpMotionImporter, AssimpMotionImporter.Config>
    {
        public override Motion Import( string filepath, Config config )
        {
            var aiScene = AssimpHelper.ImportScene( filepath );
            var aiAnimation = aiScene.Animations.FirstOrDefault();
            if ( aiAnimation == null )
                return null;

            var motion = new Motion { Duration = ConvertTime( aiAnimation.DurationInTicks, aiAnimation.TicksPerSecond ) };
            foreach ( var aiChannel in aiAnimation.NodeAnimationChannels )
            {
                var nodeName = aiChannel.NodeName;
                var nodeIndex = ( short ) ( config.NodeIndexResolver?.Invoke( nodeName ) ?? FindNodeIndex( aiScene.RootNode, nodeName ) );
                Debug.Assert( nodeIndex != -1 );

                if ( aiChannel.HasPositionKeys )
                {
                    var controller = new NodeController( ControllerType.Position, nodeIndex, aiChannel.NodeName );
                    foreach ( var aiKey in aiChannel.PositionKeys )
                    {
                        controller.Keys.Add( new Vector3Key
                        {
                            Time = ConvertTime( aiKey.Time, aiAnimation.TicksPerSecond ),
                            Value = aiKey.Value.FromAssimp()
                        } );
                    }

                    motion.Controllers.Add( controller );
                }

                if ( aiChannel.HasRotationKeys )
                {
                    var controller = new NodeController( ControllerType.Rotation, nodeIndex, aiChannel.NodeName );
                    foreach ( var aiKey in aiChannel.RotationKeys )
                    {
                        controller.Keys.Add( new QuaternionKey
                        {
                            Time = ConvertTime( aiKey.Time, aiAnimation.TicksPerSecond ),
                            Value = Quaternion.Inverse( aiKey.Value.FromAssimp() )
                        } );
                    }

                    motion.Controllers.Add( controller );
                }

                if ( aiChannel.HasScalingKeys )
                {
                    var controller = new NodeController( ControllerType.Scale, nodeIndex, aiChannel.NodeName );
                    foreach ( var aiKey in aiChannel.ScalingKeys )
                    {
                        controller.Keys.Add( new Vector3Key
                        {
                            Time = ConvertTime( aiKey.Time, aiAnimation.TicksPerSecond ),
                            Value = aiKey.Value.FromAssimp()
                        } );
                    }

                    motion.Controllers.Add( controller );
                }
            }

            return motion;
        }

        private static short ConvertTime( double ticks, double ticksPerSecond )
        {
            return ( short )( ticks / ( ticksPerSecond / 30f ) );
        }

        private static int FindNodeIndex( Assimp.Node rootNode, string nodeName )
        {
            int targetId = 0;

            bool GetTargetIdForNodeRecursive( Assimp.Node node )
            {
                if ( node.Name == nodeName )
                {
                    return true;
                }

                ++targetId;

                foreach ( var child in node.Children )
                {
                    if ( GetTargetIdForNodeRecursive( child ) )
                        return true;
                }

                return false;
            }

            if ( GetTargetIdForNodeRecursive( rootNode ) )
                return targetId;
            else
                return -1;
        }
    }
}