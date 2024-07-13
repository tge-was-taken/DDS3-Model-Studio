﻿using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.PS2.VIF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace DDS3ModelLibrary.Models
{
    public class MeshType7 : Mesh
    {
        private Triangle[] mTriangles;
        private Vector2[] mTexCoords2;

        public override MeshType Type => MeshType.Type7;

        public override short TriangleCount => (short)Triangles.Length;

        public override short VertexCount => (short)Batches.Sum(x => x.VertexCount);

        public MeshFlags Flags { get; set; }

        public short UsedNodeCount => Batches.Count == 0 ? (short)0 : (short)Batches[0].NodeBatches.Count;

        public IEnumerable<short> UsedNodeIndices => Batches.Count == 0 ? Enumerable.Empty<short>() : Batches[0].NodeBatches.Select(x => x.NodeIndex);

        public Triangle[] Triangles
        {
            get => mTriangles;
            set => mTriangles = value ?? throw new ArgumentNullException(nameof(value));
        }

        public List<MeshType7Batch> Batches { get; private set; }

        public Vector2[] TexCoords2
        {
            get => mTexCoords2;
            set => Flags = MeshFlagsHelper.Update(Flags, mTexCoords2 = value, MeshFlags.TexCoord2);
        }

        public MeshType7()
        {
            Batches = new List<MeshType7Batch>();
            Flags = MeshFlags.SmoothShading | MeshFlags.TexCoord | MeshFlags.Bit5 | MeshFlags.Bit6 | MeshFlags.RequiredForField | MeshFlags.Bit22 | MeshFlags.Normal |
                    MeshFlags.FieldTexture | MeshFlags.Weights;
        }

        protected override void Read(EndianBinaryReader reader)
        {
            // Read header
            var field00 = reader.ReadInt16Expects(0, "Field00 is not 0");
            MaterialIndex = reader.ReadInt16();
            var field04 = reader.ReadInt32Expects(0, "Field04 is not 0");
            var field08 = reader.ReadInt32Expects(0, "Field08 is not 0");
            var triangleCount = reader.ReadInt16();
            var vertexCount = reader.ReadInt16();
            var flags = Flags = (MeshFlags)reader.ReadInt32();
            var usedNodeCount = reader.ReadInt16();
            var usedNodes = reader.ReadInt16Array(usedNodeCount);
            reader.Align(16);

            // Read triangles
            Triangles = new Triangle[triangleCount];
            for (int i = 0; i < triangleCount; i++)
                Triangles[i] = new Triangle(a: reader.ReadUInt16(), b: reader.ReadUInt16(), c: reader.ReadUInt16());

            reader.Align(16);

            // Read batches
            var readVertexCount = 0;
            while (readVertexCount < vertexCount)
            {
                var batch = reader.ReadObject<MeshType7Batch>((usedNodes, Flags));
                readVertexCount += batch.VertexCount;
                Batches.Add(batch);
            }

            if (Flags.HasFlag(MeshFlags.TexCoord2))
            {
                TexCoords2 = reader.ReadVector2Array(vertexCount);
            }

            Debug.Assert(Flags == flags, "Flags doesn't match value read from file");
        }

        protected override void Write(EndianBinaryWriter writer)
        {
            // Write header
            writer.Write((short)0);
            writer.Write(MaterialIndex);
            writer.Write(0);
            writer.Write(0);
            writer.Write(TriangleCount);
            writer.Write(VertexCount);
            writer.Write((int)Flags);
            writer.Write(UsedNodeCount);
            writer.Write(UsedNodeIndices);
            writer.Align(16);

            // Write triangles
            foreach (var triangle in Triangles)
            {
                writer.Write(triangle.A);
                writer.Write(triangle.B);
                writer.Write(triangle.C);
            }

            writer.Align(16);

            var vifCmd = new VifCodeStreamBuilder();

            // Writer batches
            foreach (var batch in Batches)
            {
                // TODO: change this
                writer.WriteObject(batch, vifCmd);
            }

            writer.WriteObject(vifCmd);

            if (Flags.HasFlag(MeshFlags.TexCoord2))
            {
                writer.Write(TexCoords2);
            }
        }
    }
}