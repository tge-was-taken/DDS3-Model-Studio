using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DDS3ModelLibrary.Primitives;

namespace DDS3ModelLibrary.Modeling.Utilities
{
    public static class AssimpHelper
    {
        public static Assimp.Vector3D ToAssimp( this Vector3 value )
        {
            return new Assimp.Vector3D( value.X, value.Y, value.Z );
        }

        public static Assimp.Vector3D ToAssimp( this Vector2 value )
        {
            return new Assimp.Vector3D( value.X, value.Y, 0 );
        }

        public static Assimp.Color4D ToAssimp( this Vector4 value )
        {
            return new Assimp.Color4D( value.X, value.Y, value.Z, value.W );
        }

        public static Assimp.Matrix4x4 ToAssimp( this Matrix4x4 matrix )
        {
            return new Assimp.Matrix4x4( matrix.M11, matrix.M21, matrix.M31, matrix.M41,
                                         matrix.M12, matrix.M22, matrix.M32, matrix.M42,
                                         matrix.M13, matrix.M23, matrix.M33, matrix.M43,
                                         matrix.M14, matrix.M24, matrix.M34, matrix.M44 );
        }

        public static Assimp.Color4D ToAssimp( this Color value )
        {
            return new Assimp.Color4D( value.R / 255f,
                                       value.G / 255f,
                                       value.B / 255f,
                                       value.A / 255f );
        }

        public static Assimp.Scene CreateDefaultScene()
        {
            var aiScene = new Assimp.Scene { RootNode = new Assimp.Node( "RootNode" ) };
            return aiScene;
        }

        public static void ExportCollada( this Assimp.Scene aiScene, string path )
        {
            using ( var aiContext = new Assimp.AssimpContext() )
                aiContext.ExportFile( aiScene, path, "collada", Assimp.PostProcessSteps.JoinIdenticalVertices | Assimp.PostProcessSteps.FlipUVs | Assimp.PostProcessSteps.GenerateSmoothNormals );
        }

        public static Color FromAssimp( this Assimp.Color4D value )
        {
            return new Color( ( byte )( value.R * 255f ),
                              ( byte )( value.G * 255f ),
                              ( byte )( value.B * 255f ),
                              ( byte )( value.A * 255f ) );
        }

        public static Matrix4x4 FromAssimp( this Assimp.Matrix4x4 matrix )
        {
            return new Matrix4x4( matrix.A1, matrix.B1, matrix.C1, matrix.D1,
                                  matrix.A2, matrix.B2, matrix.C2, matrix.D2,
                                  matrix.A3, matrix.B3, matrix.C3, matrix.D3,
                                  matrix.A4, matrix.B4, matrix.C4, matrix.D4 );
        }

        public static Assimp.Scene ImportScene( string path )
        {
            using ( var aiContext = new Assimp.AssimpContext() )
            {
                aiContext.SetConfig( new Assimp.Configs.VertexBoneWeightLimitConfig( 4 ) );
                aiContext.SetConfig( new Assimp.Configs.FBXPreservePivotsConfig( false ) );
                return aiContext.ImportFile( path,
                                             Assimp.PostProcessSteps.FindDegenerates | Assimp.PostProcessSteps.FindInvalidData |
                                             Assimp.PostProcessSteps.FlipUVs | Assimp.PostProcessSteps.ImproveCacheLocality |
                                             Assimp.PostProcessSteps.JoinIdenticalVertices | Assimp.PostProcessSteps.LimitBoneWeights |
                                             Assimp.PostProcessSteps.SplitByBoneCount | Assimp.PostProcessSteps.Triangulate |
                                             Assimp.PostProcessSteps.ValidateDataStructure | Assimp.PostProcessSteps.GenerateUVCoords |
                                             Assimp.PostProcessSteps.GenerateSmoothNormals );
            }
        }

        public static Vector3 FromAssimp( this Assimp.Vector3D value )
        {
            return new Vector3( value.X, value.Y, value.Z );
        }

        public static Vector2 FromAssimpAsVector2( this Assimp.Vector3D value )
        {
            return new Vector2( value.X, value.Y );
        }

        public static Quaternion FromAssimp( this Assimp.Quaternion value )
        {
            return new Quaternion( value.X, value.Y, value.Z, value.W );
        }


        public static Matrix4x4 CalculateWorldTransform( this Assimp.Node node )
        {
            Assimp.Matrix4x4 CalculateWorldTransformInternal( Assimp.Node currentNode )
            {
                var transform = currentNode.Transform;
                if ( currentNode.Parent != null )
                    transform *= CalculateWorldTransformInternal( currentNode.Parent );

                return transform;
            }

            return FromAssimp( CalculateWorldTransformInternal( node ) );
        }

        public static Assimp.Node FindHierarchyRootNode( this Assimp.Node aiSceneRootNode )
        {
            // Pretty naiive for now.
            return aiSceneRootNode.Children.Single( x => !x.HasMeshes );
        }

        private static List<Assimp.Node> FindMeshNodes( this Assimp.Node aiSceneRootNode )
        {
            var meshNodes = new List<Assimp.Node>();

            void FindMeshNodesRecursively( Assimp.Node aiParentNode )
            {
                foreach ( var aiNode in aiParentNode.Children )
                {
                    if ( aiNode.HasMeshes )
                        meshNodes.Add( aiNode );
                    else
                        FindMeshNodesRecursively( aiNode );
                }
            }

            FindMeshNodesRecursively( aiSceneRootNode );

            return meshNodes;
        }


    }
}
