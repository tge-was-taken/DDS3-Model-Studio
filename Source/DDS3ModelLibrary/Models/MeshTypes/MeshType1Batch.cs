﻿using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.PS2.VIF;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;

namespace DDS3ModelLibrary.Models
{
    public class MeshType1Batch : IBinarySerializable
    {
        private Triangle[] mTriangles;
        private Vector3[] mPositions;
        private Vector3[] mNormals;
        private Vector2[] mTexCoords;
        private Vector2[] mTexCoords2;
        private Color[] mColors;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public short TriangleCount => (short)(Triangles?.Length ?? 0);

        public short VertexCount => (short)(Positions?.Length ?? 0);

        public MeshFlags Flags { get; set; }

        public Triangle[] Triangles
        {
            get => mTriangles;
            set => mTriangles = value ?? throw new ArgumentNullException(nameof(value));
        }

        public Vector3[] Positions
        {
            get => mPositions;
            set => mPositions = value ?? throw new ArgumentNullException(nameof(value));
        }

        public Vector3[] Normals
        {
            get => mNormals;
            set => Flags = MeshFlagsHelper.Update(Flags, mNormals = value, MeshFlags.Normal);
        }

        public Vector2[] TexCoords
        {
            get => mTexCoords;
            set => Flags = MeshFlagsHelper.Update(Flags, mTexCoords = value, MeshFlags.TexCoord);
        }

        public Vector2[] TexCoords2
        {
            get => mTexCoords2;
            set
            {
                Flags = MeshFlagsHelper.Update(Flags, mTexCoords2 = value, MeshFlags.TexCoord2);
                if (mTexCoords2 != null && mTexCoords == null)
                    throw new InvalidOperationException("TexCoord2 can not be used when TexCoord is null");
            }
        }

        public Color[] Colors
        {
            get => mColors;
            set => Flags = MeshFlagsHelper.Update(Flags, mColors = value, MeshFlags.Color);
        }

        public MeshBatchRenderMode RenderMode { get; set; }

        public MeshType1Batch()
        {
            // 13284 SmoothShading, TexCoord, Bit5, Bit6, Bit14, RequiredForField, Bit22, Normal, FieldTexture
            // 161915 SmoothShading, TexCoord, Bit5, Bit6, RequiredForField, Bit22, Normal, FieldTexture
            Flags = MeshFlags.SmoothShading | MeshFlags.Bit5 | MeshFlags.Bit6 | MeshFlags.RequiredForField | MeshFlags.Bit22 | MeshFlags.FieldTexture;

            // 84507 Mode2
            // 112539 Mode1
            RenderMode = MeshBatchRenderMode.Mode1;
        }

        public (Vector3[] Positions, Vector3[] Normals) Transform(Matrix4x4 nodeWorldTransform)
        {
            var positions = new Vector3[VertexCount];
            var normals = Normals != null ? new Vector3[positions.Length] : null;

            for (int i = 0; i < Positions.Length; i++)
            {
                positions[i] = Vector3.Transform(Positions[i], nodeWorldTransform);

                if (normals != null)
                    normals[i] = Vector3.TransformNormal(Normals[i], nodeWorldTransform);
            }

            return (positions, normals);
        }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            // Read header
            var headerPacket = reader.ReadObject<VifPacket>();
            VifValidationHelper.Ensure(headerPacket, 0, true, true, 1, VifUnpackElementFormat.Short, 4);
            var triangleCount = headerPacket.Int16Arrays[0][0];
            var vertexCount = headerPacket.Int16Arrays[0][1];
            var flags = Flags = (MeshFlags)((ushort)headerPacket.Int16Arrays[0][2] | (ushort)headerPacket.Int16Arrays[0][3] << 16);

            // Read triangles
            var indicesPacket = reader.ReadObject<VifPacket>();
            VifValidationHelper.Ensure(indicesPacket, 1, true, true, triangleCount, VifUnpackElementFormat.Byte, 4);
            Triangles = indicesPacket.SByteArrays.Select(x =>
            {
                if (x[3] != 0)
                    throw new InvalidDataException("Fourth element in indices packet isn't 0");

                return new Triangle((byte)x[0], (byte)x[1], (byte)x[2]);
            }).ToArray();

            // Read positions
            var positionsPacket = reader.ReadObject<VifPacket>();
            VifValidationHelper.Ensure(positionsPacket, null, true, true, vertexCount, VifUnpackElementFormat.Float, 3);
            Positions = positionsPacket.Vector3Array;

