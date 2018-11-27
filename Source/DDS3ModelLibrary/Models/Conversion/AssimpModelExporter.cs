using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DDS3ModelLibrary.Materials;
using DDS3ModelLibrary.Models.Utilities;
using DDS3ModelLibrary.Textures;

namespace DDS3ModelLibrary.Models.Conversion
{
    public sealed partial class AssimpModelExporter : ModelExporter<AssimpModelExporter, AssimpModelExporter.Config>
    {
        public override void Export( Model model, string filepath, Config config, TexturePack textures = null )
        {
            if ( textures != null )
            {
                var fileDirectory = Path.GetDirectoryName( filepath );
                var texturesPath = Path.Combine( fileDirectory, "textures" );

                // Export textures if they are available
                Directory.CreateDirectory( texturesPath );

                for ( var i = 0; i < textures.Count; i++ )
                    textures[ i ].GetBitmap().Save( Path.Combine( texturesPath, FormatTextureName( i ) ) );
            }

            var aiScene = ConvertToScene( model, config );
            aiScene.ExportColladaFile( filepath );
        }

        public Assimp.Scene ConvertToScene( Model model, Config config )
        {
            // Start building scene
            var aiScene = AssimpHelper.CreateDefaultScene();

            // Convert materials
            for ( var i = 0; i < model.Materials.Count; i++ )
            {
                var material   = model.Materials[i];
                var aiMaterial = new Assimp.Material { Name = FormatMaterialName( material, i ) };

                if ( material.TextureId != null )
                {
                    aiMaterial.TextureDiffuse = new Assimp.TextureSlot
                    {
                        TextureType = Assimp.TextureType.Diffuse,
                        FilePath    = Path.Combine( "textures", FormatTextureName( material.TextureId.Value ) )
                    };
                }

                aiScene.Materials.Add( aiMaterial );
            }

            // Convert nodes
            var aiNodeLookup = new Dictionary<Node, Assimp.Node>();
            for ( var i = 0; i < model.Nodes.Count; i++ )
            {
                var node   = model.Nodes[i];
                var aiNode = new Assimp.Node( FormatNodeName( node, i ), node.Parent != null ? aiNodeLookup[node.Parent] : aiScene.RootNode );
                aiNodeLookup[node] = aiNode;
                aiNode.Transform   = node.Transform.ToAssimp();

                if ( node.Geometry != null )
                {
                    ConvertMeshList( node.Geometry.Meshes, node, i, model.Nodes, aiScene, aiNode );

                    if ( node.Geometry.TranslucentMeshes != null )
                        ConvertMeshList( node.Geometry.TranslucentMeshes, node, i, model.Nodes, aiScene, aiNode );
                }

                aiNode.Parent.Children.Add( aiNode );
            }

            return aiScene;
        }

        private static void ConvertMeshList( MeshList meshList, Node node, int nodeIndex, List<Node> nodes, Assimp.Scene aiScene, Assimp.Node aiNode )
        {
            var meshStartIndex = aiScene.Meshes.Count;

            foreach ( var mesh in meshList )
            {
                switch ( mesh.Type )
                {
                    case MeshType.Type1:
                        aiScene.Meshes.AddRange( ConvertMeshType1( ( MeshType1 )mesh, node, nodeIndex ) );
                        break;
                    case MeshType.Type2:
                        aiScene.Meshes.AddRange( ConvertMeshType2( ( MeshType2 )mesh, nodes ) );
                        break;
                    case MeshType.Type3:
                        break;
                    case MeshType.Type4:
                        aiScene.Meshes.Add( ConvertMeshType4( ( MeshType4 )mesh, node, nodeIndex ) );
                        break;
                    case MeshType.Type5:
                        aiScene.Meshes.AddRange( ConvertMeshType5( ( MeshType5 )mesh, node, nodes, nodeIndex ) );
                        break;
                    case MeshType.Type7:
                        aiScene.Meshes.Add( ConvertMeshType7( ( MeshType7 )mesh, nodes ) );
                        break;
                    case MeshType.Type8:
                        aiScene.Meshes.Add( ConvertMeshType8( ( MeshType8 )mesh, node, nodeIndex ) );
                        break;
                }
            }

            var meshEndIndex = aiScene.Meshes.Count;
            for ( int i = meshStartIndex; i < meshEndIndex; i++ )
            {
                var meshNode = new Assimp.Node( $"mesh_{i:D2}", aiScene.RootNode );
                meshNode.MeshIndices.Add( i );
                meshNode.Parent.Children.Add( meshNode );
            }
        }

