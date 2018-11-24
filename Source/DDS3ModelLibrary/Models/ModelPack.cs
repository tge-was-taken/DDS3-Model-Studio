using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using DDS3ModelLibrary.IO;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.Materials;
using DDS3ModelLibrary.Models.Processing;
using DDS3ModelLibrary.Models.Utilities;
using DDS3ModelLibrary.Textures;
using DDS3ModelLibrary.Textures.Utilities;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace DDS3ModelLibrary.Models
{
    public sealed class ModelPack : AbstractResource<object>
    {
        private const int BATCH_VERTEX_LIMIT = 24;
        private const int MESH_WEIGHT_LIMIT = 3;

        public ModelPackInfo Info { get; set; }

        public TexturePack TexturePack { get; set; }

        public List<Resource> Effects { get; }

        public List<Model> Models { get; }

        public List<Resource> AnimationPacks { get; }

        public ModelPack()
        {
            Info = new ModelPackInfo();
            TexturePack = new TexturePack();
            Effects = new List<Resource>();
            Models = new List<Model>();
            AnimationPacks = new List<Resource>();
        }

        public ModelPack( string filePath ) : this()
        {
            using ( var reader = new EndianBinaryReader( new MemoryStream( File.ReadAllBytes( filePath ) ), filePath, Endianness.Little ) )
                Read( reader );
        }

        public ModelPack( Stream stream, bool leaveOpen = false ) : this()
        {
            using ( var reader = new EndianBinaryReader( stream, leaveOpen, Endianness.Little ) )
                Read( reader );
        }

        private static int AddTexture( string baseDirectory, string filePath, Dictionary<string, int> textureLookup, TexturePack texturePack )
        {
            if ( !textureLookup.TryGetValue( filePath, out var textureId ) )
            {
                var fileExists = true;
                var path       = filePath;

                if ( !File.Exists( path ) )
                {
                    // Assume it's a relative path
                    path = Path.Combine( baseDirectory, path );
                    if ( !File.Exists( path ) )
                        fileExists = false;
                }

                var bitmap = fileExists ? TextureImportHelper.ImportBitmap( path ) : new Bitmap( 32, 32 );
                var scale = 1;
                if ( scale != 1 )
                    bitmap = new Bitmap( bitmap, new Size( bitmap.Width / scale, bitmap.Height / scale ) );
                var name    = Path.GetFileNameWithoutExtension( path );
                var texture = new Texture( bitmap, PS2.GS.GSPixelFormat.PSMT8, name );
                textureId = texturePack.Count;
                texturePack.Add( texture );
                textureLookup[filePath] = textureId;
            }

            return textureId;
        }

        private static void RemoveExcessiveNodeInfluences( int vertexCount, List<short> usedNodeIndices, List<List<(short NodeIndex, float Weight)>> vertexWeights, Dictionary<short, float> nodeScores )
        {
            var excessiveNodeCount = usedNodeIndices.Count - MESH_WEIGHT_LIMIT;
            var maxScore = vertexCount;

            for ( int i = 4; i < ( 4 + excessiveNodeCount ); i++ )
            {
                foreach ( var weights in vertexWeights )
                {
                    if ( weights.Any( x => x.NodeIndex == usedNodeIndices[i] ) )
                    {
                        var excessiveWeight = weights.First( x => x.NodeIndex == usedNodeIndices[i] );
                        weights.Remove( excessiveWeight );

                        for ( int j = 0; j < 4; j++ )
                        {
                            var nodeIndex = usedNodeIndices[j];
                            var weight = 0f;
                            var index = -1;

                            // Find existing weight
                            for ( int k = 0; k < weights.Count; k++ )
                            {
                                if ( weights[k].NodeIndex == nodeIndex )
                                {
                                    weight = weights[k].Weight;
                                    index = k;
                                    break;
                                }
                            }

                            var newWeight = weight + excessiveWeight.Weight * ( nodeScores[nodeIndex] / ( float )maxScore );

                            if ( index != -1 )
                                weights[index] = (nodeIndex, newWeight);
                            else
                                weights.Add( (nodeIndex, newWeight) );
                        }
                    }

                    var weightSum = weights.Sum( x => x.Weight );
                    if ( weightSum < 1f )
                    {
                        var remainder = 1f - weightSum;
                        for ( var k = 0; k < weights.Count; k++ )
                        {
                            weights[k] = (weights[k].NodeIndex, weights[k].Weight + ( remainder / 4f ));
                        }
                    }

                    //Debug.Assert( weightSum >= 0.998f && weightSum <= 1.002f );
                }
            }

            usedNodeIndices.RemoveRange( 4, excessiveNodeCount );
        }

        private static MeshType1 ConvertToMeshType1( Assimp.Scene aiScene, Assimp.Mesh aiMesh, bool hasTexture, Matrix4x4 aiNodeWorldTransform, List<Vector3> localPositions, ref Matrix4x4 nodeInvWorldTransform )
        {
            var mesh = new MeshType1
            {
                MaterialIndex = ( short )aiMesh.MaterialIndex,
            };

            var aiBatchMeshes = AssimpHelper.SplitMeshByVertexCount( aiMesh, BATCH_VERTEX_LIMIT );
            foreach ( var aiBatchMesh in aiBatchMeshes )
            {
                var batch = new MeshType1Batch
                {
                    Triangles = aiBatchMesh
                                .Faces.Select( x => new Triangle( ( ushort ) x.Indices[ 0 ], ( ushort ) x.Indices[ 1 ],
                                                                  ( ushort ) ( x.IndexCount > 2 ? x.Indices[ 2 ] : x.Indices[ 1 ] ) ) )
                                .ToArray(),
                };

                // Convert positions
                batch.Positions = new Vector3[aiBatchMesh.VertexCount];
                if ( aiBatchMesh.HasVertices )
                {
                    for ( int j = 0; j < batch.Positions.Length; j++ )
                    {
                        var position = aiBatchMesh.Vertices[j].FromAssimp();
                        var worldPosition = Vector3.Transform( position, aiNodeWorldTransform );
                        var localPosition = Vector3.Transform( worldPosition, nodeInvWorldTransform );
                        batch.Positions[j] = localPosition;
                        localPositions.Add( localPosition );
                    }
                }

                // Convert normals
                if ( aiBatchMesh.HasNormals )
                {
                    batch.Normals = new Vector3[aiBatchMesh.VertexCount];
                    for ( int j = 0; j < batch.Normals.Length; j++ )
                    {
                        var normal = aiBatchMesh.Normals[j].FromAssimp();
                        var worldNormal = Vector3.Normalize( Vector3.TransformNormal( normal, aiNodeWorldTransform ) );
                        var localNormal = Vector3.Normalize( Vector3.TransformNormal( worldNormal, nodeInvWorldTransform ) );
                        batch.Normals[j] = localNormal;
                    }
                }

                // Convert texture coords
                if ( hasTexture && aiBatchMesh.HasTextureCoords( 0 ) )
                {
                    batch.TexCoords = new Vector2[aiBatchMesh.VertexCount];
                    for ( int j = 0; j < batch.TexCoords.Length; j++ )
                        batch.TexCoords[j] = aiBatchMesh.TextureCoordinateChannels[0][j].FromAssimpAsVector2();
                }

                if ( hasTexture && aiBatchMesh.HasTextureCoords( 1 ) )
                {
                    batch.TexCoords2 = new Vector2[aiBatchMesh.VertexCount];
                    for ( int j = 0; j < batch.TexCoords2.Length; j++ )
                        batch.TexCoords2[j] = aiBatchMesh.TextureCoordinateChannels[1][j].FromAssimpAsVector2();
                }

                if ( false && aiBatchMesh.HasVertexColors( 0 ) )
                {
                    batch.Colors = new Color[aiBatchMesh.VertexCount];
                    for ( int j = 0; j < batch.Colors.Length; j++ )
                        batch.Colors[j] = aiBatchMesh.VertexColorChannels[0][j].FromAssimp();
                }
                else if ( false )
                {
                    batch.Colors = new Color[aiBatchMesh.VertexCount];
                    for ( int i = 0; i < batch.Colors.Length; i++ )
                        batch.Colors[i] = Color.White;
                }

                // Add batch to mesh
                mesh.Batches.Add( batch );
            }

            return mesh;
        }

        private static MeshType7 ConvertToMeshType7( Assimp.Mesh aiMesh, bool hasTexture, List<Node> nodes, Matrix4x4 aiNodeWorldTransform, ref Matrix4x4 nodeInvWorldTransform, List<Vector3> localPositions )
        {
            var mesh = new MeshType7
            {
                MaterialIndex = ( short ) aiMesh.MaterialIndex,
                Triangles = aiMesh
                            .Faces.Select( x => new Triangle( ( ushort ) x.Indices[ 0 ], ( ushort ) x.Indices[ 1 ],
                                                              ( ushort ) ( x.IndexCount > 2 ? x.Indices[ 2 ] : x.Indices[ 1 ] ) ) )
                            .ToArray()
            };

            // TODO: texcoord generates artifacts, MESH_WEIGHT_LIMIT of 4 causes artifacts

            mesh.Flags = MeshFlags.Weights | MeshFlags.Normal | MeshFlags.TexCoord;

            if ( !hasTexture )
                mesh.Flags &= ~MeshFlags.TexCoord;

            var aiVertexWeights = aiMesh.GetVertexWeights();
            var vertexWeights = new List<(short NodeIndex, float Weight)>[aiVertexWeights.Length];
            for ( int i = 0; i < aiVertexWeights.Length; i++ )
            {
                var weights = new List<(short, float)>();
                foreach ( (Assimp.Bone bone, float weight) in aiVertexWeights[i] )
                {
                    var nodeIndex = nodes.FindIndex( x => x.Name == bone.Name );
                    Debug.Assert( nodeIndex != -1 );
                    weights.Add( (( short )nodeIndex, weight) );
                }

                vertexWeights[i] = weights;
            }

            var usedNodeIndices = vertexWeights.SelectMany( x => x.Select( y => y.NodeIndex ) ).Distinct().ToList();
            Debug.Assert( usedNodeIndices.Count >= 1 && usedNodeIndices.Count <= 4 );

            // Start building batches
            var batchVertexBaseIndex = 0;
            while ( batchVertexBaseIndex < aiMesh.VertexCount )
            {
                var batchVertexCount = Math.Min( BATCH_VERTEX_LIMIT, aiMesh.VertexCount - batchVertexBaseIndex );
                var batch = new MeshType7Batch { TexCoords = new Vector2[batchVertexCount] };

                // req. all vertices to use the same set of node indices
                foreach ( var usedNodeIndex in usedNodeIndices )
                {
                    var usedNodeWorldTransform = nodes[usedNodeIndex].WorldTransform;
                    var usedNodeWorldTransformInv = usedNodeWorldTransform.Inverted();

                    // get all verts with this index
                    var nodeBatch = new MeshType7NodeBatch
                    {
                        NodeIndex = usedNodeIndex,
                        Positions = new Vector4[batchVertexCount],
                        Normals = new Vector3[batchVertexCount]
                    };

                    for ( int vertexIndex = batchVertexBaseIndex; vertexIndex < ( batchVertexBaseIndex + batchVertexCount ); vertexIndex++ )
                    {
                        var nodeBatchVertexIndex = vertexIndex - batchVertexBaseIndex;

                        if ( batch.NodeBatches.Count == 0 )
                        {
                            var texCoord = aiMesh.HasTextureCoords( 0 )
                                ? aiMesh.TextureCoordinateChannels[ 0 ][ vertexIndex ].FromAssimpAsVector2()
                                : new Vector2();
                            batch.TexCoords[ nodeBatchVertexIndex ] = texCoord;
                        }

                        foreach ( (short nodeIndex, float nodeWeight) in vertexWeights[vertexIndex] )
                        {
                            if ( nodeIndex != usedNodeIndex )
                                continue;

                            // Transform position and normal to model space
                            var worldPosition = Vector3.Transform( aiMesh.Vertices[vertexIndex].FromAssimp(), aiNodeWorldTransform );
                            localPositions.Add( Vector3.Transform( worldPosition, nodeInvWorldTransform ) );
                            var position = Vector3.Transform( worldPosition, usedNodeWorldTransformInv );
                            var normal = Vector3.TransformNormal( Vector3.TransformNormal( aiMesh.Normals[vertexIndex].FromAssimp(), aiNodeWorldTransform ),
                                                                  usedNodeWorldTransformInv );

                            // add the model space positions and normals to our lists
                            nodeBatch.Positions[nodeBatchVertexIndex] = new Vector4( position, nodeWeight );

                            if ( aiMesh.HasNormals )
                                nodeBatch.Normals[nodeBatchVertexIndex] = normal;
                        }
                    }

                    batch.NodeBatches.Add( nodeBatch );
                }

                Debug.Assert( batch.NodeBatches.Count > 0 );
                Debug.Assert( batch.NodeBatches.TrueForAll( x => x.VertexCount == batch.NodeBatches[0].VertexCount ) );
                Debug.Assert( batch.NodeBatches.SelectMany( x => x.Positions ).Sum( x => x.W ) == batch.VertexCount );

                mesh.Batches.Add( batch );
                batchVertexBaseIndex += batchVertexCount;
            }

            var vertexCount = mesh.VertexCount;
            Debug.Assert( mesh.Triangles.All( x => x.A < vertexCount && x.B < vertexCount && x.C < vertexCount ) );
            return mesh;
        }

        private static MeshType8 ConvertToMeshType8( Assimp.Mesh aiMesh, bool hasTexture, Matrix4x4 aiNodeWorldTransform, List<Vector3> localPositions, ref Matrix4x4 nodeInvWorldTransform )
        {
            var mesh = new MeshType8
            {
                MaterialIndex = ( short ) aiMesh.MaterialIndex,
                Triangles = aiMesh
                            .Faces.Select( x => new Triangle( ( ushort ) x.Indices[ 0 ], ( ushort ) x.Indices[ 1 ],
                                                              ( ushort ) ( x.IndexCount > 2 ? x.Indices[ 2 ] : x.Indices[ 1 ] ) ) )
                            .ToArray()
            };

            if ( !hasTexture )
                mesh.Flags &= ~MeshFlags.TexCoord;

            var processedVertexCount = 0;
            while ( processedVertexCount < aiMesh.VertexCount )
            {
                var batchVertexCount = Math.Min( BATCH_VERTEX_LIMIT, aiMesh.VertexCount - processedVertexCount );
                var batch = new MeshType8Batch { Positions = new Vector3[batchVertexCount] };

                // Convert positions
                if ( aiMesh.HasVertices )
                {
                    for ( int j = 0; j < batchVertexCount; j++ )
                    {
                        var position = aiMesh.Vertices[processedVertexCount + j].FromAssimp();
                        var worldPosition = Vector3.Transform( position, aiNodeWorldTransform );
                        var localPosition = Vector3.Transform( worldPosition, nodeInvWorldTransform );
                        batch.Positions[ j ] = localPosition;
                        localPositions.Add( localPosition );
                    }
                }

                // Convert normals
                batch.Normals = new Vector3[batchVertexCount];
                if ( aiMesh.HasNormals )
                {
                    for ( int j = 0; j < batchVertexCount; j++ )
                        batch.Normals[j] = Vector3.TransformNormal( Vector3.TransformNormal( aiMesh.Normals[processedVertexCount + j].FromAssimp(), aiNodeWorldTransform ), nodeInvWorldTransform );
                }

                // Convert texture coords
                batch.TexCoords = new Vector2[batchVertexCount];
                if ( aiMesh.HasTextureCoords( 0 ) )
                {
                    for ( int j = 0; j < batchVertexCount; j++ )
                        batch.TexCoords[j] = aiMesh.TextureCoordinateChannels[0][processedVertexCount + j].FromAssimpAsVector2();
                }

                // Add batch to mesh
                mesh.Batches.Add( batch );
                processedVertexCount += batchVertexCount;
            }

            Debug.Assert( processedVertexCount == aiMesh.VertexCount );

            return mesh;
        }

        public void Replace( string filePath )
        {
            var baseDirectory = Path.GetDirectoryName( Path.GetFullPath( filePath ) );
            var aiContext = new Assimp.AssimpContext();
        //    aiContext.SetConfig( new Assimp.Configs.FBXPreservePivotsConfig( false ) );
            aiContext.SetConfig( new Assimp.Configs.VertexBoneWeightLimitConfig( 4 ) );
            var aiScene = aiContext.ImportFile( filePath, Assimp.PostProcessSteps.JoinIdenticalVertices |
                                                          Assimp.PostProcessSteps.FindInvalidData | Assimp.PostProcessSteps.FlipUVs |
                                                          Assimp.PostProcessSteps.ImproveCacheLocality |
                                                          Assimp.PostProcessSteps.Triangulate | Assimp.PostProcessSteps.LimitBoneWeights
                                              );

            // Clear stuff we're going to replace
            var model = Models[0];   
            model.Materials.Clear();
            foreach ( var node in model.Nodes )
            {
                node.Geometry = null;
                node.BoundingBox = null;
            }

            // Convert materials and textures
            var textureLookup = new Dictionary<string, int>();
            TexturePack = new TexturePack();
            foreach ( var aiMaterial in aiScene.Materials )
            {
                var materialName = TagName.Parse( aiMaterial.Name );
                var isTextured = true && aiMaterial.HasTextureDiffuse;
                var hasOverlay = false && materialName["ovl"].Count == 2;
                int textureId = 0;
                int overlayMaskId = 0;
                int overlayTextureId = 0;

                if ( isTextured )
                {
                    textureId = AddTexture( baseDirectory, aiMaterial.TextureDiffuse.FilePath, textureLookup, TexturePack );

                    if ( hasOverlay )
                    {
                        overlayMaskId = AddTexture( baseDirectory, materialName["ovl"][ 0 ], textureLookup, TexturePack );
                        overlayTextureId = AddTexture( baseDirectory, materialName["ovl"][ 1 ], textureLookup, TexturePack );
                    }
                }

                Material material;

                if ( materialName["ps"].Count == 1 && int.TryParse( materialName["ps"][0], out var presetId ) && MaterialPresetStore.IsValidPresetId( presetId ) )
                {
                    if ( !isTextured )
                        material = Material.FromPreset( presetId );
                    else if ( !hasOverlay )
                        material = Material.FromPreset( presetId, textureId );
                    else
                        material = Material.FromPreset( presetId, textureId, overlayMaskId, overlayTextureId );
                }
                else
                {
                    if ( !isTextured )
                        material = Material.CreateDefault();
                    else if ( !hasOverlay )
                        material = Material.CreateDefault( textureId );
                    else
                        material = Material.CreateDefault( textureId, overlayMaskId, overlayTextureId );
                }

                model.Materials.Add( material );
            }

            var nodeLocalPositions = new Dictionary<Node, List<Vector3>>();

            void RecurseOverNodes( Assimp.Node aiNode, ref Matrix4x4 aiParentNodeWorldTransform )
            {
                var aiNodeWorldTransform = aiParentNodeWorldTransform * aiNode.Transform.FromAssimp();
                if ( aiNode.HasMeshes )
                {
                    var aiMeshes = new List<Assimp.Mesh>();
                    foreach ( var aiMesh in aiNode.MeshIndices.Select( x => aiScene.Meshes[x] ) )
                    {
                        if ( aiMesh.BoneCount > MESH_WEIGHT_LIMIT )
                        {
                            aiMeshes.AddRange( AssimpHelper.SplitMeshByBoneCount( aiMesh, MESH_WEIGHT_LIMIT ) );
                        }
                        else
                        {
                            aiMeshes.Add( aiMesh );
                        }
                    }

                    foreach ( var aiMesh in aiMeshes )
                    {
                        var node = AssimpHelper.DetermineBestTargetNode( aiMesh, aiNode, name => model.Nodes.Find( x => x.Name == name ),
                                                                         model.Nodes[ model.Nodes.Count > 1 ? 1 : 0 ] );
                        var nodeWorldTransform = node.WorldTransform;
                        var nodeInvWorldTransform = nodeWorldTransform.Inverted();
                        var hasTexture = model.Materials[aiMesh.MaterialIndex].TextureId != null;
                        if ( !nodeLocalPositions.TryGetValue( node, out var localPositions ) )
                            localPositions = nodeLocalPositions[ node ] = new List<Vector3>();

                        Mesh mesh;
                        if ( aiMesh.BoneCount > 1 )
                        {
                            // Weighted mesh
                            // TODO: decide between type 2 and 7?
                            Debug.Assert( aiMesh.BoneCount <= MESH_WEIGHT_LIMIT );
                            mesh = ConvertToMeshType7( aiMesh, hasTexture, model.Nodes, aiNodeWorldTransform, ref nodeInvWorldTransform,
                                                       localPositions );
                        }
                        else
                        {
                            // Unweighted mesh
                            // TODO: decide between type 1 and 8?
                            mesh = ConvertToMeshType8( aiMesh, hasTexture, aiNodeWorldTransform, localPositions,
                                                       ref nodeInvWorldTransform );
                            //mesh = ConvertToMeshType1( aiScene, aiMesh, hasTexture, aiNodeWorldTransform, localPositions,
                            //                           ref nodeInvWorldTransform );
                            //continue;
                        }

                        if ( node.Geometry == null )
                            node.Geometry = new Geometry();

                        node.Geometry.Meshes.Add( mesh );
                    }
                }

                foreach ( var aiNodeChild in aiNode.Children )
                {
                    RecurseOverNodes( aiNodeChild, ref aiNodeWorldTransform );
                }
            }

            // Traverse scene and do conversion
            var identityTransform = Matrix4x4.Identity;
            RecurseOverNodes( aiScene.RootNode, ref identityTransform );

            // Calculate bounding boxes
            foreach ( var kvp in nodeLocalPositions )
            {
                if ( kvp.Key.Geometry == null )
                    continue;

                kvp.Key.BoundingBox = BoundingBox.Calculate( kvp.Value );
                Debug.Assert( kvp.Key.Geometry != null );
            }
        }

        protected override void Read( EndianBinaryReader reader, object context = null )
        {
            Info = null;

            var foundEnd = false;
            while ( !foundEnd && reader.Position < reader.BaseStream.Length )
            {
                var start = reader.Position;
                var header = reader.ReadObject<ResourceHeader>();
                var end = AlignmentHelper.Align( start + header.FileSize, 64 );
                var resContext = new Resource.IOContext( header, false, null );

                switch ( header.Identifier )
                {
                    case ResourceIdentifier.ModelPackInfo:
                        Info = reader.ReadObject<ModelPackInfo>( resContext );
                        break;

                    case ResourceIdentifier.Particle:
                    case ResourceIdentifier.Video:
                        Effects.Add( reader.ReadObject<BinaryResource>( resContext ) );
                        break;

                    case ResourceIdentifier.TexturePack:
                        TexturePack = reader.ReadObject<TexturePack>( resContext );
                        break;

                    case ResourceIdentifier.Model:
                        Models.Add( reader.ReadObject<Model>( resContext ) );
                        break;

                    case ResourceIdentifier.MotionPack:
                        AnimationPacks.Add( reader.ReadObject<BinaryResource>( resContext ) );
                        break;

                    case ResourceIdentifier.ModelPackEnd:
                        foundEnd = true;
                        break;

                    default:
                        throw new InvalidDataException( $"Unexpected '{header.Identifier}' chunk in PB file" );
                }

                // Some files have broken offsets & filesize in their texture pack (f021_aljira.PB)
                if ( header.Identifier != ResourceIdentifier.TexturePack )
                    reader.SeekBegin( end );
            }
        }

        protected override void Write( EndianBinaryWriter writer, object context = null )
        {
            if ( Info != null )
            {
                // Some files don't have this
                writer.WriteObject( Info, new Resource.IOContext( this ) );
            }

            writer.WriteObjects( Effects );

            if ( TexturePack != null && TexturePack.Count > 0 )
                writer.WriteObject( TexturePack );

            writer.WriteObjects( Models );
            writer.WriteObjects( AnimationPacks );

            // write dummy end chunk
            writer.Write( ( int )ResourceFileType.ModelPackEnd );
            writer.Write( 16 );
            writer.Write( ( int )ResourceIdentifier.ModelPackEnd );
            writer.Write( 0 );
            writer.Align( 64 );
        }
    }
}
