using DDS3ModelLibrary.IO.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DDS3ModelLibrary.Motions.Internal
{
    internal class KeyframeTrack : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public List<IKey> Keyframes { get; }

        public KeyframeTrack()
        {
            Keyframes = new List<IKey>();
        }

        public KeyframeTrack(List<IKey> keyframes)
        {
            Keyframes = keyframes ?? throw new ArgumentNullException(nameof(keyframes));
        }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            var dataSize = reader.ReadInt32();
            var keyframeCount = reader.ReadInt16();
            var keyframeSize = reader.ReadInt16();
            var keyframeTimings = reader.ReadInt16Array(keyframeCount);
            reader.Align(4);

            var controllerType = (ControllerType)context;
            for (int i = 0; i < keyframeCount; i++)
            {
                IKey key;

                switch (controllerType)
                {
                    case ControllerType.Position:
                        switch (keyframeSize)
                        {
                            case 4:
                                key = reader.ReadObject<UInt32Key>();
                                break;

                            case 8:
                                key = reader.ReadObject<PositionKeySize8>();
                                break;

                            case 12:
                                key = reader.ReadObject<Vector3Key>();
                                break;

                            default:
                                key = reader.ReadObject<RawKey>(keyframeSize);
                                break;
                        }
                        break;

                    case ControllerType.Type1:
                        switch (keyframeSize)
                        {
                            case 4:
                                key = reader.ReadObject<UInt32Key>();
                                break;

                            default:
                                key = reader.ReadObject<RawKey>(keyframeSize);
                                break;
                        }
                        break;

                    case ControllerType.Scale:
                        switch (keyframeSize)
                        {
                            case 12:
                                key = reader.ReadObject<Vector3Key>();
                                break;

                            case 20:
                                key = reader.ReadObject<Single5Key>();
                                break;

                            default:
                                key = reader.ReadObject<RawKey>(keyframeSize);
                                break;
                        }
                        break;

                    case ControllerType.Rotation:
                        switch (keyframeSize)
                        {
                            case 8:
                                key = reader.ReadObject<QuaternionKey>();
                                break;

                            case 20:
                                key = reader.ReadObject<Single5Key>();
                                break;

                            default:
                                key = reader.ReadObject<RawKey>(keyframeSize);
                                break;
                        }
                        break;

                    case ControllerType.Morph:
                        switch (keyframeSize)
                        {
                            case 1:
                                key = reader.ReadObject<ByteKey>();
                                break;

                            case 4:
                                key = reader.ReadObject<UInt32Key>();
                                break;

                            default:
                                key = reader.ReadObject<RawKey>(keyframeSize);
                                break;
                        }
                        break;

                    case ControllerType.Type5:
                        switch (keyframeSize)
                        {
                            case 4:
                                key = reader.ReadObject<SingleKey>();
                                break;

                            default:
                                key = reader.ReadObject<RawKey>(keyframeSize);
                                break;
                        }
                        break;

                    case ControllerType.Type8:
                        switch (keyframeSize)
                        {
                            case 4:
                                key = reader.ReadObject<UInt32Key>();
                                break;

                            default:
                                key = reader.ReadObject<RawKey>(keyframeSize);
                                break;
                        }
                        break;

                    case ControllerType.Type10004:
                        switch (keyframeSize)
                        {
                            case 4:
                                key = reader.ReadObject<UInt32Key>();
                                break;
                            default:
                                key = reader.ReadObject<RawKey>(keyframeSize);
                                break;
                        }
                        break;

                    case ControllerType.Type20000:
                        switch (keyframeSize)
                        {
                            case 8:
                                key = reader.ReadObject<Single2Key>();
                                break;
                            default:
                                key = reader.ReadObject<RawKey>(keyframeSize);
                                break;
                        }
                        break;

                    default:
                        key = reader.ReadObject<RawKey>(keyframeSize);
                        break;
                }

                key.Time = keyframeTimings[i];
                Keyframes.Add(key);
            }

            reader.Align(4);
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            var start = writer.Position;
            var dataSize = 0;
            var keyframeSize = Keyframes.Count > 0 ? Keyframes[0].Size : 0;
            Debug.Assert(Keyframes.All(x => x.Size == keyframeSize));

            var keyframes = Keyframes.OrderBy(x => x.Time);
            writer.WriteInt32(0); // data size placeholder
            writer.WriteInt16((short)Keyframes.Count);
            writer.WriteInt16((short)keyframeSize);
            writer.WriteInt16s(keyframes.Select(x => x.Time));
            writer.Align(4);
            writer.WriteObjects(keyframes);
            writer.Align(4);

            var end = writer.Position;
            writer.SeekBegin(start);
            dataSize = (int)(end - start);
            writer.WriteInt32(dataSize);
            writer.SeekBegin(end);
        }
    }
}