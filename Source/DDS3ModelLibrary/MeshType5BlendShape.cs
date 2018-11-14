using System.Numerics;

namespace DDS3ModelLibrary
{
    public class MeshType5BlendShape
    {
        public Vector3[] Positions { get; set; }

        public Vector3[] Normals { get; set; }

        public Vector2[] TexCoords { get; set; }

        public MeshType5BlendShape()
        {
        }

        public MeshType5BlendShape( Vector3[] positions, Vector3[] normals, Vector2[] texCoords )
        {
            Positions = positions;
            Normals = normals;
            TexCoords = texCoords;
        }
    }
}