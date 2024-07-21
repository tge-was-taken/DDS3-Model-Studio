using DDS3ModelLibrary.IO;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.IO.Internal;
using DDS3ModelLibrary.Models;
using DDS3ModelLibrary.Motions.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace DDS3ModelLibrary.Motions
{
    public class MotionPack : Resource
    {
        public override ResourceDescriptor ResourceDescriptor { get; } =
            new ResourceDescriptor(ResourceFileType.MotionPack, ResourceIdentifier.MotionPack);

        // caching
        private List<MotionControllerDefinition> mControllerDefinitions;
        private List<MotionDefinition> mMotionDefinitions;

        public short Group { get; set; }
        public short PlayGroup { get; set; }
        public short FirstMotion { get; set; }
        public short Flags { get; set; }
        public List<Motion> Motions { get; private set; }

        public MotionPack()
        {
            Motions = new List<Motion>();
        }

        public MotionPack(Stream stream, bool leaveOpen = false) : this()
        {
            using (var reader = new EndianBinaryReader(stream, leaveOpen, Endianness.Little))
                Read(reader);
        }

        public MotionPack(string filePath) : this()
        {
            using (var reader = new EndianBinaryReader(new MemoryStream(File.ReadAllBytes(filePath)), filePath, Endianness.Little))
                Read(reader);
        }

        internal override void ReadContent(EndianBinaryReader reader, IOContext context)
        {
            var nodes = (List<Node>)context.Context;

            // -- header --
            reader.SeekCurrent(8);
            Group = reader.ReadInt16();
            PlayGroup = reader.ReadInt16();
            FirstMotion = reader.ReadInt16();
            Flags = reader.ReadInt16();

            // -- motion data header --
            reader.PushBaseOffset();
            var motionCount = reader.ReadInt16();
            var controllerCount = reader.ReadInt16();
            var motionTableOffset = reader.ReadInt32();

            // -- controllers definitions --
            mControllerDefinitions = reader.ReadObjects<MotionControllerDefinition>(controllerCount);

            // -- motions --
            reader.ReadAtOffset(motionTableOffset, () =>
            {
                mMotionDefinitions = reader.ReadObjectOffsets<MotionDefinition>(motionCount, mControllerDefinitions);
                foreach (var motionDef in mMotionDefinitions)
                {
                    var motion = motionDef != null ? new Motion(motionDef, mControllerDefinitions) : null;

                    if (motion != null && nodes != null && motion.Controllers.All(x => x.NodeIndex < nodes.Count))
                    {
                        // Assign node names
                        foreach (var controller in motion.Controllers)
                            controller.NodeName = nodes[controller.NodeIndex].Name;
                    }

                    Motions.Add(motion);
                }
            });

            // TODO: proper caching
            mControllerDefinitions = null;
            mMotionDefinitions = null;
        }

        internal override void WriteContent(EndianBinaryWriter writer, IOContext context)
        {
            writer.OffsetPositions.Clear();

            var start = writer.Position;

            // -- header --
            // Write relocation table last (lowest priority)
            writer.PushBaseOffset(start + 0x10);
            writer.ScheduleWriteOffsetAligned(-1, 4, false, () =>
            {
                // Encode & write relocation table
                var encodedRelocationTable =
                    RelocationTableEncoding.Encode(writer.OffsetPositions.Select(x => (int)x).ToList(), (int)writer.BaseOffset);
                writer.WriteBytes(encodedRelocationTable);

                var end = writer.Position;
                writer.SeekBegin(start + 4);
                writer.WriteInt32(encodedRelocationTable.Length);
                writer.SeekBegin(end);
            });

            writer.WriteInt32(0);
            writer.WriteInt16(Group);
            writer.WriteInt16(PlayGroup);
            writer.WriteInt16(FirstMotion);
            writer.WriteInt16(Flags);

            // -- motion data header --
            if (mControllerDefinitions == null)
            {
                mControllerDefinitions = Motions.Where(x => x != null)
                                                .SelectMany(x => x.Controllers)
                                                .Select(x => x.GetDefinition())
                                                .Distinct()
                                                .OrderBy( x => x.NodeIndex )
                                                .ThenBy( x => (int)x.Type )
                                                .ToList();
            }

            if (mMotionDefinitions == null)
            {
                mMotionDefinitions = BuildMotionDefinitions();
            }

            writer.WriteInt16((short)Motions.Count);
            writer.WriteInt16((short)mControllerDefinitions.Count);
            writer.ScheduleWriteOffsetAligned(4, () =>
            {
                writer.ScheduleWriteObjectOffsetsAligned(mMotionDefinitions, 4);
            });

            // -- controller definitions --
            writer.WriteObjects(mControllerDefinitions);

            // write all the things
            writer.PerformScheduledWrites();
            writer.PopBaseOffset();
        }

        private List<MotionDefinition> BuildMotionDefinitions()
        {
            var motionDefs = new List<MotionDefinition>();
            var motionDefCache = new Dictionary<Motion, MotionDefinition>();

            foreach (var motion in Motions)
            {
                if (motion == null)
                {
                    motionDefs.Add(null);
                    continue;
                }

                if (motionDefCache.TryGetValue(motion, out var motionDef))
                {
                    motionDefs.Add(motionDef);
                    continue;
                }

                motionDef = new MotionDefinition
                {
                    Duration = motion.Duration,
                };

                foreach (var controllerDef in mControllerDefinitions)
                {
                    var controller = motion.Controllers.FirstOrDefault(x => x.GetDefinitionHash() == controllerDef.GetHashCode());

                    if (controller != null)
                    {
                        motionDef.Tracks.Add(new KeyframeTrack(controller.Keys));
                        continue;
                    }

                    // Need to add dummy values
                    var keyframes = new List<IKey>();

                    void AddPlaceholderKeyframe(short time)
                    {
                        IKey key;
                        switch (controllerDef.Type)
                        {
                            case ControllerType.Position:
                                key = new Vector3Key();
                                break;
                            case ControllerType.Type1:
                            case ControllerType.Morph:
                            case ControllerType.Type8:
                                key = new UInt32Key();
                                break;
                            case ControllerType.Scale:
                                key = new Vector3Key { Value = Vector3.One };
                                break;
                            case ControllerType.Rotation:
                                key = new QuaternionKey { Value = Quaternion.Identity };
                                break;
                            case ControllerType.Type5:
                                key = new SingleKey();
                                break;
                            default:
                                throw new NotSupportedException($"Adding placeholder keyframe not implemented for type {controllerDef.Type}");
                        }

                        key.Time = time;
                        keyframes.Add(key);
                    }

                    AddPlaceholderKeyframe(0);
                    motionDef.Tracks.Add(new KeyframeTrack(keyframes));
                }

                motionDefs.Add(motionDef);
                motionDefCache[motion] = motionDef;
            }

            return motionDefs;
        }
    }
}