            // Read normals
            if (flags.HasFlag(MeshFlags.Normal))
            {
                var normalsPacket = reader.ReadObject<VifPacket>();
                VifValidationHelper.Ensure(normalsPacket, null, true, true, vertexCount, VifUnpackElementFormat.Float, 3);
                Normals = normalsPacket.Vector3Array;
            }

            if (flags.HasFlag(MeshFlags.TexCoord))
            {
                // Read texture coords
                if (!flags.HasFlag(MeshFlags.TexCoord2))
                {
                    var texCoordsPacket = reader.ReadObject<VifPacket>();
                    VifValidationHelper.Ensure(texCoordsPacket, null, true, true, vertexCount, VifUnpackElementFormat.Float, 2);
                    TexCoords = texCoordsPacket.Vector2Array;
                }
                else
                {
                    var texCoordsPacket = reader.ReadObject<VifPacket>();
                    VifValidationHelper.Ensure(texCoordsPacket, null, true, true, vertexCount, VifUnpackElementFormat.Float, 4);
                    TexCoords = texCoordsPacket.Vector4Array.Select(x => new Vector2(x.X, x.Y)).ToArray();
                    TexCoords2 = texCoordsPacket.Vector4Array.Select(x => new Vector2(x.Z, x.W)).ToArray();
                }
            }

            if (flags.HasFlag(MeshFlags.Color))
            {
                // Read colors
                var colorsPacket = reader.ReadObject<VifPacket>();
                VifValidationHelper.Ensure(colorsPacket, null, true, true, vertexCount, VifUnpackElementFormat.Byte, 4);
                Colors = colorsPacket.SByteArrays.Select(x => new Color((byte)x[0], (byte)x[1], (byte)x[2], (byte)x[3]))
                                     .ToArray();
            }

            // Read activate command
            var activateCode = reader.ReadObject<VifCode>();
            VifValidationHelper.ValidateActivateMicro(activateCode);

            // Not sure if this makes any difference yet
            RenderMode = activateCode.Immediate == 0x0C ? MeshBatchRenderMode.Mode1 : MeshBatchRenderMode.Mode2;

            Debug.Assert(Flags == flags);
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            var vif = (VifCodeStreamBuilder)context;

            // Header
            vif.UnpackHeader(TriangleCount, VertexCount, (uint)Flags);

            var nextAddress = 8;

            // Triangles
            vif.Unpack(nextAddress, Triangles.Select(x => new sbyte[] { (sbyte)x.A, (sbyte)x.B, (sbyte)x.C, 0 }).ToArray());
            nextAddress = AlignmentHelper.Align(nextAddress + ((TriangleCount * 4) * 2), 8);

            // Positions
            var effectiveVertexSize = (int)((VertexCount * 12) / 1.5f);
            vif.Unpack(nextAddress, Positions);
            nextAddress = AlignmentHelper.Align(nextAddress + effectiveVertexSize, 8);

            if (Flags.HasFlag(MeshFlags.Normal))
            {
                // Normals
                vif.Unpack(nextAddress, Normals);
                nextAddress = AlignmentHelper.Align(nextAddress + effectiveVertexSize, 8);
            }

            if (Flags.HasFlag(MeshFlags.TexCoord))
            {
                if (!Flags.HasFlag(MeshFlags.TexCoord2))
                {
                    // Texcoord 1
                    vif.Unpack(nextAddress, TexCoords);
                }
                else
                {
                    // Texcoord 1 & 2
                    var mergedTexCoords = new Vector4[VertexCount];
                    for (int i = 0; i < mergedTexCoords.Length; i++)
                    {
                        mergedTexCoords[i].X = TexCoords[i].X;
                        mergedTexCoords[i].Y = TexCoords[i].Y;
                        mergedTexCoords[i].Z = TexCoords2[i].X;
                        mergedTexCoords[i].W = TexCoords2[i].Y;
                    }

                    vif.Unpack(nextAddress, mergedTexCoords);
                }

                nextAddress = AlignmentHelper.Align(nextAddress + (TexCoords.Length * 8), 8);
            }

            if (Flags.HasFlag(MeshFlags.Color))
                vif.Unpack(nextAddress, Colors.Select(x => new[] { (sbyte)x.R, (sbyte)x.G, (sbyte)x.B, (sbyte)x.A }).ToArray());

            vif.ActivateMicro((ushort)(RenderMode == MeshBatchRenderMode.Mode1 ? 0x0C : 0x10));
        }
    }
}