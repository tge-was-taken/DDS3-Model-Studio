using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using DDS3ModelLibrary;

namespace DDS3ModelLibraryCLI
{
    internal class Program
    {
        private static void Main( string[] args )
        {
            //ReplaceModelTest();
            OpenAndSaveModelPackBatchTest();
        }

        private static void OpenAndSaveModelPackTest()
        {
            var modelPack = new ModelPack( @"..\..\..\..\Resources\player_a.PB" );
            modelPack.Save( @"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\model\field\player_a.PB" );
        }

        private static void FindUniqueFiles( string outDirectory, string searchDirectory, string extension )
        {
            Directory.CreateDirectory( outDirectory );

            var checksums = new HashSet<long>();
            var paths     = Directory.EnumerateFiles( searchDirectory, "*" + extension, SearchOption.AllDirectories ).ToList();
            var done      = 0;
            Parallel.ForEach( paths, new ParallelOptions() { MaxDegreeOfParallelism = 6 }, ( path ) =>
            {
                Console.WriteLine( $"{done}/{paths.Count} {path}" );

                long checksum = 0;
                using ( var stream = File.OpenRead( path ) )
                {
                    while ( stream.Position < stream.Length )
                    {
                        checksum += stream.ReadByte();
                    }
                }

                lock ( checksums )
                {
                    if ( !checksums.Contains( checksum ) )
                    {
                        File.Copy( path, Path.Combine( outDirectory, Path.GetFileNameWithoutExtension( path ) + "_" + checksum + extension ), true );
                        checksums.Add( checksum );
                    }
                }

                ++done;
            } );
        }

        private static void OpenAndSaveModelPackBatchTest()
        {
            if ( !Directory.Exists( "unique_models" ) )
                FindUniqueFiles( "unique_models", @"D:\Modding\DDS3", ".PB" );

            if ( !Directory.Exists( "unique_models_mb" ) )
                FindUniqueFiles( "unique_models_mb", @"D:\Modding\DDS3", ".MB" );

            var paths = Directory.EnumerateFiles( "unique_models", "*.PB", SearchOption.AllDirectories )
                                 .Concat( Directory.EnumerateFiles( "unique_models_mb", "*.MB", SearchOption.AllDirectories ) )
                                 .ToList();
            var done = 0;
            Parallel.ForEach( paths, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, ( path ) =>
            {
                Console.WriteLine( Path.GetFileName(path) );

                var modelPack = new ModelPack( path );
                modelPack.Save( path + ".out" );
                File.Delete( path + ".out" );
            });
        }

        private static void ReplaceModelTest()
        {
            var modelPack = new ModelPack( @"..\..\..\..\Resources\player_a.PB" );
            var model = modelPack.Models[ 0 ];

            var context = new Assimp.AssimpContext();
            context.SetConfig( new Assimp.Configs.FBXPreservePivotsConfig( false ) );
            var aiScene = context.ImportFile( "player_a_test.fbx", Assimp.PostProcessSteps.JoinIdenticalVertices |
                                                        Assimp.PostProcessSteps.CalculateTangentSpace | Assimp.PostProcessSteps.FindDegenerates |
                                                        Assimp.PostProcessSteps.FindInvalidData | Assimp.PostProcessSteps.FlipUVs |
                                                        Assimp.PostProcessSteps.GenerateNormals | Assimp.PostProcessSteps.GenerateUVCoords |
                                                        Assimp.PostProcessSteps.ImproveCacheLocality |
                                                        Assimp.PostProcessSteps.Triangulate |
                                                        //Assimp.PostProcessSteps.FixInFacingNormals |
                                                        Assimp.PostProcessSteps.GenerateSmoothNormals |
                                                        Assimp.PostProcessSteps.JoinIdenticalVertices |
                                                        Assimp.PostProcessSteps.LimitBoneWeights | Assimp.PostProcessSteps.OptimizeMeshes
                                                     //| Assimp.PostProcessSteps.PreTransformVertices
                                            );

            var headNode = model.Nodes.First( x => x.Name == "p_a Head" );
            //headNode.Geometry = new Geometry();
            //headNode.Geometry.Meshes.Add( ConvertAssimpMesh( aiScene.Meshes[ 0 ], model.Nodes ) );

            modelPack.Save( @"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\model\field\player_a.PB" );
        }

        private static Mesh ConvertAssimpMesh( Assimp.Mesh aiMesh, List<Node> nodes )
        {
            var mesh = new MeshType7();
            mesh.MaterialId = ( short ) aiMesh.MaterialIndex;
            mesh.VertexCount = ( short ) aiMesh.VertexCount;

            // Convert triangles
            mesh.Triangles = new Triangle[aiMesh.FaceCount];
            for ( int i = 0; i < aiMesh.Faces.Count; i++ )
            {
                var aiFace = aiMesh.Faces[ i ];
                var a = ( ushort )aiFace.Indices[ 0 ];
                var b = ( ushort )aiFace.Indices[ 1 ];
                var c = aiFace.IndexCount > 2 ? ( ushort )aiFace.Indices[2] : b;
                mesh.Triangles[i ] = new Triangle( a, b, c );
            }

            var texCoords = aiMesh.TextureCoordinateChannels[0].Select( x => new Vector2( x.X, x.Y ) ).ToArray();
            var positions = aiMesh.Vertices.Select( x => new Vector4( x.X, x.Y, x.Z, 1f ) ).ToArray();
            var normals = aiMesh.Normals.Select( x => new Vector3( x.X, x.Y, x.Z ) ).ToArray();
            const int VERTEX_LIMIT = 24;

            var batchCount = (int)Math.Round( ( float ) aiMesh.VertexCount / VERTEX_LIMIT, MidpointRounding.AwayFromZero );
            var processedVertexCount = 0;
            for ( int i = 0; i < batchCount; i++ )
            {
                // Create batches
                var batchVertexCount = Math.Min( VERTEX_LIMIT, mesh.VertexCount - processedVertexCount );
                var batch = new MeshBatchType7();
                batch.VertexCount = ( short ) batchVertexCount;
                batch.TexCoords = new Vector2[batchVertexCount];
                Array.Copy( texCoords, processedVertexCount, batch.TexCoords, 0, batchVertexCount );

                // Create node batches
                {
                    var nodeBatch = new MeshNodeBatchType7();
                    //nodeBatch.NodeId = ( short ) nodes.FindIndex( x => x.Name == aiMesh.Bones[ 0 ].Name );
                    nodeBatch.NodeId = 50;

                    nodeBatch.Positions = new Vector4[batchVertexCount];
                    Array.Copy( positions, processedVertexCount, nodeBatch.Positions, 0, batchVertexCount );

                    nodeBatch.Normals = new Vector3[batchVertexCount];
                    Array.Copy( normals, processedVertexCount, nodeBatch.Normals, 0, batchVertexCount );

                    // Add node batch to batch
                    batch.NodeBatches.Add( nodeBatch );
                }

                {
                    var nodeBatch = new MeshNodeBatchType7();
                    nodeBatch.NodeId = 51;
                    nodeBatch.Positions = new Vector4[batchVertexCount];
                    nodeBatch.Normals = new Vector3[batchVertexCount];
                    batch.NodeBatches.Add( nodeBatch );
                }

                // Add batch to mesh
                mesh.Batches.Add( batch );
                processedVertexCount += batchVertexCount;
            }

            // Return converted mesh
            return mesh;
        }
    }
}
