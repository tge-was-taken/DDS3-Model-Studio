using System.Numerics;
using DDS3ModelLibrary.Primitives;
using DDS3ModelLibrary.PS2.VIF;

namespace DDS3ModelLibrary
{
    internal class MeshType2NodeBatchContext
    {
        public short       NodeIndex { get; }
        public bool        Last      { get; set; }
        public Triangle[]  Triangles { get; set; }
        public Vector2[] TexCoords { get; set; }
        public Vector2[] TexCoords2 { get; set; }
        public Color[]     Colors    { get; set; }
        public VifCodeStreamBuilder Vif { get; }

        public MeshType2NodeBatchContext( short nodeIndex, bool last )
        {
            NodeIndex = nodeIndex;
            Last      = last;
        }

        public MeshType2NodeBatchContext( Triangle[] triangles, Vector2[] texCoords, Vector2[] texCoords2, Color[] colors, VifCodeStreamBuilder vif )
        {
            Triangles = triangles;
            TexCoords = texCoords;
            TexCoords2 = texCoords2;
            Colors = colors;
            Vif = vif;
        }
    }
}