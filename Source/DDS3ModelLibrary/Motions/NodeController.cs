using System;
using System.Collections.Generic;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.Motions.Internal;

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
        /// Gets or sets Field02.
        /// </summary>
        public short Field02 { get; set; }

        /// <summary>
        /// Gets or sets the index of the node whose properties are being affected by this controller.
        /// </summary>
        public short NodeIndex { get; set; }

        /// <summary>
        /// Gets or sets the name of the node whose properties are being affected by this controller.
        /// Metadata only.
        /// </summary>
        public string NodeName { get; set; }

        /// <summary>
        /// Gets or sets Field06.
        /// Always 0.
        /// </summary>
        public short Field06 { get; set; }

        /// <summary>
        /// Gets the list of keys associated with this controller.
        /// </summary>
        public List<IKey> Keys { get; private set; }

        public NodeController()
        {
            Keys = new List<IKey>();
        }

        public NodeController( ControllerType type, short nodeIndex, string nodeName ) : this()
        {
            Type = type;
            NodeIndex = nodeIndex;
            NodeName = nodeName;
        }

        internal NodeController( MotionControllerDefinition definition, List<IKey> keys )
        {
            Type = definition.Type;
            Field02 = definition.Field02;
            NodeIndex = definition.NodeIndex;
            Field06 = definition.Field06;
            Keys = keys;
        }

        internal MotionControllerDefinition GetDefinition()
        {
            return new MotionControllerDefinition
            {
                Type = Type,
                Field02 = Field02,
                NodeIndex = NodeIndex,
                Field06 = Field06
            };
        }

        internal int GetDefinitionHash()
        {
            var hashCode = -541184802;
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + Field02.GetHashCode();
            hashCode = hashCode * -1521134295 + NodeIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + Field06.GetHashCode();
            return hashCode;
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Type = ( ControllerType )reader.ReadUInt16();
            Field02 = reader.ReadInt16();
            NodeIndex = reader.ReadInt16();
            Field06 = reader.ReadInt16();
            Keys = reader.ReadObject<KeyframeTrack>().Keyframes;
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteInt16( ( short )Type );
            writer.WriteInt16( Field02 );
            writer.WriteInt16( NodeIndex );
            writer.WriteInt16( Field06 );
            writer.WriteObject( new KeyframeTrack( Keys ) );
        }
    }
}