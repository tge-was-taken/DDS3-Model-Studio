﻿using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.PS2.VIF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace DDS3ModelLibrary.Models
{

    public class MeshType7Batch : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public short UsedNodeCount => (short)NodeBatches.Count;

        public short VertexCount => (short)(NodeBatches.Count > 0 ? NodeBatches[0].VertexCount : 0);

        public List<MeshType7NodeBatch> NodeBatches { get; }

        public Vector2[] TexCoords { get; set; }

        public MeshType7Batch()
        {
            NodeBatches = new List<MeshType7NodeBatch>();
        }

        public (Vector3[] Positions, Vector3[] Normals, NodeWeight[][] Weights) Transform(List<Node> nodes)
        {
            var positions = new Vector3[VertexCount];
            var normals = new Vector3[positions.Length];
            var weights = new NodeWeight[positions.Length][];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = new NodeWeight[UsedNodeCount];

            for (var nodeBatchIndex = 0; nodeBatchIndex < NodeBatches.Count; nodeBatchIndex++)
            {
                var nodeBatch = NodeBatches[nodeBatchIndex];
                var nodeWorldTransform = nodes[nodeBatch.NodeIndex].WorldTransform;

                for (int i = 0; i < nodeBatch.Positions.Length; i++)
                {
                    var position = new Vector3(nodeBatch.Positions[i].X, nodeBatch.Positions[i].Y,
                                                nodeBatch.Positions[i].Z);
                    var weight = nodeBatch.Positions[i].W;
                    var weightedNodeWorldTransform = nodeWorldTransform * weight;
                    var weightedWorldPosition = Vector3.Transform(position, weightedNodeWorldTransform);
                    positions[i] += weightedWorldPosition;

                    if (nodeBatch.Normals != null)
                        normals[i] += Vector3.TransformNormal(nodeBatch.Normals[i], weightedNodeWorldTransform);

                    weights[i][nodeBatchIndex] = new NodeWeight(nodeBatch.NodeIndex, weight);
                }
            }

            return (positions, normals, weights);
        }

        void IBinarySerializable.Read(EndianBinaryReader reader, object context)
        {
            (short[] usedNodeIds, MeshFlags flags) = ((short[], MeshFlags))context;

            var headerPacket = reader.ReadObject<VifPacket>();
            VifValidationHelper.Ensure(headerPacket, 0xFF, true, false, 1, VifUnpackElementFormat.Short, 2);

            var usedNodeCount = headerPacket.Int16Arrays[0][0];
            var vertexCount = headerPacket.Int16Arrays[0][1];

            if (usedNodeCount + 1 != usedNodeIds.Length)
                throw new InvalidDataException("Used node count + 1 is not equal to the number of used node ids.");

            foreach (short nodeId in usedNodeIds)
                NodeBatches.Add(reader.ReadObject<MeshType7NodeBatch>(nodeId));

            // @NOTE(TGE): Not a single mesh type 7 mesh does not have this flag set so I don't know if it should be checked for.
            var texCoordsPacket = reader.ReadObject<VifPacket>();
            VifValidationHelper.Ensure(texCoordsPacket, null, true, false, vertexCount, VifUnpackElementFormat.Float, 2);
            TexCoords = texCoordsPacket.Vector2Array;

            var texCoordsKickTag = reader.ReadObject<VifCode>();
            VifValidationHelper.Ensure(texCoordsKickTag, 0, 0, VifCommand.CntMicro);

            var flushTag = reader.ReadObject<VifCode>();
            VifValidationHelper.Ensure(flushTag, 0, 0, VifCommand.FlushEnd);
            reader.Align(16);
        }

        void IBinarySerializable.Write(EndianBinaryWriter writer, object context)
        {
            var vif = (VifCodeStreamBuilder)context;

            if (UsedNodeCount < 2)
                throw new InvalidOperationException("Mesh batch must have at least 2 used nodes");

            vif.UnpackHeader((short)(UsedNodeCount - 1), VertexCount);

            for (var i = 0; i < NodeBatches.Count; i++)
            {
                var batch = NodeBatches[i];
                writer.WriteObject(batch, (vif, i == 0));
            }

            if (TexCoords != null)
            {
                vif.Unpack(TexCoords);
                vif.ExecuteMicro();
            }

            vif.FlushEnd();
        }
    }
}