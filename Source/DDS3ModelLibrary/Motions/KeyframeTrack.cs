using System;
using System.Collections.Generic;
using System.Linq;

using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Motions
{
    public class KeyframeTrack : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public List<IKeyframe> Keyframes { get; }

        public KeyframeTrack()
        {
            Keyframes = new List<IKeyframe>();
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            var dataSize = reader.ReadInt32();
            var keyframeCount = reader.ReadInt16();
            var keyframeSize = reader.ReadInt16();
            var keyframeTimings = reader.ReadInt16Array( keyframeCount );
            reader.Align( 4 );

            var controller = ( MotionController )context;
            for ( int i = 0; i < keyframeCount; i++ )
            {
                switch ( controller.Type )
                {
                    case ControllerType.Translation:
                        switch ( keyframeSize )
                        {
                            case 4:
                                Keyframes.Add( reader.ReadObject<TranslationKeyframeSize4>( keyframeTimings[ i ] ) );
                                break;

                            case 8:
                                Keyframes.Add( reader.ReadObject<TranslationKeyframeSize8>( keyframeTimings[i] ) );
                                break;

                            case 12:
                                Keyframes.Add( reader.ReadObject<TranslationKeyframeSize12>( keyframeTimings[i] ) );
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case ControllerType.Type1:
                        switch ( keyframeSize )
                        {
                            case 4:
                                Keyframes.Add( reader.ReadObject<MorphKeyframeSize4>( keyframeTimings[i] ) );
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case ControllerType.Scale:
                        switch ( keyframeSize )
                        {
                            case 12:
                                Keyframes.Add( reader.ReadObject<ScaleKeyframeSize12>( keyframeTimings[i] ) );
                                break;

                            case 20:
                                Keyframes.Add( reader.ReadObject<RotationKeyframeSize20>( keyframeTimings[i] ) );
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case ControllerType.Rotation:
                        switch ( keyframeSize )
                        {
                            case 8:
                                Keyframes.Add( reader.ReadObject<RotationKeyframeSize8>( keyframeTimings[i] ) );
                                break;

                            case 20:
                                Keyframes.Add( reader.ReadObject<RotationKeyframeSize20>( keyframeTimings[i] ) );
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case ControllerType.Morph:
                        switch ( keyframeSize )
                        {
                            case 1:
                                Keyframes.Add( reader.ReadObject<MorphKeyframeSize1>( keyframeTimings[i] ) );
                                break;

                            case 4:
                                Keyframes.Add( reader.ReadObject<MorphKeyframeSize4>( keyframeTimings[i] ) );
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case ControllerType.Type5:
                        switch ( keyframeSize )
                        {
                            case 4:
                                Keyframes.Add( reader.ReadObject<Type5KeyframeSize4>( keyframeTimings[i] ) );
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case ControllerType.Type8:
                        switch ( keyframeSize )
                        {
                            case 4:
                                Keyframes.Add( reader.ReadObject<Type8KeyframeSize4>( keyframeTimings[i] ) );
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            reader.Align( 4 );
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            var start = writer.Position;
            var keyframeSize = Keyframes.Count > 0 ? Keyframes[ 0 ].Size : 0;

            writer.WriteInt32( 0 );
            writer.WriteInt16( ( short )Keyframes.Count );
            writer.WriteInt16( ( short ) keyframeSize );
            writer.WriteInt16s( Keyframes.Select( x => x.Time ) );
            writer.Align( 4 );
            writer.WriteObjects( Keyframes );
            writer.Align( 4 );

            var end = writer.Position;
            writer.SeekBegin( start );
            writer.WriteInt32( ( int ) ( end - start ) );
            writer.SeekBegin( end );
        }
    }
}