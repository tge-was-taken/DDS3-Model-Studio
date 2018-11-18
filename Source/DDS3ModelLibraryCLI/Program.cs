using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DDS3ModelLibrary;
using DDS3ModelLibrary.IO.Common;
using DDS3ModelLibrary.Modeling.Utilities;
using DDS3ModelLibrary.Primitives;
using Newtonsoft.Json;

namespace DDS3ModelLibraryCLI
{
    internal class Program
    {
        private static void Main( string[] args )
        {
            //ExportObj( new ModelPack( @"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\model\devil\0x00\0x0a\0x0a.PB" ) );
            //return;
            OpenAndSaveModelPackBatchTest();return;
            //ReplaceModelTest();return;

            //var modelPack = new ModelPack( @"..\..\..\..\Resources\player_a.PB" );
            var modelPack = new ModelPack( @"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\model\field\player_a.PB" );

            //using ( var writer = File.CreateText( "test.obj" ) )
            //{
            //    var vertexBaseIndex = 0;

            //    foreach ( var model in modelPack.Models )
            //    {
            //        foreach ( var node in model.Nodes )
            //        {
            //            if ( node.Geometry == null )
            //                continue;

            //            for ( var meshIndex = 0; meshIndex < node.Geometry.Meshes.Count; meshIndex++ )
            //            {
            //                var _mesh = node.Geometry.Meshes[ meshIndex ];
            //                if ( _mesh.Type != MeshType.Type7 )
            //                    continue;

            //                var mesh = ( MeshType7 ) _mesh;
            //                var positions = new Vector3[mesh.VertexCount];
            //                var normals   = new Vector3[positions.Length];
            //                var weights   = new List<(short NodeIndex, float Weight)>[positions.Length];
            //                var texCoords = new Vector2[positions.Length];
            //                var batchVertexBaseIndex = 0;

            //                foreach ( var batch in mesh.Batches )
            //                {
            //                    for ( var nodeBatchIndex = 0; nodeBatchIndex < batch.NodeBatches.Count; nodeBatchIndex++ )
            //                    {
            //                        var nodeBatch          = batch.NodeBatches[ nodeBatchIndex ];
            //                        var nodeWorldTransform = model.Nodes[ nodeBatch.NodeIndex ].WorldTransform;

            //                        for ( int i = 0; i < nodeBatch.Positions.Length; i++ )
            //                        {
            //                            var position = new Vector3( nodeBatch.Positions[i].X, nodeBatch.Positions[i].Y,
            //                                                        nodeBatch.Positions[i].Z );
            //                            var weight                     = nodeBatch.Positions[i].W;
            //                            var weightedNodeWorldTransform = nodeWorldTransform * weight;
            //                            var weightedWorldPosition      = Vector3.Transform( position, weightedNodeWorldTransform );
            //                            positions[batchVertexBaseIndex + i] += weightedWorldPosition;
            //                            if ( weights[batchVertexBaseIndex + i] == null )
            //                                weights[batchVertexBaseIndex + i] = new List<(short NodeIndex, float Weight)>();
            //                            weights[batchVertexBaseIndex + i].Add( (nodeBatch.NodeIndex, weight) );
            //                            normals[batchVertexBaseIndex + i] += Vector3.TransformNormal( nodeBatch.Normals[i], weightedNodeWorldTransform );
            //                        }
            //                    }

            //                    Array.Copy( batch.TexCoords, 0, texCoords, batchVertexBaseIndex, batch.TexCoords.Length );

            //                    //foreach ( var position in positions )
            //                    //{
            //                    //    writer.WriteLine( $"v {position.X} {position.Y} {position.Z}" );
            //                    //}

            //                    //foreach ( var normal in normals )
            //                    //{
            //                    //    writer.WriteLine( $"vn {normal.X} {normal.Y} {normal.Z}" );
            //                    //}

            //                    //foreach ( var texCoord in batch.TexCoords )
            //                    //{
            //                    //    writer.WriteLine( $"vt {texCoord.X} {texCoord.Y}" );
            //                    //}

            //                    batchVertexBaseIndex += batch.VertexCount;
            //                }

            //                foreach ( var position in positions )
            //                {
            //                    writer.WriteLine( $"v {position.X} {position.Y} {position.Z}" );
            //                }

            //                foreach ( var normal in normals )
            //                {
            //                    writer.WriteLine( $"vn {normal.X} {normal.Y} {normal.Z}" );
            //                }

            //                foreach ( var texCoord in texCoords )
            //                {
            //                    writer.WriteLine( $"vt {texCoord.X} {texCoord.Y}" );
            //                }

            //                //// Find unique node indices used by the mesh (max 4 per mesh!)
            //                //var usedNodeIndices = weights.SelectMany( x => x.Select( y => y.NodeIndex ) ).Distinct().ToList();

            //                //// Calculate node index usage frequency
            //                //var usedNodeIndicesFrequency = new Dictionary<int, int>();
            //                //for ( int i = 0; i < usedNodeIndices.Count; i++ )
            //                //    usedNodeIndicesFrequency[usedNodeIndices[i]] = 0;

            //                //for ( int j = 0; j < positions.Length; j++ )
            //                //{
            //                //    foreach ( var (nodeIndex, _) in weights[j] )
            //                //        ++usedNodeIndicesFrequency[nodeIndex];
            //                //}

            //                //// Sort used node indices by frequency
            //                //usedNodeIndices = usedNodeIndices.OrderBy( x => usedNodeIndicesFrequency[ x ] ).ToList();

            //                //// Start building batches
            //                //var vertexIndexRemap     = new Dictionary<int, int>();
            //                //var batches = new List<MeshType7Batch>();
            //                //batchVertexBaseIndex = 0;

            //                //while ( vertexIndexRemap.Count < positions.Length )
            //                //{
            //                //    var batchVertexCount = Math.Min( 24, positions.Length - vertexIndexRemap.Count );
            //                //    var batch = new MeshType7Batch();
            //                //    var batchVertexIndexRemap = new Dictionary<int, int>();
            //                //    var batchTexCoords = new List<Vector2>();

            //                //    // req. all vertices to use the same set of node indices
            //                //    for ( var i = 0; i < usedNodeIndices.Count; i++ )
            //                //    {
            //                //        var nodeIndex = usedNodeIndices[i];
            //                //        var nodeWorldTransform = model.Nodes[nodeIndex].WorldTransform;
            //                //        var nodeWorldTransformInv = nodeWorldTransform.Inverted();

            //                //        // get all verts with this index
            //                //        var nodePositions = new List<Vector4>();
            //                //        var nodeNormals = new List<Vector3>();

            //                //        for ( int j = 0; j < positions.Length; j++ )
            //                //        {
            //                //            // Skip this vertex if it has already been processed before
            //                //            if ( vertexIndexRemap.ContainsKey( j ) )
            //                //                continue;

            //                //            foreach ( (short NodeIndex, float Weight) in weights[j] )
            //                //            {
            //                //                if ( NodeIndex != nodeIndex )
            //                //                    continue;

            //                //                // Transform position and normal to model space
            //                //                var position = Vector3.Transform( positions[j], nodeWorldTransformInv );
            //                //                var normal = Vector3.TransformNormal( normals[j], nodeWorldTransformInv );

            //                //                // Add entry to vertex remap, and add the model space positions and normals to our lists
            //                //                batchVertexIndexRemap[j] = batchVertexBaseIndex + nodePositions.Count;
            //                //                nodePositions.Add( new Vector4( position, Weight ) );
            //                //                nodeNormals.Add( normal );

            //                //                if ( i == 0 )
            //                //                {
            //                //                    // Only add this once, of course
            //                //                    batchTexCoords.Add( texCoords[ j ] );
            //                //                }

            //                //                // Stop looking if we've reached our vertex count
            //                //                if ( nodePositions.Count == batchVertexCount )
            //                //                    goto end;
            //                //            }
            //                //        }

            //                //        end:
            //                //        batch.NodeBatches.Add( new MeshType7NodeBatch()
            //                //        {
            //                //            NodeIndex = nodeIndex,
            //                //            Positions = nodePositions.ToArray(),
            //                //            Normals   = nodeNormals.ToArray()
            //                //        });
            //                //    }

            //                //    batch.TexCoords = batchTexCoords.ToArray();

            //                //    foreach ( var i in batchVertexIndexRemap )
            //                //        vertexIndexRemap.Add( i.Key, i.Value );

            //                //    Debug.Assert( batch.NodeBatches.Count > 0 );
            //                //    Debug.Assert( batch.NodeBatches.TrueForAll( x => x.VertexCount == batch.NodeBatches[ 0 ].VertexCount ) );
            //                //    batches.Add( batch );
            //                //    batchVertexBaseIndex += batchVertexCount;
            //                //}

            //                //var materialIndex = mesh.MaterialIndex;
            //                //var triangles = new Triangle[mesh.TriangleCount];
            //                //for ( var i = 0; i < mesh.Triangles.Length; i++ )
            //                //{
            //                //    ref var triangle = ref triangles[i];
            //                //    triangle.A = ( ushort )vertexIndexRemap[mesh.Triangles[i].A];
            //                //    triangle.B = ( ushort )vertexIndexRemap[mesh.Triangles[i].B];
            //                //    triangle.C = ( ushort )vertexIndexRemap[mesh.Triangles[i].C];
            //                //}

            //                //mesh = new MeshType7();
            //                //mesh.Batches.AddRange( batches );
            //                //mesh.Triangles = triangles;
            //                //mesh.MaterialIndex = materialIndex;
            //                //node.Geometry.Meshes[meshIndex] = mesh;

            //                writer.WriteLine( $"o node_{node.Name}_mesh_{meshIndex}" );
            //                foreach ( var triangle in mesh.Triangles )
            //                {
            //                    writer.WriteLine( "f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", vertexBaseIndex + triangle.A + 1, vertexBaseIndex + triangle.B + 1, vertexBaseIndex + triangle.C + 1 );
            //                }

            //                vertexBaseIndex += mesh.VertexCount;
            //            }
            //        }
            //    }
            //}

            

            modelPack.Save( @"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\model\field\player_a.PB" );


            //ReplaceModelTest();
            //return;
            //GenerateMaterialPresets();
            //return;
            //var modelPack = new ModelPack( @"..\..\..\..\Resources\player_a.PB" );
            //foreach ( var material in modelPack.Models[0].Materials )
            //{
            //    Console.WriteLine( MaterialPresetStore.GetPresetId( material ) );
            //}
            //return;
            ////for ( var i = 0; i < modelPack.TexturePack.Count; i++ )
            ////{
            ////    var texture = modelPack.TexturePack[ i ];
            ////    texture.GetBitmap().Save( $"player_a_{i}.png" );
            ////}

            //modelPack.Save( @"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\model\field\player_a.PB" );
            ////ReplaceModelTest();
            //OpenAndSaveModelPackBatchTest();
        }