        private static IEnumerable<Assimp.Mesh> ConvertMeshType1( MeshType1 mesh, Node node, int nodeIndex )
        {
            foreach ( var batch in mesh.Batches )
            {
                var aiMesh = new Assimp.Mesh { MaterialIndex = mesh.MaterialIndex };

                aiMesh.Faces.AddRange( batch.Triangles.Select( x => new Assimp.Face( new int[] { x.A, x.B, x.C } ) ) );

                ( var positions, var normals ) = batch.Transform( node.WorldTransform );

                aiMesh.Vertices.AddRange( positions.ToAssimp() );

                if ( normals != null )
                    aiMesh.Normals.AddRange( normals.ToAssimp() );

                if ( batch.Colors != null )
                    aiMesh.VertexColorChannels[ 0 ].AddRange( batch.Colors.ToAssimp() );

                if ( batch.TexCoords != null )
                    aiMesh.TextureCoordinateChannels[ 0 ].AddRange( batch.TexCoords.ToAssimp() );

                if ( batch.TexCoords2 != null )
                    aiMesh.TextureCoordinateChannels[ 1 ].AddRange( batch.TexCoords2.ToAssimp() );

                AssignFauxWeights( aiMesh, node, nodeIndex );

                yield return aiMesh;
            }
        }

        private static IEnumerable<Assimp.Mesh> ConvertMeshType2( MeshType2 mesh, List<Node> nodes )
        {
            foreach ( var batch in mesh.Batches )
            {
                var aiMesh = new Assimp.Mesh { MaterialIndex = mesh.MaterialIndex };

                aiMesh.Faces.AddRange( batch.Triangles.Select( x => new Assimp.Face( new int[] { x.A, x.B, x.C } ) ) );

                ( var positions, var normals, var weights ) = batch.Transform( nodes );

                aiMesh.Vertices.AddRange( positions.ToAssimp() );

                if ( normals != null )
                    aiMesh.Normals.AddRange( normals.ToAssimp() );

                ConvertWeights( nodes, new Dictionary<int, Assimp.Bone>(), weights, aiMesh, 0 );

                if ( batch.Colors != null )
                    aiMesh.VertexColorChannels[0].AddRange( batch.Colors.ToAssimp() );

                if ( batch.TexCoords != null )
                    aiMesh.TextureCoordinateChannels[0].AddRange( batch.TexCoords.ToAssimp() );

                if ( batch.TexCoords2 != null )
                    aiMesh.TextureCoordinateChannels[1].AddRange( batch.TexCoords2.ToAssimp() );

                yield return aiMesh;
            }
        }

        private static Assimp.Mesh ConvertMeshType4( MeshType4 mesh, Node node, int nodeIndex )
        {
            var aiMesh = new Assimp.Mesh { MaterialIndex = mesh.MaterialIndex };

            aiMesh.Faces.AddRange( mesh.Triangles.Select( x => new Assimp.Face( new int[] { x.A, x.B, x.C } ) ) );

            (var positions, var normals) = mesh.Transform( node.WorldTransform );

            aiMesh.Vertices.AddRange( positions.ToAssimp() );

            if ( normals != null )
                aiMesh.Normals.AddRange( normals.ToAssimp() );

            AssignFauxWeights( aiMesh, node, nodeIndex );

            return aiMesh;
        }

        private static IEnumerable<Assimp.Mesh> ConvertMeshType5( MeshType5 mesh, Node node, List<Node> nodes, int nodeIndex )
        {
            if ( mesh.UsedNodeCount == 0 )
            {
                foreach ( ( var positions, var normals ) in mesh.Transform( node.WorldTransform ) )
                {
                    var aiMesh = new Assimp.Mesh { MaterialIndex = mesh.MaterialIndex };
                    aiMesh.Faces.AddRange( mesh.Triangles.Select( x => new Assimp.Face( new int[] { x.A, x.B, x.C } ) ) );

                    aiMesh.Vertices.AddRange( positions.ToAssimp() );

                    if ( normals != null )
                        aiMesh.Normals.AddRange( normals.ToAssimp() );

                    if ( mesh.TexCoords != null )
                        aiMesh.TextureCoordinateChannels[0].AddRange( mesh.TexCoords.ToAssimp() );

                    if ( mesh.TexCoords2 != null )
                        aiMesh.TextureCoordinateChannels[1].AddRange( mesh.TexCoords2.ToAssimp() );

                    AssignFauxWeights( aiMesh, node, nodeIndex );

                    yield return aiMesh;
                }
            }
            else
            {
                var aiMesh = new Assimp.Mesh { MaterialIndex = mesh.MaterialIndex };
                aiMesh.Faces.AddRange( mesh.Triangles.Select( x => new Assimp.Face( new int[] { x.A, x.B, x.C } ) ) );

                (var positions, var normals, var weights) = mesh.Transform( nodes );

                aiMesh.Vertices.AddRange( positions.ToAssimp() );

                if ( normals != null )
                    aiMesh.Normals.AddRange( normals.ToAssimp() );

                ConvertWeights( nodes, new Dictionary<int, Assimp.Bone>(), weights, aiMesh, 0 );

                if ( mesh.TexCoords != null )
                    aiMesh.TextureCoordinateChannels[0].AddRange( mesh.TexCoords.ToAssimp() );

                if ( mesh.TexCoords2 != null )
                    aiMesh.TextureCoordinateChannels[1].AddRange( mesh.TexCoords2.ToAssimp() );

                yield return aiMesh;
            }
        }

