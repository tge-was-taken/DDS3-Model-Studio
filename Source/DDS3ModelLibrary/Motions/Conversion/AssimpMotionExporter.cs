using System;
using System.Collections.Generic;
using System.Numerics;
using DDS3ModelLibrary.Models;
using DDS3ModelLibrary.Models.Conversion;
using DDS3ModelLibrary.Models.Utilities;

namespace DDS3ModelLibrary.Motions.Conversion
{
    public sealed partial class AssimpMotionExporter : MotionExporter<AssimpMotionExporter, AssimpMotionExporter.Config>
    {
        public override void Export( Model model, Motion motion, string filepath, Config config )
        {
            var aiScene = AssimpModelExporter.Instance.ConvertToScene( model, new AssimpModelExporter.Config() );     

            var aiAnimation = new Assimp.Animation
            {
                DurationInTicks = motion.Duration,
                TicksPerSecond  = 30
            };

            var aiChannelLookup = new Dictionary<int, Assimp.NodeAnimationChannel>();
            for ( var i = 0; i < model.Nodes.Count; i++ )
            {
                var node = model.Nodes[ i ];
                aiAnimation.NodeAnimationChannels.Add( aiChannelLookup[ i ] = new Assimp.NodeAnimationChannel()
                {
                    NodeName = node.Name.Replace( " ", "_" ),
                });
            }

            var aiChannelLookupRev = new Dictionary<Assimp.NodeAnimationChannel, int>();
            foreach ( var lookup in aiChannelLookup )
                aiChannelLookupRev[lookup.Value] = lookup.Key;

            foreach ( var controller in motion.Controllers )
            {
                if ( !aiChannelLookup.TryGetValue( controller.NodeIndex, out var aiChannel ) )
                {
                    throw new InvalidOperationException();
                }

                switch ( controller.Type )
                {
                    case ControllerType.Position:
                        foreach ( var key in controller.Keys )
                        {
                            switch ( key )
                            {
                                case Vector3Key positionKey:
                                    aiChannel.PositionKeys.RemoveAll( x => x.Time == positionKey.Time );
                                    aiChannel.PositionKeys.Add( new Assimp.VectorKey( positionKey.Time, positionKey.Value.ToAssimp() ) );
                                    break;
                            }
                        }
                        break;
                    case ControllerType.Type1:
                        break;
                    case ControllerType.Scale:
                        foreach ( var key in controller.Keys )
                        {
                            switch ( key )
                            {
                                case Vector3Key scaleKey:
                                    aiChannel.ScalingKeys.RemoveAll( x => x.Time == scaleKey.Time );
                                    aiChannel.ScalingKeys.Add( new Assimp.VectorKey( scaleKey.Time, scaleKey.Value.ToAssimp() ) );
                                    break;
                            }
                        }
                        break;
                    case ControllerType.Rotation:
                        foreach ( var key in controller.Keys )
                        {
                            switch ( key )
                            {
                                case QuaternionKey rotationKey:
                                    aiChannel.RotationKeys.RemoveAll( x => x.Time == rotationKey.Time );
                                    aiChannel.RotationKeys.Add( new Assimp.QuaternionKey( rotationKey.Time, Quaternion.Inverse( rotationKey.Value ).ToAssimp() ) );
                                    break;
                            }
                        }
                        break;
                    case ControllerType.Morph:
                        break;
                    case ControllerType.Type5:
                        break;
                    case ControllerType.Type8:
                        break;
                }
            }

            foreach ( var aiChannel in aiAnimation.NodeAnimationChannels )
            {
                if ( aiChannel.PositionKeys.Count == 0 || aiChannel.PositionKeys[ 0 ].Time != 0 )
                    aiChannel.PositionKeys.Insert( 0, new Assimp.VectorKey( 0, model.Nodes[ aiChannelLookupRev[ aiChannel ] ].Position.ToAssimp() ) );

                if ( aiChannel.RotationKeys.Count == 0 || aiChannel.RotationKeys[0].Time != 0 )
                {
                    var rotation = model.Nodes[aiChannelLookupRev[aiChannel]].Rotation;
                    aiChannel.RotationKeys.Insert( 0, new Assimp.QuaternionKey( 0, Quaternion.Inverse( Quaternion.CreateFromRotationMatrix( ( Matrix4x4.CreateRotationX( rotation.X ) *
                                                                                                                                               Matrix4x4.CreateRotationY( rotation.Y ) *
                                                                                                                                               Matrix4x4.CreateRotationZ( rotation.Z ) ) ) ).ToAssimp() ) );
                }

                if ( aiChannel.ScalingKeys.Count == 0 || aiChannel.ScalingKeys[0].Time != 0 )
                    aiChannel.ScalingKeys.Insert( 0, new Assimp.VectorKey( 0, model.Nodes[aiChannelLookupRev[aiChannel]].Scale.ToAssimp() ) );


                var keyCount =
                    Math.Max( 1, Math.Max( aiChannel.PositionKeyCount, Math.Max( aiChannel.RotationKeyCount, aiChannel.ScalingKeyCount ) ) );

                if ( aiChannel.PositionKeyCount < keyCount )
                {
                    var lastKey = aiChannel.PositionKeyCount == 0
                        ? new Assimp.VectorKey( -1, model.Nodes[ aiChannelLookupRev[ aiChannel ] ].Position.ToAssimp() )
                        : aiChannel.PositionKeys[ aiChannel.PositionKeyCount - 1 ];

                    while ( aiChannel.PositionKeyCount < keyCount )
                    {
                        lastKey.Time++;
                        if ( lastKey.Time > aiAnimation.DurationInTicks )
                            break;
                        aiChannel.PositionKeys.Add( lastKey );
                    }
                }

                if ( aiChannel.RotationKeyCount < keyCount )
                {
                    var rotation = model.Nodes[aiChannelLookupRev[aiChannel]].Rotation;

                    var lastKey = aiChannel.RotationKeyCount == 0
                        ? new Assimp.QuaternionKey( -1, Quaternion.Inverse( Quaternion.CreateFromRotationMatrix( ( Matrix4x4.CreateRotationX( rotation.X ) *
                                                                                              Matrix4x4.CreateRotationY( rotation.Y ) *
                                                                                              Matrix4x4.CreateRotationZ( rotation.Z ) ) ) ).ToAssimp() )
                        : aiChannel.RotationKeys[ aiChannel.RotationKeyCount - 1 ];

                    while ( aiChannel.RotationKeyCount < keyCount )
                    {
                        lastKey.Time++;
                        if ( lastKey.Time > aiAnimation.DurationInTicks )
                            break;
                        aiChannel.RotationKeys.Add( lastKey );
                    }
                }

                if ( aiChannel.ScalingKeyCount < keyCount )
                {
                    var lastKey = aiChannel.ScalingKeyCount == 0
                        ? new Assimp.VectorKey( -1, model.Nodes[aiChannelLookupRev[aiChannel]].Scale.ToAssimp() )
                        : aiChannel.ScalingKeys[aiChannel.ScalingKeyCount - 1];

                    while ( aiChannel.ScalingKeyCount < keyCount )
                    {
                        lastKey.Time++;
                        if ( lastKey.Time > aiAnimation.DurationInTicks )
                            break;
                        aiChannel.ScalingKeys.Add( lastKey );
                    }
                }
            }

            aiScene.Animations.Add( aiAnimation );

            //aiScene.ExportColladaFile( filepath );
            using ( var aiContext = new Assimp.AssimpContext() )
                aiContext.ExportFile( aiScene, filepath, "collada", Assimp.PostProcessSteps.FlipUVs );
        }
    }
}