using System;
using System.Collections.Generic;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.Primitives;
using DDS3ModelLibrary.PS2.VIF;

namespace DDS3ModelLibrary
{
    public class MeshType2Batch : IBinarySerializable
    {
        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public short UsedNodeCount => ( short )NodeBatches.Count;

        public short VertexCount => ( short )( NodeBatches.Count > 0 ? NodeBatches[0].VertexCount : 0 );

        public short TriangleCount => ( short )Triangles.Length;

        public List<MeshType2NodeBatch> NodeBatches { get; }

        public Triangle[] Triangles { get; set; }

        public Vector2[][] TexCoords { get; set; }

        public Color[] Colors { get; set; }

        public MeshType2Batch()
        {
            NodeBatches = new List<MeshType2NodeBatch>();
        }

        public (Vector3[] Positions, Vector3[] Normals, NodeWeight[][] Weights) GetProcessed( List<Node> nodes )
        {
            var positions = new Vector3[VertexCount];
            var normals = new Vector3[positions.Length];
            var weights = new NodeWeight[positions.Length][];
            for ( int i = 0; i < weights.Length; i++ )
                weights[i] = new NodeWeight[UsedNodeCount];

            for ( var nodeBatchIndex = 0; nodeBatchIndex < NodeBatches.Count; nodeBatchIndex++ )
            {
                var nodeBatch = NodeBatches[nodeBatchIndex];
                var nodeWorldTransform = nodes[nodeBatch.NodeIndex].WorldTransform;

                for ( int i = 0; i < nodeBatch.Positions.Length; i++ )
                {
                    var position = new Vector3( nodeBatch.Positions[i].X, nodeBatch.Positions[i].Y,
                                                nodeBatch.Positions[i].Z );
                    var weight = nodeBatch.Positions[i].W;
                    var weightedNodeWorldTransform = nodeWorldTransform * weight;
                    var weightedWorldPosition = Vector3.Transform( position, weightedNodeWorldTransform );
                    positions[i] += weightedWorldPosition;
                    normals[i] += Vector3.TransformNormal( nodeBatch.Normals[i], weightedNodeWorldTransform );
                    weights[i][nodeBatchIndex] = new NodeWeight( nodeBatch.NodeIndex, weight );
                }
            }

            return (positions, normals, weights);
        }

        void IBinarySerializable.Read( EndianBinaryReader reader, object context )
        {
            var usedNodeIndices = ( short[] )context;

            MeshType2NodeBatchContext nodeBatchContext = null;
            for ( var i = 0; i < usedNodeIndices.Length; i++ )
            {
                nodeBatchContext = new MeshType2NodeBatchContext( usedNodeIndices[i], i == usedNodeIndices.Length - 1 );
                NodeBatches.Add( reader.ReadObject<MeshType2NodeBatch>( nodeBatchContext ) );
            }

            Triangles = nodeBatchContext.Triangles;
            TexCoords = nodeBatchContext.TexCoords;
            Colors = nodeBatchContext.Colors;
        }

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context )
        {
            var nodeBatchContext = new MeshType2NodeBatchContext( Triangles, TexCoords, Colors, (VifCodeStreamBuilder)context );

            for ( var i = 0; i < NodeBatches.Count; i++ )
            {
                nodeBatchContext.Last = i == NodeBatches.Count - 1;
                writer.WriteObject( NodeBatches[i], nodeBatchContext );
            }
        }
    }
}