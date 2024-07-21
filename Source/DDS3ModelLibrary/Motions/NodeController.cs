using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.Motions.Internal;
using System.Collections.Generic;

namespace DDS3ModelLibrary.Motions
{
    /// <summary>
    /// Controls the animation of a single property of a node.
    /// The property that is modified depends on the type.
    /// </summary>
    public class NodeController : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the type of controller (what it affects) this controller is.
        /// </summary>
        public ControllerType Type { get; set; }

        /// <summary>
        /// Gets or sets the index of the node whose properties are being affected by this controller.
        /// </summary>
        public int NodeIndex { get; set; }

        /// <summary>
        /// Gets or sets the name of the node whose properties are being affected by this controller.
        /// Metadata only.
        /// </summary>
        public string NodeName { get; set; }

        /// <summary>
        /// Gets the list of keys associated with this controller.
        /// </summary>
        public List<IKey> Keys { get; private set; }

        public NodeController()
        {
            Keys = new List<IKey>();
        }

        public NodeController(ControllerType type, short nodeIndex, string nodeName) : this()
        {
            Type = type;
            NodeIndex = nodeIndex;
            NodeName = nodeName;
        }

        internal NodeController(MotionControllerDefinition definition, List<IKey> keys)
        {
            Type = definition.Type;
            NodeIndex = definition.NodeIndex;
            Keys = keys;
        }

        internal MotionControllerDefinition GetDefinition()
        {
            return new MotionControllerDefinition
            {
                Type = Type,
                NodeIndex = NodeIndex,
            };
        }

        internal int GetDefinitionHash()
        {
            var hashCode = -541184802;
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + NodeIndex.GetHashCode();
            return hashCode;
        }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            Type = (ControllerType)reader.ReadInt32();
            NodeIndex = reader.ReadInt32();
            Keys = reader.ReadObject<KeyframeTrack>().Keyframes;
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            writer.WriteInt32((int)Type);
            writer.WriteInt32(NodeIndex);
            writer.WriteObject(new KeyframeTrack(Keys));
        }
    }
}