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
        public Vector2[][] TexCoords { get; set; } = new Vector2[2][];
        public Color[]     Colors    { get; set; }
        public VifCodeStreamBuilder Vif { get; }

        public MeshType2NodeBatchContext( short nodeIndex, bool last )
        {
            NodeIndex = nodeIndex;
            Last      = last;
        }

        public MeshType2NodeBatchContext( Triangle[] triangles, Vector2[][] texCoords, Color[] colors, VifCodeStreamBuilder vif )
        {
            Triangles = triangles;
            TexCoords = texCoords;
            Colors = colors;
            Vif = vif;
        }
    }
}