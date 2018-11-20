using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static List<Assimp.Node> FindMeshNodes( this Assimp.Node aiSceneRootNode )
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

        public static T DetermineBestTargetNode<T>( Assimp.Mesh aiMesh, Assimp.Node aiNode, Func<string,T> nodeSearchFunc, T fallback )
        {
            if ( aiMesh.BoneCount > 1 )
            {
                // Select node to which the mesh is weighted most
                var boneWeightCoverage = CalculateBoneWeightCoverage( aiMesh );
                var maxCoverage        = boneWeightCoverage.Max( x => x.Coverage );
                var bestTargetBone     = boneWeightCoverage.First( x => x.Coverage == maxCoverage ).Bone;
                return nodeSearchFunc( bestTargetBone.Name );
            }
            else if ( aiMesh.BoneCount == 1 )
            {
                // Use our only bone as the target node
                return nodeSearchFunc( aiMesh.Bones[ 0 ].Name );
            }
            else
            {
                // Try to find a parent of the mesh's ainode that exists within the existing hierarchy
                var aiNodeParent = aiNode.Parent;
                while ( aiNodeParent != null )
                {
                    var nodeParent = nodeSearchFunc( aiNodeParent.Name );
                    if ( nodeParent != null )
                        return nodeParent;

                    aiNodeParent = aiNodeParent.Parent;
                }

                // Return fallback
                return fallback;
            }
        }

        public static List<(float Coverage, Assimp.Bone Bone)> CalculateBoneWeightCoverage( Assimp.Mesh aiMesh )
        {
            var boneScores = new List<(float Coverage, Assimp.Bone Bone)>();

            foreach ( var bone in aiMesh.Bones )
            {
                float weightTotal = 0;
                foreach ( var vertexWeight in bone.VertexWeights )
                    weightTotal += vertexWeight.Weight;

                float weightCoverage = ( weightTotal / aiMesh.VertexCount );
                boneScores.Add( (weightCoverage, bone) );
            }

            return boneScores;
        }

        public static List<Assimp.Mesh> SplitMeshByBoneCount( Assimp.Scene scene, Assimp.Mesh mesh, int maxNodeCount )
        {
            var vertexWeights = new List<(Assimp.Node, float)>[mesh.VertexCount];
            var missingVertexWeights = new List<int>();
            for ( int i = 0; i < mesh.VertexCount; i++ )
            {
                var weights = new List<(Assimp.Node, float)>();
                foreach ( var bone in mesh.Bones )
                {
                    foreach ( var vertexWeight in bone.VertexWeights )
                    {
                        if ( vertexWeight.VertexID == i )
                        {
                            var node = scene.RootNode.FindNode( bone.Name );
                            Debug.Assert( node != null );
                            weights.Add( (node, vertexWeight.Weight) );
                        }
                    }
                }

                if ( weights.Count == 0 )
                {
                    weights = null;
                    missingVertexWeights.Add( i );
                    continue;
                }

                Debug.Assert( weights.Count > 0 );
                vertexWeights[i] = weights;
            }

            // Resolve vertices without any weights by finding the closest vertex next to it that does, and taking its weights
            foreach ( var i in missingVertexWeights )
            {
                var position = mesh.Vertices[i];
                var tolerance = 0.001f;
                List<(Assimp.Node, float)> weights = null;

                while ( weights == null )
                {
                    for ( var j = 0; j < mesh.Vertices.Count; j++ )
                    {
                        if ( missingVertexWeights.Contains( j ) )
                            continue;

                        // ＤＥＬＴＡ
                        var otherPosition = mesh.Vertices[j];
                        var delta = position - otherPosition;
                        if ( ( delta.X < 0 ? delta.X >= -tolerance : delta.X <= tolerance ) &&
                             ( delta.Y < 0 ? delta.Y >= -tolerance : delta.Y <= tolerance ) &&
                             ( delta.Z < 0 ? delta.Z >= -tolerance : delta.Z <= tolerance ) )
                        {
                            weights = vertexWeights[j];
                            break;
                        }
                    }

                    tolerance *= 2f;
                }

                vertexWeights[i] = weights;
            }

            Debug.Assert( vertexWeights.All( x => x != null ) );

            var subMeshes = new List<Assimp.Mesh>();
            var meshFaces = mesh.Faces.ToList();
            while ( meshFaces.Count > 0 )
            {
                var usedNodes = new HashSet<Assimp.Node>();
                var faces = new List<Assimp.Face>();

                // Get faces that fit inside the new mesh
                for ( var faceIndex = 0; faceIndex < meshFaces.Count; faceIndex++ )
                {
                    var face = meshFaces[faceIndex];
                    var faceUsedNodes = face.Indices.SelectMany( y => vertexWeights[y].Select( z => z.Item1 ) ).ToList();
                    Debug.Assert( faceUsedNodes.Count <= maxNodeCount, "faceUsedNodes.Count <= maxNodeCount" ); // would need averaging..
                    var faceUniqueUsedNodeCount = faceUsedNodes.Count( x => !usedNodes.Contains( x ) );
                    if ( ( usedNodes.Count + faceUniqueUsedNodeCount ) > maxNodeCount )
                    {
                        // Skip
                        continue;
                    }

                    // It does fit ＼(^o^)／
                    faces.Add( face );
                    foreach ( var node in faceUsedNodes )
                        usedNodes.Add( node );

                    Debug.Assert( usedNodes.Count <= maxNodeCount );
                }

                // Remove the faces we claimed from the pool
                foreach ( var face in faces )
                    meshFaces.Remove( face );

                // Build submesh
                var subMesh = new Assimp.Mesh()
                {
                    MaterialIndex = mesh.MaterialIndex,
                    MorphMethod = mesh.MorphMethod,
                    Name = mesh.Name + $"_submesh{subMeshes.Count}",
                    PrimitiveType = mesh.PrimitiveType,
                };
                var vertexCache = new List<(Assimp.Vector3D Position, Assimp.Vector3D Normal, Assimp.Vector3D TexCoord, List<(Assimp.Node, float)> Weights)>();
                foreach ( var face in faces )
                {
                    var newFace = new Assimp.Face();

                    foreach ( var index in face.Indices )
                    {
                        var position = mesh.Vertices[index];
                        var normal = mesh.Normals[index];
                        var texCoord = mesh.TextureCoordinateChannels[0][index];
                        var weights = vertexWeights[index];

                        // Try to find a duplicate of this vertex
                        var newIndex = vertexCache.FindIndex( x => x.Position == position && x.Normal == normal && x.TexCoord == texCoord &&
                                                                     x.Weights.SequenceEqual( weights ) );
                        if ( newIndex == -1 )
                        {
                            // This vertex is unique, add it to cache
                            newIndex = vertexCache.Count;

                            for ( int i = 0; i < weights.Count; i++ )
                                Debug.Assert( usedNodes.Contains( weights[i].Item1 ) );

                            vertexCache.Add( (position, normal, texCoord, weights) );
                        }

                        newFace.Indices.Add( newIndex );
                    }

                    subMesh.Faces.Add( newFace );
                }

                int vertexIndex = 0;
                foreach ( (Assimp.Vector3D position, Assimp.Vector3D normal, Assimp.Vector3D texCoord, List<(Assimp.Node, float)> weights) in vertexCache )
                {
                    // Split the vertex data from the cache
                    subMesh.Vertices.Add( position );
                    subMesh.Normals.Add( normal );
                    subMesh.TextureCoordinateChannels[0].Add( texCoord );

                    foreach ( (Assimp.Node node, float weight) in weights )
                    {
                        var bone = subMesh.Bones.FirstOrDefault( x => x.Name == node.Name );
                        if ( bone == null )
                        {
                            var originalBone = mesh.Bones.Find( x => x.Name == node.Name );
                            bone = new Assimp.Bone()
                            {
                                Name = originalBone.Name,
                                OffsetMatrix = originalBone.OffsetMatrix
                            };
                            subMesh.Bones.Add( bone );
                            Debug.Assert( subMesh.Bones.Count <= maxNodeCount );
                        }

                        bone.VertexWeights.Add( new Assimp.VertexWeight( vertexIndex, weight ) );
                    }

                    ++vertexIndex;
                }

                var vertexIndices = Enumerable.Range( 0, subMesh.VertexCount ).ToList();
                foreach ( var bone in subMesh.Bones )
                {
                    foreach ( var vertexWeight in bone.VertexWeights )
                        vertexIndices.Remove( vertexWeight.VertexID );
                }

                Debug.Assert( vertexIndices.Count == 0 );

                subMeshes.Add( subMesh );
            }

            return subMeshes;
        }

        public static List<Assimp.Mesh> SplitMeshByVertexCount( Assimp.Mesh mesh, int vertexCount )
        {
            // todo
            return null;
        }
    }
}
