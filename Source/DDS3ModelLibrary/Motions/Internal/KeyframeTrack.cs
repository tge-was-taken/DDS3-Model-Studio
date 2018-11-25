using System;
using System.Collections.Generic;
using System.Linq;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Motions.Internal
{
    internal class KeyframeTrack : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public List<IKeyframe> Keyframes { get; }

        public KeyframeTrack()
        {
            Keyframes = new List<IKeyframe>();
        }

        public KeyframeTrack( List<IKeyframe> keyframes )
        {
            Keyframes = keyframes ?? throw new ArgumentNullException( nameof( keyframes ) );
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            reader.SeekCurrent( 4 );
            var keyframeCount = reader.ReadInt16();
            var keyframeSize = reader.ReadInt16();
            var keyframeTimings = reader.ReadInt16Array( keyframeCount );
            reader.Align( 4 );

            var controllerType = ( ControllerType )context;
            for ( int i = 0; i < keyframeCount; i++ )
            {
                IKeyframe keyframe;

                switch ( controllerType )
                {
                    case ControllerType.Translation:
                        switch ( keyframeSize )
                        {
                            case 4:
                                keyframe = reader.ReadObject<TranslationKeyframeSize4>();
                                break;

                            case 8:
                                keyframe = reader.ReadObject<TranslationKeyframeSize8>();
                                break;

                            case 12:
                                keyframe = reader.ReadObject<TranslationKeyframeSize12>();
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case ControllerType.Type1:
                        switch ( keyframeSize )
                        {
                            case 4:
                                keyframe = reader.ReadObject<MorphKeyframeSize4>();
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case ControllerType.Scale:
                        switch ( keyframeSize )
                        {
                            case 12:
                                keyframe = reader.ReadObject<ScaleKeyframeSize12>();
                                break;

                            case 20:
                                keyframe = reader.ReadObject<RotationKeyframeSize20>();
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case ControllerType.Rotation:
                        switch ( keyframeSize )
                        {
                            case 8:
                                keyframe = reader.ReadObject<RotationKeyframeSize8>();
                                break;

                            case 20:
                                keyframe = reader.ReadObject<RotationKeyframeSize20>();
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case ControllerType.Morph:
                        switch ( keyframeSize )
                        {
                            case 1:
                                keyframe = reader.ReadObject<MorphKeyframeSize1>();
                                break;

                            case 4:
                                keyframe = reader.ReadObject<MorphKeyframeSize4>();
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case ControllerType.Type5:
                        switch ( keyframeSize )
                        {
                            case 4:
                                keyframe = reader.ReadObject<Type5KeyframeSize4>();
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case ControllerType.Type8:
                        switch ( keyframeSize )
                        {
                            case 4:
                                keyframe = reader.ReadObject<Type8KeyframeSize4>();
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }

                keyframe.Time = keyframeTimings[ i ];
                Keyframes.Add( keyframe );
            }

            reader.Align( 4 );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            var start = writer.Position;
            var dataSize = 0;
            var keyframeSize = Keyframes.Count > 0 ? Keyframes[ 0 ].Size : 0;

            writer.WriteInt32( dataSize );
            writer.WriteInt16( ( short )Keyframes.Count );
            writer.WriteInt16( ( short ) keyframeSize );
            writer.WriteInt16s( Keyframes.Select( x => x.Time ) );
            writer.Align( 4 );
            writer.WriteObjects( Keyframes );
            writer.Align( 4 );

            var end = writer.Position;
            writer.SeekBegin( start );
            dataSize = ( int ) ( end - start );
            writer.WriteInt32( dataSize );
            writer.SeekBegin( end );
        }
    }
}