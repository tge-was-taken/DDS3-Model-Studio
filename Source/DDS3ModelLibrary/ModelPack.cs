using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.Modeling;
using DDS3ModelLibrary.Modeling.Utilities;
using DDS3ModelLibrary.Primitives;
using DDS3ModelLibrary.Texturing;
using Color = DDS3ModelLibrary.Primitives.Color;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace DDS3ModelLibrary
{
    public class ModelPack : IBinarySerializable
    {
        private const int BATCH_VERTEX_LIMIT = 24;
        private const int MESH_WEIGHT_LIMIT = 4;

        BinarySourceInfo IBinarySerializable.SourceInfo { get; set; }

        public ModelPackInfo Info { get; set; }

        public TexturePack TexturePack { get; set; }

        public List<Resource> Effects { get; }

        public List<Model> Models { get; }

        public List<Resource> AnimationPacks { get; }

        public ModelPack()
        {
            Info = new ModelPackInfo();
            TexturePack = null;
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

        public void Save( string filePath )
        {
            using ( var writer = new EndianBinaryWriter( new MemoryStream(), Endianness.Little ) )
            {
                Write( writer );
                using ( var fileStream = File.Create( filePath ) )
                {
                    writer.BaseStream.Position = 0;
                    writer.BaseStream.CopyTo( fileStream );
                }
            }
        }

        public void Save( Stream stream, bool leaveOpen = true )
        {
            using ( var writer = new EndianBinaryWriter( stream, leaveOpen, Endianness.Little ) )
                Write( writer );
        }

        public MemoryStream Save()
        {
            var stream = new MemoryStream();
            Save( stream );
            stream.Position = 0;
            return stream;
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

                var bitmap = fileExists ? TextureImportHelper.ImportAsBitmap( path ) : new Bitmap( 32, 32 );
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

            if ( !hasTexture )
                mesh.Flags &= ~MeshFlags.TexCoord;

            // Get vertex weights
            var vertexWeights = new List<List<(short NodeIndex, float Weight)>>();
            var nodeScores = new Dictionary<short, float>();
            for ( int i = 0; i < aiMesh.VertexCount; i++ )
            {
                var weights = new List<(short, float)>();
                foreach ( var aiBone in aiMesh.Bones )
                {
                    foreach ( var aiVertexWeight in aiBone.VertexWeights )
                    {
                        if ( aiVertexWeight.VertexID == i )
                        {
                            var nodeIndex = nodes.FindIndex( x => x.Name == aiBone.Name );
                            Debug.Assert( nodeIndex != -1 );
                            weights.Add( (( short )nodeIndex, aiVertexWeight.Weight) );

                            if ( !nodeScores.ContainsKey( ( short ) nodeIndex ) )
                                nodeScores[ ( short ) nodeIndex ] = 0;

                            nodeScores[(short)nodeIndex] += aiVertexWeight.Weight;
                        }
                    }
                }

                vertexWeights.Add( weights );
            }

            // Find unique node indices used by the mesh (max 4 per mesh!)
            var usedNodeIndices = vertexWeights.SelectMany( x => x.Select( y => y.NodeIndex ) ).Distinct().OrderByDescending( x => nodeScores[ x ] )
                                               .ToList();

            Trace.Assert( usedNodeIndices.Count > 1 );
            if ( usedNodeIndices.Count > 4 )
                RemoveExcessiveNodeInfluences( aiMesh.VertexCount, usedNodeIndices, vertexWeights, nodeScores );

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
                        var addedAny = false;
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

                            if ( batch.NodeBatches.Count == 0 )
                            {
                                batch.TexCoords[nodeBatchVertexIndex] = aiMesh.HasTextureCoords( 0 )
                                    ? aiMesh.TextureCoordinateChannels[0][vertexIndex].FromAssimpAsVector2()
                                    : new Vector2();
                            }

                            addedAny = true;
                        }

                        if ( !addedAny && batch.NodeBatches.Count == 0 )
                        {
                            batch.TexCoords[nodeBatchVertexIndex] = aiMesh.HasTextureCoords( 0 )
                                ? aiMesh.TextureCoordinateChannels[0][vertexIndex].FromAssimpAsVector2()
                                : new Vector2();
                        }
                    }

                    batch.NodeBatches.Add( nodeBatch );
                }

                Debug.Assert( batch.NodeBatches.Count > 0 );
                Debug.Assert( batch.NodeBatches.TrueForAll( x => x.VertexCount == batch.NodeBatches[0].VertexCount ) );

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
            aiContext.SetConfig( new Assimp.Configs.FBXPreservePivotsConfig( false ) );
            aiContext.SetConfig( new Assimp.Configs.MaxBoneCountConfig( 4 ) );
            aiContext.SetConfig( new Assimp.Configs.VertexBoneWeightLimitConfig( 4 ) );
            aiContext.SetConfig( new Assimp.Configs.MeshVertexLimitConfig( 100 ) );
            var aiScene = aiContext.ImportFile( filePath, Assimp.PostProcessSteps.JoinIdenticalVertices |
                                                          Assimp.PostProcessSteps.FindDegenerates |
                                                          Assimp.PostProcessSteps.FindInvalidData | Assimp.PostProcessSteps.FlipUVs |
                                                          Assimp.PostProcessSteps.ImproveCacheLocality |
                                                          Assimp.PostProcessSteps.Triangulate | Assimp.PostProcessSteps.LimitBoneWeights // | Assimp.PostProcessSteps.SplitLargeMeshes // | Assimp.PostProcessSteps.SplitByBoneCount
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
                var isTextured = aiMaterial.HasTextureDiffuse;
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
                    if ( !aiMaterial.HasTextureDiffuse )
                        material = Material.FromPreset( presetId );
                    else if ( !hasOverlay )
                        material = Material.FromPreset( presetId, textureId );
                    else
                        material = Material.FromPreset( presetId, textureId, overlayMaskId, overlayTextureId );
                }
                else
                {
                    if ( !aiMaterial.HasTextureDiffuse )
                        material = Material.CreateDefault();
                    else if ( !hasOverlay )
                        material = Material.CreateDefault( textureId );
                    else
                        material = Material.CreateDefault( textureId, overlayMaskId, overlayTextureId );
                }

                model.Materials.Add( material );
            }

            if ( TexturePack.Count == 0 )
                TexturePack = null;

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
                            aiMeshes.AddRange( AssimpHelper.SplitMesh( aiScene, aiMesh, MESH_WEIGHT_LIMIT ) );
                        }
                        else
                        {
                            aiMeshes.Add( aiMesh );
                        }
                    }

                    foreach ( var aiMesh in aiMeshes )
                    {
                        var node = AssimpHelper.DetermineBestTargetNode( aiMesh, aiNode, name => model.Nodes.Find( x => x.Name == name ),
                                                                         model.Nodes[ 1 ] );
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
                kvp.Key.BoundingBox = BoundingBox.Calculate( kvp.Value );
                Debug.Assert( kvp.Key.Geometry != null );
            }
        }

        private void Read( EndianBinaryReader reader )
        {
            var foundEnd = false;
            while ( !foundEnd && reader.Position < reader.BaseStream.Length )
            {
                var start  = reader.Position;
                var header = reader.ReadObject<ResourceHeader>();
                var end    = AlignmentHelper.Align( start + header.FileSize, 64 );

                switch ( header.Identifier )
                {
                    case ResourceIdentifier.ModelPackInfo:
                        Info = reader.ReadObject<ModelPackInfo>( header );
                        break;

                    case ResourceIdentifier.Particle:
                    case ResourceIdentifier.Video:
                        Effects.Add( reader.ReadObject<BinaryResource>( header ) );
                        break;

                    case ResourceIdentifier.TexturePack:
                        TexturePack = reader.ReadObject<TexturePack>( header );
                        break;

                    case ResourceIdentifier.Model:
                        Models.Add( reader.ReadObject<Model>( header ) );
                        break;

                    case ResourceIdentifier.AnimationPack:
                        AnimationPacks.Add( reader.ReadObject<BinaryResource>( header ) );
                        break;

                    case ResourceIdentifier.ModelPackEnd:
                        foundEnd = true;
                        break;

                    default:
                        throw new UnexpectedDataException( $"Unexpected '{header.Identifier}' chunk in PB file" );
                }

                // Some files have broken offsets & filesize in their texture pack (f021_aljira.PB)
                if ( header.Identifier != ResourceIdentifier.TexturePack )
                    reader.SeekBegin( end );
            }
        }

        private void Write( EndianBinaryWriter writer )
        {
            if ( Info != null )
            {
                // Some files don't have this
                writer.WriteObject( Info, this );
            }

            writer.WriteObjects( Effects );

            if ( TexturePack != null )
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

        void IBinarySerializable.Read( EndianBinaryReader reader, object context ) => Read( reader );

        void IBinarySerializable.Write( EndianBinaryWriter writer, object context ) => Write( writer );
    }
}
