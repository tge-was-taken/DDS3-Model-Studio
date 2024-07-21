using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.PS2.VIF;
using System.IO;
using System.Numerics;

namespace DDS3ModelLibrary.Models
{
    public class MeshType8Batch : IBinarySerializable
    {
        public short VertexCount => Positions != null ? (short)Positions.Length : (short)0;

        public Vector3[] Positions { get; set; }

        public Vector3[] Normals { get; set; }

        public Vector2[] TexCoords { get; set; }

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public MeshType8Batch()
        {
        }

        public (Vector3[] Positions, Vector3[] Normals) Transform(Matrix4x4 nodeWorldTransform)
        {
            var positions = new Vector3[VertexCount];
            var normals = new Vector3[positions.Length];

            for (int i = 0; i < Positions.Length; i++)
            {
                positions[i] = Vector3.Transform(Positions[i], nodeWorldTransform);
                normals[i] = Vector3.TransformNormal(Normals[i], nodeWorldTransform);
            }

            return (positions, normals);
        }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            var flags = (MeshFlags)context;

            var headerPacket = reader.ReadObject<VifPacket>();
            VifValidationHelper.Ensure(headerPacket, 0xFF, true, false, 1, VifUnpackElementFormat.Short, 2);

            var vertexCount = headerPacket.Int16Arrays[0][0];
            if (headerPacket.Int16Arrays[0][1] != 0)
                throw new InvalidDataException("Header packet second short is not 0");

            var positionsPacket = reader.ReadObject<VifPacket>();
            VifValidationHelper.Ensure(positionsPacket, 0, true, false, vertexCount, VifUnpackElementFormat.Float, 3);
            Positions = positionsPacket.Vector3Array;

            var normalsPacket = reader.ReadObject<VifPacket>();
            VifValidationHelper.Ensure(normalsPacket, 0x18, true, false, vertexCount, VifUnpackElementFormat.Float, 3);
            Normals = normalsPacket.Vector3Array;

            var texCoordPacket = reader.ReadObject<VifPacket>();
            VifValidationHelper.Ensure(texCoordPacket, 0x30, true, false, vertexCount, VifUnpackElementFormat.Float, 2);
            TexCoords = texCoordPacket.Vector2Array;

            var activateTag = reader.ReadObject<VifCode>();
            VifValidationHelper.Ensure(activateTag, 0x16, 0, VifCommand.ActMicro);

            var flushTag = reader.ReadObject<VifCode>();
            VifValidationHelper.Ensure(flushTag, 0, 0, VifCommand.FlushEnd);
            reader.Align(16);
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            var vif = (VifCodeStreamBuilder)context;
            vif.UnpackHeader(VertexCount, 0);
            vif.Unpack(Positions);
            vif.Unpack(Normals);
            vif.Unpack(TexCoords);
            vif.ActivateMicro(0x16);
            vif.FlushEnd();
        }
    }
}