        public static void ExportObj( ModelPack modelPack )
        {
            void WritePositions( StreamWriter writer, Vector3[] positions )
            {
                foreach ( var position in positions )
                    writer.WriteLine( $"v {position.X} {position.Y} {position.Z}" );
            }

            void WriteTexCoords( StreamWriter writer, Vector2[] texCoords )
            {
                foreach ( var texCoord in texCoords )
                    writer.WriteLine( $"vt {texCoord.X} {texCoord.Y}" );
            }

            void WriteNormals( StreamWriter writer, Vector3[] normals )
            {
                foreach ( var normal in normals )
                    writer.WriteLine( $"vn {normal.X} {normal.Y} {normal.Z}" );
            }

            void WriteTriangles( StreamWriter writer, Node node, int meshIndex, Triangle[] triangles, int vertexBaseIndex )
            {
                writer.WriteLine( $"o node_{node.Name}_mesh_{meshIndex}" );
                foreach ( var triangle in triangles )
                {
                    writer.WriteLine( "f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", vertexBaseIndex + triangle.A + 1,
                                      vertexBaseIndex + triangle.B + 1, vertexBaseIndex + triangle.C + 1 );
                }
            }

            using ( var writer = File.CreateText( "test.obj" ) )
            {
                var vertexBaseIndex = 0;

                foreach ( var model in modelPack.Models )
                {
                    foreach ( var node in model.Nodes )
                    {
                        if ( node.Geometry == null )
                            continue;

                        for ( var meshIndex = 0; meshIndex < node.Geometry.Meshes.Count; meshIndex++ )
                        {
                            var _mesh = node.Geometry.Meshes[meshIndex];
                            writer.WriteLine( $"// node '{node.Name}' mesh ({_mesh.Type}) #{meshIndex}" );

                            switch ( _mesh.Type )
                            {
                                case MeshType.Type1:
                                    {
                                        var mesh = ( MeshType1 )_mesh;
                                        foreach ( var batch in mesh.Batches )
                                        {
                                            ( Vector3[] positions, Vector3[] normals ) = batch.GetProcessed( node.WorldTransform );
                                            WritePositions( writer, positions );
                                            WriteNormals( writer, normals );
                                            WriteTexCoords( writer, batch.TexCoords[ 0 ] );
                                            WriteTriangles( writer, node, meshIndex, batch.Triangles, vertexBaseIndex );
                                            vertexBaseIndex += batch.VertexCount;
                                        }
                                    }
                                    break;
                                case MeshType.Type2:
                                    {
                                        var mesh = ( MeshType2 )_mesh;
                                        foreach ( var batch in mesh.Batches )
                                        {
                                            ( Vector3[] positions, Vector3[] normals, _ ) = batch.GetProcessed( model.Nodes );
                                            WritePositions( writer, positions );
                                            WriteNormals( writer, normals );
                                            WriteTexCoords( writer, batch.TexCoords[0] );
                                            WriteTriangles( writer, node, meshIndex, batch.Triangles, vertexBaseIndex );
                                            vertexBaseIndex += batch.VertexCount;
                                        }
                                    }
                                    break;
                                case MeshType.Type3:
                                    break;
                                case MeshType.Type4:
                                    break;
                                case MeshType.Type5:
                                    break;
                                case MeshType.Type7:
                                    {
                                        var mesh = ( MeshType7 )_mesh;
                                        foreach ( var batch in mesh.Batches )
                                        {
                                            (Vector3[] positions, Vector3[] normals, _) = batch.GetProcessed( model.Nodes );

                                            WritePositions( writer, positions );
                                            WriteNormals( writer, normals );
                                            WriteTexCoords( writer, batch.TexCoords );
                                        }

                                        WriteTriangles( writer, node, meshIndex, mesh.Triangles, vertexBaseIndex );
                                        vertexBaseIndex += _mesh.VertexCount;
                                    }
                                    break;
                                case MeshType.Type8:
                                    {
                                        var mesh = ( MeshType8 )_mesh;
                                        foreach ( var batch in mesh.Batches )
                                        {
                                            ( Vector3[] positions, Vector3[] normals ) = batch.GetProcessed( node.WorldTransform );
                                            WritePositions( writer, positions );
                                            WriteNormals( writer, normals );
                                            WriteTexCoords( writer, batch.TexCoords );
                                        }

                                        WriteTriangles( writer, node, meshIndex, mesh.Triangles, vertexBaseIndex );
                                        vertexBaseIndex += _mesh.VertexCount;
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private static void OpenAndSaveModelPackTest()
        {
            var modelPack = new ModelPack( @"..\..\..\..\Resources\player_a.PB" );
            modelPack.Save( @"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\model\field\player_a.PB" );
        }

        private static string GetChecksum( string file )
        {
            using ( FileStream stream = File.OpenRead( file ) )
            {
                return GetChecksum( stream );
            }
        }

        private static string GetChecksum( Stream stream )
        {
            var    sha      = new SHA256Managed();
            byte[] checksum = sha.ComputeHash( stream );
            return BitConverter.ToString( checksum ).Replace( "-", String.Empty );
        }

        private static void FindUniqueFiles( string outDirectory, string searchDirectory, string extension )
        {
            Directory.CreateDirectory( outDirectory );

            var checksums = new HashSet<string>();
            var paths     = Directory.EnumerateFiles( searchDirectory, "*" + extension, SearchOption.AllDirectories ).ToList();
            var done      = 0;
            Parallel.ForEach( paths, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, ( path ) =>
            {
                Console.WriteLine( $"{done}/{paths.Count} {path}" );

                var checksum = GetChecksum( path );

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

            if ( !Directory.Exists( "unique_lb" ) )
            {
                FindUniqueFiles( "unique_lb", @"D:\Modding\DDS3", ".LB" );
            }

            if ( !Directory.Exists( "unique_lb_extracted" ) )
            {
                Directory.CreateDirectory( "unique_lb_extracted" );
                var checksums = new HashSet<string>();
                Parallel.ForEach( Directory.EnumerateFiles( "unique_lb" ), ( path ) =>
                {
                    var fileName = Path.GetFileNameWithoutExtension( path );
                    var outPath = $"unique_lb_extracted\\";
                    Directory.CreateDirectory( outPath );

                    using ( var lb = new AtlusFileSystemLibrary.FileSystems.LB.LBFileSystem() )
                    {
                        lb.Load( path );

                        foreach ( var file in lb.EnumerateFiles() )
                        {
                            var info = lb.GetInfo( file );
                            using ( var stream = lb.OpenFile( file ) )
                            {
                                var checksum = GetChecksum( stream );
                                stream.Position = 0;
                                var extract = false;

                                lock ( checksums )
                                {
                                    if ( !checksums.Contains( checksum ) )
                                    {
                                        checksums.Add( checksum );
                                        extract = true;
                                    }
                                }

                                if ( extract )
                                {
                                    var nameParts = Path.GetFileNameWithoutExtension( path ).Split( new[] { '_' } );
                                    Array.Resize( ref nameParts, nameParts.Length - 1 );

                                    using ( var fileStream =
                                        File.Create( Path.Combine( outPath, string.Concat( nameParts ) + "_" + file + "_" + checksum + "." + info.Extension )
                                                   ) )
                                    {
                                        Console.WriteLine( $"Extracting: {fileName} #{file} ({info.UserId:D2}, {info.Extension})" );
                                        stream.CopyTo( fileStream );
                                    }
                                }
                            }
                        }
                    }
                } );
            }

            var uniqueValues = new HashSet<int>();

            var paths = Directory.EnumerateFiles( "unique_models", "*.PB", SearchOption.AllDirectories ).ToList();
            var done = 0;
            Parallel.ForEach( paths, new ParallelOptions() { MaxDegreeOfParallelism = 16 }, ( path ) =>
            {
                Console.WriteLine( Path.GetFileName( path ) );
                var modelPack = new ModelPack( path );
                new ModelPack( modelPack.Save() );
            } );

            //var paths = Directory.EnumerateFiles( "unique_models", "*.PB", SearchOption.AllDirectories ).ToList();
            //var done = 0;
            //Parallel.ForEach( paths, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, ( path ) =>
            //{
            //    //Console.WriteLine( Path.GetFileName( path ) );
            //    var modelPack = new ModelPack( path );
            //    //modelPack.Save( path + ".out" );
            //    //File.Delete( path + ".out" );
            //    //modelPack.Save();
            //} );

            //var done = 0;
            //Parallel.ForEach( paths, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, ( path ) =>
            //{
            //    Console.WriteLine( Path.GetFileName( path ) );

            //    var outPath = "unique_models\\" + Path.GetFileName( path ) + ".PB";

            //    if ( Path.GetExtension( path ) == ".PAC" )
            //    {
            //        using ( var reader = new EndianBinaryReader( path, Endianness.Little ) )
            //        {
            //            reader.Position = 0x24;
            //            var size = reader.ReadInt32();
            //            reader.Position += 4;
            //            var offset = reader.ReadInt32();
            //            using ( var fileStream = File.Create( outPath ) )
            //                new StreamView( reader.BaseStream, offset, size ).CopyTo( fileStream );
            //        }
            //    }
            //    else
            //    {
            //        File.Copy( path, outPath, true );
            //    }
            //} );
        }

        private static void GenerateMaterialPresets()
        {
            var paths = Directory.EnumerateFiles( "unique_models", "*.PB", SearchOption.AllDirectories ).ToList();
            Directory.CreateDirectory( "material_presets" );

            var materialCache = new Dictionary<int, (int Id, bool IsTextured, bool HasOverlay)>();
            var done = 0;
            Parallel.ForEach( paths, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, ( path ) =>
            {
                Console.WriteLine( Path.GetFileName( path ) );
                var modelPack = new ModelPack( path );
                foreach ( var model in modelPack.Models )
                {
                    foreach ( var material in model.Materials )
                    {
                        var materialHash = material.GetPresetHashCode();
                        var isTextured = material.TextureId.HasValue;
                        var hasOverlay = material.OverlayTextureIds != null;

                        lock ( materialCache )
                        {
                            var inCache = materialCache.ContainsKey( materialHash );
                            if ( !inCache || materialCache[materialHash].IsTextured != isTextured || materialCache[materialHash].HasOverlay != hasOverlay )
                            {
                                var id = materialCache.Count;
                                if ( inCache )
                                {
                                    id = materialCache[materialHash].Id;
                                }

                                var json = JsonConvert.SerializeObject( material, Formatting.Indented );
                                var name = id.ToString();

                                if ( isTextured )
                                    name += "_d";

                                if ( hasOverlay )
                                    name += "_o";

                                File.WriteAllText( $"material_presets\\{name}.json",
                                                   json );

                                if ( !inCache )
                                {
                                    materialCache[materialHash] = (materialCache.Count, isTextured, hasOverlay);
                                }
                            }
                        }
                    }
                }
            } );

            var materialIdLookup = new Dictionary<int, int>();
            foreach ( var tuple in materialCache )
            {
                materialIdLookup[tuple.Key] = tuple.Value.Id;
            }

            File.WriteAllText( "material_presets\\index.json", JsonConvert.SerializeObject( materialIdLookup, Formatting.Indented ) );
        }

        private static void ReplaceModelTest()
        {
            var modelPack = new ModelPack( @"..\..\..\..\Resources\player_a.PB" );
            modelPack.Replace( "player_a_test.fbx" );
            modelPack.Save( @"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\model\field\player_a.PB" );
            return;
        }
    }
}
