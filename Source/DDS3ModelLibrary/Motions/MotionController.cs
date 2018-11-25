using System;
using DDS3ModelLibrary.IO.Common;

namespace DDS3ModelLibrary.Motions
{
    /// <summary>
    /// Controls the animation of a single property of a node.
    /// The property that is modified depends on the type.
    /// </summary>
    public class MotionController : IBinarySerializable
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
        /// Gets or sets Field06.
        /// Always 0.
        /// </summary>
        public short Field06 { get; set; }

        public MotionController()
        {
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            Type = ( ControllerType )reader.ReadUInt16();
            Field02 = reader.ReadInt16();
            NodeIndex = reader.ReadInt16();
            Field06 = reader.ReadInt16();
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            writer.WriteInt16( ( short )Type );
            writer.WriteInt16( Field02 );
            writer.WriteInt16( NodeIndex );
            writer.WriteInt16( Field06 );
        }
    }
}