        private static Assimp.Mesh ConvertMeshType7( MeshType7 mesh, List<Node> nodes )
        {
            var aiBoneLookup = new Dictionary<int, Assimp.Bone>();

            var aiMesh = new Assimp.Mesh { MaterialIndex = mesh.MaterialIndex };
            aiMesh.Faces.AddRange( mesh.Triangles.Select( x => new Assimp.Face( new int[] { x.A, x.B, x.C } ) ) );

            foreach ( var batch in mesh.Batches )
            {
                (var positions, var normals, var weights) = batch.Transform( nodes );

                ConvertWeights( nodes, aiBoneLookup, weights, aiMesh, aiMesh.VertexCount );

                aiMesh.Vertices.AddRange( positions.ToAssimp() );

                if ( normals != null )
                    aiMesh.Normals.AddRange( normals.ToAssimp() );

                if ( batch.TexCoords != null )
                    aiMesh.TextureCoordinateChannels[0].AddRange( batch.TexCoords.ToAssimp() );
            }

            if ( mesh.TexCoords2 != null )
                aiMesh.TextureCoordinateChannels[ 1 ].AddRange( mesh.TexCoords2.ToAssimp() );

            return aiMesh;
        }

        private static Assimp.Mesh ConvertMeshType8( MeshType8 mesh, Node node, int nodeIndex )
        {
            var aiMesh = new Assimp.Mesh { MaterialIndex = mesh.MaterialIndex };
            aiMesh.Faces.AddRange( mesh.Triangles.Select( x => new Assimp.Face( new int[] { x.A, x.B, x.C } ) ) );

            foreach ( var batch in mesh.Batches )
            {
                (var positions, var normals) = batch.Transform( node.WorldTransform );

                aiMesh.Vertices.AddRange( positions.ToAssimp() );

                if ( normals != null )
                    aiMesh.Normals.AddRange( normals.ToAssimp() );

                if ( batch.TexCoords != null )
                    aiMesh.TextureCoordinateChannels[0].AddRange( batch.TexCoords.ToAssimp() );
            }

            if ( mesh.TexCoords2 != null )
                aiMesh.TextureCoordinateChannels[1].AddRange( mesh.TexCoords2.ToAssimp() );

            AssignFauxWeights( aiMesh, node, nodeIndex );

            return aiMesh;
        }

        private static void ConvertWeights( List<Node> nodes, Dictionary<int, Assimp.Bone> aiBoneLookup, NodeWeight[][] weights, Assimp.Mesh aiMesh, int vertexBaseIndex )
        {
            for ( int i = 0; i < weights.Length; i++ )
            {
                foreach ( var nodeWeight in weights[i] )
                {
                    if ( nodeWeight.Weight == 0f )
                        continue;

                    if ( !aiBoneLookup.TryGetValue( nodeWeight.NodeIndex, out var aiBone ) )
                    {
                        var node = nodes[ nodeWeight.NodeIndex ];
                        aiMesh.Bones.Add( aiBoneLookup[ nodeWeight.NodeIndex ] = aiBone = new Assimp.Bone
                        {
                            Name         = FormatNodeName( node, nodeWeight.NodeIndex ),
                            OffsetMatrix = node.WorldTransform.Inverted().ToAssimp()
                        } );
                    }

                    aiBone.VertexWeights.Add( new Assimp.VertexWeight( vertexBaseIndex + i, nodeWeight.Weight ) );
                }
            }
        }

        private static void AssignFauxWeights( Assimp.Mesh aiMesh, Node node, int nodeIndex )
        {
            var aiBone = new Assimp.Bone { Name = FormatNodeName( node, nodeIndex ), OffsetMatrix = node.WorldTransform.Inverted().ToAssimp() };
            for ( int i = 0; i < aiMesh.VertexCount; i++ )
                aiBone.VertexWeights.Add( new Assimp.VertexWeight( i, 1f ) );

            aiMesh.Bones.Add( aiBone );
        }

        private static string FormatTextureName( int textureIndex ) => $"texture_{textureIndex:D2}.png";

        private static string FormatMaterialName( Material material, int index )
        {
            var name = $"material_{index:D2}";

            if ( MaterialPresetStore.TryGetPresetId( material, out var presetId ) )
                name += $"@ps({presetId})";

            if ( material.OverlayTextureIds != null )
                name += $"@ovl({FormatTextureName( material.OverlayTextureIds[ 0 ] )},{FormatTextureName( material.OverlayTextureIds[ 1 ] )})";

            return name;
        }

        private static string FormatNodeName( Node node, int index )
        {
            return node.Name?.Replace( " ", "_" ) ?? $"node_{index:D2}";
        }
    }
}