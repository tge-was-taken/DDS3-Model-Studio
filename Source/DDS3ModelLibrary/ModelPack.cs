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

namespace DDS3ModelLibrary
{
    public class ModelPack : IBinarySerializable
    {
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

        public void Replace( string filePath )
        {
            var baseDirectory = Path.GetDirectoryName( Path.GetFullPath( filePath ) );
            var aiContext = new Assimp.AssimpContext();
            aiContext.SetConfig( new Assimp.Configs.FBXPreservePivotsConfig( false ) );
            aiContext.SetConfig( new Assimp.Configs.MeshVertexLimitConfig( 1000 ) );
            aiContext.SetConfig( new Assimp.Configs.MaxBoneCountConfig( 4 ) );
            var aiScene = aiContext.ImportFile( filePath, Assimp.PostProcessSteps.JoinIdenticalVertices |
                                                          Assimp.PostProcessSteps.FindDegenerates |
                                                          Assimp.PostProcessSteps.FindInvalidData | Assimp.PostProcessSteps.FlipUVs |
                                                          Assimp.PostProcessSteps.ImproveCacheLocality |
                                                          Assimp.PostProcessSteps.Triangulate | Assimp.PostProcessSteps.SplitByBoneCount
                                              );

            // Clear stuff we're going to replace
            var model = Models[0];   
            model.Materials.Clear();
            foreach ( var node in model.Nodes )
            {
                node.Geometry = null;
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

            const int BATCH_VERTEX_LIMIT = 24;
            const int MAX_WEIGHTS_PER_VERTEX = 4;

            var geometry = new Geometry();

            void RecurseOverNodes( Assimp.Node aiNode, ref Matrix4x4 aiParentNodeWorldTransform )
            {
                var aiNodeWorldTransform = aiParentNodeWorldTransform * aiNode.Transform.FromAssimp();
                if ( aiNode.HasMeshes )
                {
                    var node = model.Nodes[ 1 ];
                    var nodeWorldTransform = node.WorldTransform;
                    var nodeInvWorldTransform = nodeWorldTransform.Inverted();
                    var worldPositions = new List<Vector3>();

                    foreach ( int aiMeshIndex in aiNode.MeshIndices )
                    {
                        var aiMesh = aiScene.Meshes[ aiMeshIndex ];
                        var hasTexture = model.Materials[aiMesh.MaterialIndex].TextureId != null;

                        if ( aiMesh.BoneCount > 1 )
                        {
                            // Weighted mesh
                            // TODO: decide between type 2 and 7?
                            var mesh = new MeshType7();
                            mesh.MaterialIndex = ( short )aiMesh.MaterialIndex;
                            mesh.Triangles = aiMesh
                                             .Faces.Select( x => new Triangle( ( ushort )x.Indices[0], ( ushort )x.Indices[1],
                                                                               ( ushort )( x.IndexCount > 2 ? x.Indices[2] : x.Indices[1] ) ) )
                                             .ToArray();

                            if ( !hasTexture )
                                mesh.Flags &= ~MeshFlags.TexCoord;

                            // Get vertex weights
                            var vertexWeights = new List<List<(short NodeIndex, float Weight)>>();
                            for ( int i = 0; i < aiMesh.VertexCount; i++ )
                            {
                                var weights = new List<(short, float)>();
                                foreach ( var aiBone in aiMesh.Bones )
                                {
                                    foreach ( var aiVertexWeight in aiBone.VertexWeights )
                                    {
                                        if ( aiVertexWeight.VertexID == i )
                                        {
                                            var nodeIndex = model.Nodes.FindIndex( x => x.Name == aiBone.Name );
                                            Debug.Assert( nodeIndex != -1 );
                                            weights.Add( ( ( short )nodeIndex, aiVertexWeight.Weight ) );
                                        }
                                    }
                                }

                                vertexWeights.Add( weights );
                            }

                            // Find unique node indices used by the mesh (max 4 per mesh!)
                            var usedNodeIndices = vertexWeights.SelectMany( x => x.Select( y => y.NodeIndex ) ).Distinct().ToList();
                            if ( usedNodeIndices.Count == 1 )
                                Debugger.Break();

                            //// Calculate node index usage frequency
                            //var usedNodeIndicesFrequency = new Dictionary<int, int>();
                            //for ( int i = 0; i < usedNodeIndices.Count; i++ )
                            //    usedNodeIndicesFrequency[usedNodeIndices[i]] = 0;

                            //for ( int j = 0; j < aiMesh.VertexCount; j++ )
                            //{
                            //    foreach ( var (nodeIndex, _) in vertexWeights[j] )
                            //        ++usedNodeIndicesFrequency[nodeIndex];
                            //}

                            //// Sort used node indices by frequency
                            //usedNodeIndices = usedNodeIndices.OrderBy( x => usedNodeIndicesFrequency[x] ).ToList();

                            // Start building batches
                            var vertexIndexRemap = new Dictionary<int, int>();
                            var batchVertexBaseIndex = 0;

                            // TODO: verify output against input!

                            while ( vertexIndexRemap.Count < aiMesh.VertexCount )
                            {
                                var effectiveBatchVertexLimit = Math.Min( BATCH_VERTEX_LIMIT, aiMesh.VertexCount - vertexIndexRemap.Count );
                                var batchVertexCount = 0;
                                var batch = new MeshType7Batch();
                                var batchVertexIndexRemap = new Dictionary<int, int>();
                                var batchTexCoords = new List<Vector2>();

                                // req. all vertices to use the same set of node indices
                                for ( var i = 0; i < usedNodeIndices.Count; i++ )
                                {
                                    var usedNodeIndex = usedNodeIndices[i];
                                    var usedNodeWorldTransform = model.Nodes[usedNodeIndex].WorldTransform;
                                    var usedNodeWorldTransformInv = usedNodeWorldTransform.Inverted();

                                    // get all verts with this index
                                    var nodePositions = new List<Vector4>();
                                    var nodeNormals = new List<Vector3>();
                                    var failCount = 0;

                                    if ( batchVertexCount == 0 )
                                    {
                                        for ( int vertexIndex = 0; vertexIndex < aiMesh.VertexCount; vertexIndex++ )
                                        {
                                            // Skip this vertex if it has already been processed before
                                            if ( vertexIndexRemap.ContainsKey( vertexIndex ) )
                                                continue;

                                            foreach ( (short nodeIndex, float nodeWeight) in vertexWeights[vertexIndex] )
                                            {
                                                if ( nodeIndex != usedNodeIndex )
                                                    continue;

                                                // Transform position and normal to model space
                                                var worldPosition = Vector3.Transform( aiMesh.Vertices[ vertexIndex ].FromAssimp(), aiNodeWorldTransform );
                                                worldPositions.Add( worldPosition );
                                                var position = Vector3.Transform( worldPosition, usedNodeWorldTransformInv );
                                                //var position = worldPosition;
                                                var normal = Vector3.TransformNormal( Vector3.TransformNormal( aiMesh.Normals[vertexIndex].FromAssimp(), aiNodeWorldTransform ),
                                                                                      usedNodeWorldTransformInv );

                                                // Add entry to vertex remap, and add the model space positions and normals to our lists
                                                batchVertexIndexRemap[vertexIndex] = batchVertexBaseIndex + nodePositions.Count;
                                                nodePositions.Add( new Vector4( position, nodeWeight ) );
                                                nodeNormals.Add( normal );

                                                batchTexCoords.Add( aiMesh.TextureCoordinateChannels[ 0 ][ vertexIndex ].FromAssimpAsVector2() );

                                                // Stop looking if we've reached our vertex count
                                                if ( nodePositions.Count == effectiveBatchVertexLimit )
                                                    break;
                                            }

                                            // Ditto
                                            if ( nodePositions.Count == effectiveBatchVertexLimit )
                                                break;
                                        }

                                        batchVertexCount = nodePositions.Count;
                                        if ( batchVertexCount == 0 )
                                        {
                                            ++failCount;

                                            // Skip
                                            usedNodeIndices.Remove( usedNodeIndex );
                                            usedNodeIndices.Add( usedNodeIndex );
                                            i -= 1;
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        foreach ( var kvp in batchVertexIndexRemap )
                                        {
                                            var vertexIndex = kvp.Key;
                                            var anyAdded = false;
                                            foreach ( (short nodeIndex, float nodeWeight) in vertexWeights[vertexIndex] )
                                            {
                                                if ( nodeIndex != usedNodeIndex )
                                                    continue;

                                                // Transform position and normal to model space
                                                var worldPosition = Vector3.Transform( aiMesh.Vertices[vertexIndex].FromAssimp(), aiNodeWorldTransform );
                                                worldPositions.Add( worldPosition );
                                                var position = Vector3.Transform( worldPosition, usedNodeWorldTransformInv );
                                                var normal = Vector3.TransformNormal( Vector3.TransformNormal( aiMesh.Normals[vertexIndex].FromAssimp(), aiNodeWorldTransform ),
                                                                                      usedNodeWorldTransformInv );

                                                nodePositions.Add( new Vector4( position, nodeWeight ) );
                                                nodeNormals.Add( normal );
                                                anyAdded = true;

                                                // Stop looking if we've reached our vertex count
                                                if ( nodePositions.Count == batchVertexCount )
                                                    break;
                                            }

                                            if ( !anyAdded )
                                            {
                                                // DUMMY!
                                                nodePositions.Add( new Vector4() );
                                                nodeNormals.Add( new Vector3() );
                                            }

                                            // Ditto
                                            if ( nodePositions.Count == batchVertexCount )
                                                break;
                                        }
                                    }

                                    batch.NodeBatches.Add( new MeshType7NodeBatch
                                    {
                                        NodeIndex = usedNodeIndex,
                                        Positions = nodePositions.ToArray(),
                                        Normals = nodeNormals.ToArray()
                                    } );
                                }

                                batch.TexCoords = batchTexCoords.ToArray();
                                Debug.Assert( batch.TexCoords.Length == batchVertexCount );

                                foreach ( var i in batchVertexIndexRemap )
                                    vertexIndexRemap.Add( i.Key, i.Value );

                                Debug.Assert( batch.NodeBatches.Count > 0 );
                                Debug.Assert( batch.NodeBatches.TrueForAll( x => x.VertexCount == batch.NodeBatches[0].VertexCount ) );
                                mesh.Batches.Add( batch );
                                batchVertexBaseIndex += batchVertexCount;
                            }

                            // Remap triangle vertex indices
                            for ( var i = 0; i < mesh.Triangles.Length; i++ )
                            {
                                ref var triangle = ref mesh.Triangles[i];
                                triangle.A = ( ushort )vertexIndexRemap[mesh.Triangles[i].A];
                                triangle.B = ( ushort )vertexIndexRemap[mesh.Triangles[i].B];
                                triangle.C = ( ushort )vertexIndexRemap[mesh.Triangles[i].C];
                            }

                            var vertexCount = mesh.VertexCount;
                            Debug.Assert( mesh.Triangles.All( x => x.A < vertexCount && x.B < vertexCount && x.C < vertexCount ) );

                            //var processedVertexCount = 0;
                            //while ( processedVertexCount < aiMesh.VertexCount )
                            //{
                            //    // Weighted mesh
                            //    // TODO: decide between type 2 and 7?
                            //    var mesh = new MeshType7();
                            //    mesh.MaterialIndex = ( short )aiMesh.MaterialIndex;
                            //    mesh.Triangles = aiMesh
                            //                     .Faces.Select( x => new Triangle( ( ushort )x.Indices[0], ( ushort )x.Indices[1],
                            //                                                       ( ushort )( x.IndexCount > 2 ? x.Indices[2] : x.Indices[1] ) ) )
                            //                     .ToArray();


                            //    var usedBones = new List<Assimp.Bone>();
                            //    var vertexBoneWeights = new List<List<(Assimp.Bone Bone, float Weight)>>();
                            //    var effectiveBatchVertexLimit = Math.Min( BATCH_VERTEX_LIMIT, aiMesh.VertexCount - processedVertexCount );
                            //    var batchVertexCount = 0;
                            //    for ( int i = 0; i < effectiveBatchVertexLimit; ++i, ++batchVertexCount )
                            //    {
                            //        var vertexIndex = processedVertexCount + i;
                            //        var assignedBones = aiMesh.Bones.Where( x => x.VertexWeights.Any( y => y.VertexID == vertexIndex ) );

                            //        var curVertexWeights = new List<(Assimp.Bone Bone, float Weight)>();
                            //        var skip = false;
                            //        foreach ( var assignedBone in assignedBones )
                            //        {
                            //            curVertexWeights.Add( (assignedBone,
                            //                                      assignedBone.VertexWeights.First( x => x.VertexID == vertexIndex ).Weight) );

                            //            if ( !usedBones.Contains( assignedBone ) )
                            //            {
                            //                usedBones.Add( assignedBone );
                            //                if ( usedBones.Count == MAX_WEIGHTS_PER_VERTEX )
                            //                {
                            //                    skip = assignedBone != assignedBones.Last();
                            //                    break;
                            //                }
                            //            }
                            //        }

                            //        if ( !skip )
                            //            vertexBoneWeights.Add( curVertexWeights );

                            //        if ( usedBones.Count == MAX_WEIGHTS_PER_VERTEX )
                            //            break;

                            //    }

                            //    mesh.Triangles = aiMesh
                            //                     .Faces.Where( x => x.Indices.All( y => y >= processedVertexCount &&
                            //                                                            y < ( processedVertexCount + batchVertexCount ) ) )
                            //                     .Select( x => new Triangle( ( ushort ) x.Indices[ 0 ], ( ushort ) x.Indices[ 1 ],
                            //                                                 ( ushort ) ( x.IndexCount > 2 ? x.Indices[ 2 ] : x.Indices[ 1 ] ) ) )
                            //                     .ToArray();

                            //    // TODO
                            //    if ( usedBones.Count == 1 )
                            //    {
                            //        usedBones.Add( usedBones[ 0 ] );
                            //    }

                            //    var batch = new MeshType7Batch();

                            //    foreach ( var aiBone in usedBones )
                            //    {
                            //        var nodeBatch = new MeshType7NodeBatch();
                            //        nodeBatch.NodeIndex = ( short )model.Nodes.FindIndex( x => x.Name == aiBone.Name );
                            //        Debug.Assert( nodeBatch.NodeIndex > 0 );

                            //        var aiBoneNode = aiScene.RootNode.FindNode( aiBone.Name );
                            //        var aiBoneNodeWorldTransform = aiBoneNode.CalculateWorldTransform();
                            //        var boneNode = model.Nodes[ nodeBatch.NodeIndex ];
                            //        var boneNodeWorldTransform = boneNode.WorldTransform;
                            //        var boneNodeInvWorldTransform = boneNodeWorldTransform.Inverted();

                            //        // Convert positions
                            //        nodeBatch.Positions = new Vector4[batchVertexCount];
                            //        for ( int i = 0; i < batchVertexCount; i++ )
                            //        {
                            //            var vertexIndex = processedVertexCount + i;
                            //            var position      = aiMesh.Vertices[vertexIndex].FromAssimp();
                            //            var worldPosition = Vector3.Transform( position, aiNodeWorldTransform );
                            //            worldPositions.Add( worldPosition );
                            //            var boneWeights = vertexBoneWeights[ i ];
                            //            var weight = boneWeights.Find( x => x.Bone == aiBone ).Weight;
                            //            nodeBatch.Positions[ i ] =
                            //                new Vector4( Vector3.Transform( worldPosition, boneNodeInvWorldTransform ), weight );
                            //        }

                            //        // Convert normals
                            //        nodeBatch.Normals = new Vector3[batchVertexCount];
                            //        for ( int i = 0; i < batchVertexCount; i++ )
                            //        {
                            //            var boneWeights = vertexBoneWeights[i];
                            //            nodeBatch.Normals[ i ] =
                            //                Vector3
                            //                    .TransformNormal( Vector3.TransformNormal( aiMesh.Normals[ processedVertexCount + i ].FromAssimp(), aiNodeWorldTransform ),
                            //                                      boneNodeInvWorldTransform );
                            //        }

                            //        batch.NodeBatches.Add( nodeBatch );
                            //    }

                            //    // Convert texture coords
                            //    batch.TexCoords = new Vector2[batchVertexCount];
                            //    for ( int j = 0; j < batchVertexCount; j++ )
                            //        batch.TexCoords[j] = aiMesh.TextureCoordinateChannels[0][processedVertexCount + j].FromAssimpAsVector2();

                            //    // Add batch to mesh
                            //    mesh.Batches.Add( batch );
                            //    processedVertexCount += batchVertexCount;
                            //    geometry.Meshes.Add( mesh );
                            //}

                            //Debug.Assert( processedVertexCount == aiMesh.VertexCount );
                            geometry.Meshes.Add( mesh );
                        }
                        else
                        {
                            // Unweighted mesh
                            // TODO: decide between type 1 and 8?
                            var mesh = new MeshType8();
                            mesh.MaterialIndex = ( short ) aiMesh.MaterialIndex;
                            mesh.Triangles = aiMesh
                                             .Faces.Select( x => new Triangle( ( ushort ) x.Indices[ 0 ], ( ushort ) x.Indices[ 1 ],
                                                                               ( ushort ) ( x.IndexCount > 2 ? x.Indices[ 2 ] : x.Indices[ 1 ] ) ) )
                                             .ToArray();

                            if ( !hasTexture )
                                mesh.Flags &= ~MeshFlags.TexCoord;

                            var processedVertexCount = 0;
                            while ( processedVertexCount < aiMesh.VertexCount )
                            {
                                var batchVertexCount = Math.Min( BATCH_VERTEX_LIMIT, aiMesh.VertexCount - processedVertexCount );
                                var batch = new MeshType8Batch();

                                // Convert positions
                                batch.Positions = new Vector3[batchVertexCount];
                                if ( aiMesh.HasVertices )
                                {
                                    for ( int j = 0; j < batchVertexCount; j++ )
                                    {
                                        var position      = aiMesh.Vertices[processedVertexCount + j].FromAssimp();
                                        var worldPosition = Vector3.Transform( position, aiNodeWorldTransform );
                                        worldPositions.Add( worldPosition );
                                        batch.Positions[j] = Vector3.Transform( worldPosition, nodeInvWorldTransform );
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

                            geometry.Meshes.Add( mesh );
                        }
                    }

                    node.Geometry = geometry;
                    node.BoundingBox = BoundingBox.Calculate( worldPositions );
                }

                foreach ( var aiNodeChild in aiNode.Children )
                {
                    RecurseOverNodes( aiNodeChild, ref aiNodeWorldTransform );
                }
            }

            var identityTransform = Matrix4x4.Identity;
            RecurseOverNodes( aiScene.RootNode, ref identityTransform );
        }

        private static Node DetermineBestTargetNode( Assimp.Mesh aiMesh, List<Node> convertedNodes )
        {
            if ( aiMesh.BoneCount > 1 )
            {
                var boneConveragePercents = CalculateBoneWeightCoveragePercents( aiMesh );
                var maxCoverage = boneConveragePercents.Max( x => x.Coverage );
                var bestTargetBone = boneConveragePercents.First( x => x.Coverage == maxCoverage ).Bone;
                return convertedNodes.Find( x => x.Name == bestTargetBone.Name );
            }
            else
            {
                return convertedNodes.Find( x => x.Name == aiMesh.Bones[0].Name );
            }
        }

        private static List<(float Coverage, Assimp.Bone Bone)> CalculateBoneWeightCoveragePercents( Assimp.Mesh aiMesh )
        {
            var boneScores = new List<(float Coverage, Assimp.Bone Bone)>();

            foreach ( var bone in aiMesh.Bones )
            {
                float weightTotal = 0;
                foreach ( var vertexWeight in bone.VertexWeights )
                    weightTotal += vertexWeight.Weight;

                float weightCoverage = ( weightTotal / aiMesh.VertexCount ) * 100f;
                boneScores.Add( (weightCoverage, bone) );
            }

            return boneScores;
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
