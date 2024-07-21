using AtlusFileSystemLibrary;
using AtlusFileSystemLibrary.FileSystems.LB;
using DDS3ModelLibrary.Models;
using DDS3ModelLibrary.Models.Conversion;
using DDS3ModelLibrary.Models.Field;
using DDS3ModelLibrary.Motions;
using DDS3ModelLibrary.Textures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace DDS3ModelLibraryCLI
{
    internal class Program
    {
        private static void MatchingTests()
        {
           // Matches
           var tb = new TexturePack(@"F:\Projects\Nocturne\Dump\DDS3\model\field\_test\player_a.TB");
           tb.Save("player_a_new.TB");

           // Matches
           var mb = new Model(@"F:\Projects\Nocturne\Dump\DDS3\model\field\_test\player_a.MB");
           mb.Save("player_a_new.MB");

            // Matches
           var ab = new MotionPack(@"F:\Projects\Nocturne\Dump\DDS3\model\field\_test\player_a_0.AB");
           ab.Save("player_a_0_new.AB");
        }

        private static void ReimportTests()
        {
            var pb = new ModelPack(@"F:\Projects\Nocturne\Dump\DDS3\model\field\player_a.PB");
            AssimpModelExporter.Instance.Export(pb.Models[0], @"player_a.DAE", pb.TexturePack);
            pb.Replace("player_a.DAE", enableOverlays: true);
            pb.Save("player_a_new.PB");
        }

        private static void Main(string[] args)
        {
            //MatchingTests();
            ReimportTests();

            //var resourcePath = @"..\..\..\..\..\Resources";;

            //var model = new ModelPack(@"F:\Projects\Nocturne\Dump\DDS3\model\field\player_a.PB");
            ////FbxModelExporter.Instance.Export(model.Models[0], "player_a.fbx", new FbxModelExporterConfig()
            ////{
            ////    MergeMeshes = true,
            ////}, model.TexturePack);
            //AssimpModelExporter.Instance.Export(model.Models[0], "player_a.dae", new AssimpModelExporter.Config()
            //{

            //}, model.TexturePack);
            ////model.Replace("player_A.fbx");
            //model.Replace("player_A_new.fbx");
            //model.Save(@"F:\Projects\Nocturne\Hostfs\dds3data\model\field\player_a.PB");

            //var fieldModel2 = new FieldScene(@"D:\Software\Games\PS2\Shin Megami Tensei - Digital Devil Saga (USA)\DDS3\fld\f\f010\f010_002\02_00.F1");

            //var fieldModel = new FieldScene(@"F:\Projects\Nocturne\Hostfs\dds3data\fld\f\f015\f015_006.LB_unpacked\02_00.F1");
            //fieldModel.Objects.RemoveAll(x => x.ResourceType == FieldObjectResourceType.Model);
            //foreach (var item in fieldModel2.Objects.Where(x =>x.ResourceType == FieldObjectResourceType.Model))
            //{
            //    fieldModel.Objects.Add(
            //        new FieldObject() 
            //        { 
            //            Id = fieldModel.Objects.Max(x => x.Id) + 1, 
            //            Name = item.Name,
            //            Transform = item.Transform, 
            //            Resource = item.Resource
            //        });
            //}
            //fieldModel.Save(@"F:\Projects\Nocturne\Hostfs\dds3data\fld\f\f015\f015_006.LB_unpacked\02_00.F1");

            #region Old stuff
            //{
            //    File.Delete( "test.fbx" );
            //    var modelPack = new ModelPack( @"D:\dumps\smt3_ntsc\DDS3\model\field\player_a.PB" );
            //    //var modelPack = new ModelPack( @"D:\dumps\smt3_ntsc\DDS3\model\devil\on\0x126_on.PB");
            //    FbxModelExporter.Instance.Export( modelPack.Models[0] , "test.fbx" , modelPack.TexturePack );
            //    return;

            //    //AssimpModelExporter.Instance.Export( modelPack.Models[ 0 ], "player_a.dae", modelPack.TexturePack );
            //    //for ( var i = 0; i < modelPack.MotionPacks[ 0 ].Motions.Count; i++ )
            //    //{
            //    //    var motion = modelPack.MotionPacks[ 0 ].Motions[ i ];
            //    //    if ( motion == null )
            //    //        continue;

            //    //    AssimpMotionExporter.Instance.Export( modelPack.Models[ 0 ], motion, $"player_a_motion_{i:D2}.dae" );
            //    //}

            //    var newMotion =
            //        AssimpMotionImporter.Instance.Import( @"D:\Users\smart\Desktop\nocturne_player_a_fortnite.fbx",
            //                                              new AssimpMotionImporter.Config
            //                                              {
            //                                                  NodeIndexResolver = n => modelPack.Models[0].Nodes.FindIndex( x => x.Name == n )
            //                                              } );
            //    for ( int i = 0; i < modelPack.MotionPacks[0].Motions.Count; i++ )
            //    {
            //        modelPack.MotionPacks[0].Motions[i] = newMotion;
            //    }

            //    modelPack.Save( @"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\model\field\player_a.PB" );
            //}

            //{
            //    var lb = new LBFileSystem();
            //    lb.Load( @"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\fld\f\f037\_f037_027.LB" );

            //    var f1Handle = lb.GetHandle( "F1" );

            //    FieldScene f1;
            //    using ( var stream = lb.OpenFile( f1Handle ) )
            //        f1 = new FieldScene( stream, true );

            //    foreach ( var obj in f1.Objects )
            //    {
            //        switch ( obj.ResourceType )
            //        {
            //            case FieldObjectResourceType.Model:
            //                {
            //                    var model = ( Model ) obj.Resource;
            //                    foreach ( var material in model.Materials )
            //                    {
            //                        if ( material.TextureId.HasValue )
            //                            material.TextureId = 0;

            //                        material.Color1 = material.Color2 = material.Color3 = material.Color4 = material.Color5 = null;
            //                        material.Float1 = null;
            //                        material.FloatArray1 = material.FloatArray2 = material.FloatArray3 = null;
            //                    }

            //                    foreach ( var node in model.Nodes )
            //                    {
            //                        if ( node.Geometry == null )
            //                            continue;

            //                        foreach ( var _mesh in node.Geometry.Meshes )
            //                        {
            //                            if ( _mesh is MeshType1 mesh )
            //                            {
            //                                foreach ( var batch in mesh.Batches )
            //                                {
            //                                    batch.Flags &= ~MeshFlags.Normal;
            //                                    batch.Flags &= ~MeshFlags.Color;
            //                                }
            //                            }
            //                        }
            //                    }
            //                }
            //                break;
            //            case FieldObjectResourceType.Type3:
            //                break;
            //            case FieldObjectResourceType.TextureListFileName:
            //                break;
            //            case FieldObjectResourceType.Effect:
            //                break;
            //            case FieldObjectResourceType.Light:
            //                break;
            //        }
            //    }
            //    ExportObj( f1 );

            //    lb.AddFile( f1Handle, f1.Save(), true, ConflictPolicy.Replace );

            //    var tbHandle = lb.GetHandle( "TBN" );
            //    var texturePack = new TexturePack();
            //    texturePack.Textures.Add( new Texture( new Bitmap( @"D:\Modding\Tools\magenta.png" ) ) );
            //    lb.AddFile( tbHandle, texturePack.Save(), true, ConflictPolicy.Replace );

            //    lb.Save( @"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\fld\f\f037\f037_027.LB" );
            //}

            //OpenAndSaveModelPackTest();
            //ReplaceF1Test();
            //ReplaceModelTest();
            //OpenAndSaveModelPackBatchTest();
            //return;
            //ExportObj( new ModelPack( @"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\model\field\player_b.PB" ) );
            //return;
            //OpenAndSaveModelPackBatchTest();return;
            //OpenAndSaveFieldSceneBatchTest();return;
            //ReplaceModelTest();return;

            //var modelPack = new ModelPack( @"..\..\..\..\Resources\player_a.PB" );
            //var modelPack = new ModelPack( @"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\model\field\player_a.PB" );

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



            //modelPack.Save( @"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\model\field\player_a.PB" );


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
            #endregion
        }

        private static void ReplaceF1Test()
        {
            var modelPack = new ModelPack();
            var model = new Model();
            model.Nodes.Add(new Node { Name = "model" });
            modelPack.Models.Add(model);
            modelPack.Replace("f1test.fbx");

            var lb = new LBFileSystem();
            lb.Load(@"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\fld\f\f037\_f037_027.LB");

            var f1Handle = lb.GetHandle("F1");

            FieldScene f1;
            using (var stream = lb.OpenFile(f1Handle))
                f1 = new FieldScene(stream, true);

            f1.Objects.RemoveAll(x => x.ResourceType == FieldObjectResourceType.Model);
            f1.Objects.Clear();
            f1.Objects.Add(new FieldObject() { Id = 0, Name = "model", Transform = new FieldObjectTransform(), Resource = modelPack.Models[0] });
            ExportObj(f1);

            lb.AddFile(f1Handle, f1.Save(), true, ConflictPolicy.Replace);

            if (modelPack.TexturePack != null)
            {
                var tbHandle = lb.GetHandle("TBN");
                lb.AddFile(tbHandle, modelPack.TexturePack.Save(), true, ConflictPolicy.Replace);
            }

            lb.Save(@"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\fld\f\f037\f037_027.LB");
        }

        private static void WriteMesh(StreamWriter streamWriter, Model model, Node node1, Mesh _mesh, int meshIndex1, bool isMesh2, ref int _vertexBaseIndex, FieldObject fieldObj = null)
        {
            var vertexBaseIndex = _vertexBaseIndex;
            void WritePositions(StreamWriter writer, Vector3[] positions)
            {
                for (var i = 0; i < positions.Length; i++)
                {
                    var position = positions[i];
                    if (fieldObj?.Transform != null)
                        position = Vector3.Transform(position, fieldObj.Transform.Matrix);

                    writer.WriteLine($"v {position.X} {position.Y} {position.Z}");
                }
            }

            void WriteNormals(StreamWriter writer, Vector3[] normals)
            {
                for (var i = 0; i < normals.Length; i++)
                {
                    var normal = normals[i];
                    if (fieldObj?.Transform != null)
                        normal = Vector3.TransformNormal(normal, fieldObj.Transform.Matrix);

                    writer.WriteLine($"vn {normal.X} {normal.Y} {normal.Z}");
                }
            }

            void WriteTexCoords(StreamWriter writer, Vector2[] texCoords)
            {
                foreach (var texCoord in texCoords)
                    writer.WriteLine($"vt {texCoord.X} {texCoord.Y}");
            }

            void WriteTriangles(StreamWriter writer, Node node, int meshIndex, Triangle[] triangles, MeshType meshType,
                                 int shapeIndex = 0)
            {
                writer.WriteLine($"o {fieldObj?.Name ?? ""}_node_{node.Name}_mesh{(isMesh2 ? "2" : "")}_{meshIndex}_{meshType}_{shapeIndex}");
                foreach (var triangle in triangles)
                {
                    writer.WriteLine("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", vertexBaseIndex + triangle.A + 1,
                                      vertexBaseIndex + triangle.B + 1, vertexBaseIndex + triangle.C + 1);
                }
            }

            {
                switch (_mesh.Type)
                {
                    case MeshType.Type1:
                        {
                            var mesh = (MeshType1)_mesh;
                            foreach (var batch in mesh.Batches)
                            {
                                (Vector3[] positions, Vector3[] normals) = batch.Transform(node1.WorldTransform);
                                WritePositions(streamWriter, positions);

                                WriteNormals(streamWriter, batch.Normals != null ? normals : new Vector3[positions.Length]);
                                WriteTexCoords(streamWriter, batch.TexCoords ?? new Vector2[positions.Length]);
                                WriteTriangles(streamWriter, node1, meshIndex1, batch.Triangles, MeshType.Type1);
                                vertexBaseIndex += batch.VertexCount;
                            }
                        }
                        break;
                    case MeshType.Type2:
                        {
                            var mesh = (MeshType2)_mesh;
                            foreach (var batch in mesh.Batches)
                            {
                                (Vector3[] positions, Vector3[] normals, _) = batch.Transform(model.Nodes);
                                WritePositions(streamWriter, positions);
                                WriteNormals(streamWriter, normals);
                                WriteTexCoords(streamWriter, batch.TexCoords ?? new Vector2[positions.Length]);
                                WriteTriangles(streamWriter, node1, meshIndex1, batch.Triangles, MeshType.Type2);
                                vertexBaseIndex += batch.VertexCount;
                            }
                        }
                        break;
                    case MeshType.Type3:
                        break;
                    case MeshType.Type4:
                        {
                            var mesh = (MeshType4)_mesh;
                            (Vector3[] positions, Vector3[] normals) = mesh.Transform(node1.WorldTransform);
                            WritePositions(streamWriter, positions);
                            WriteNormals(streamWriter, normals);
                            WriteTexCoords(streamWriter, new Vector2[positions.Length]);
                            WriteTriangles(streamWriter, node1, meshIndex1, mesh.Triangles, MeshType.Type4);
                            vertexBaseIndex += mesh.VertexCount;
                        }
                        break;
                    case MeshType.Type5:
                        {
                            var mesh = (MeshType5)_mesh;
                            var shapes = mesh.Transform(node1.WorldTransform);
                            for (var i = 0; i < shapes.Length; i++)
                            {
                                var shape = shapes[i];
                                WritePositions(streamWriter, shape.Positions);
                                WriteNormals(streamWriter, shape.Normals);
                                WriteTexCoords(streamWriter, mesh.TexCoords);
                                WriteTriangles(streamWriter, node1, meshIndex1, mesh.Triangles, MeshType.Type5, i);
                                vertexBaseIndex += shape.Positions.Length;
                            }
                        }
                        break;
                    case MeshType.Type7:
                        {
                            var mesh = (MeshType7)_mesh;
                            foreach (var batch in mesh.Batches)
                            {
                                (Vector3[] positions, Vector3[] normals, _) = batch.Transform(model.Nodes);

                                WritePositions(streamWriter, positions);
                                WriteNormals(streamWriter, normals);
                                WriteTexCoords(streamWriter, batch.TexCoords);
                            }

                            WriteTriangles(streamWriter, node1, meshIndex1, mesh.Triangles, MeshType.Type7);
                            vertexBaseIndex += _mesh.VertexCount;
                        }
                        break;
                    case MeshType.Type8:
                        {
                            var mesh = (MeshType8)_mesh;
                            foreach (var batch in mesh.Batches)
                            {
                                (Vector3[] positions, Vector3[] normals) = batch.Transform(node1.WorldTransform);
                                WritePositions(streamWriter, positions);
                                WriteNormals(streamWriter, normals);
                                WriteTexCoords(streamWriter, batch.TexCoords);
                            }

                            WriteTriangles(streamWriter, node1, meshIndex1, mesh.Triangles, MeshType.Type8);
                            vertexBaseIndex += _mesh.VertexCount;
                        }
                        break;
                }
            }

            _vertexBaseIndex = vertexBaseIndex;
        }

        public static void ExportObj(ModelPack modelPack, string fileName = "test.obj")
        {
            var vertexBaseIndex = 0;

            using (var writer = File.CreateText(fileName))
            {
                foreach (var model in modelPack.Models)
                {
                    foreach (var node in model.Nodes)
                    {
                        if (node.Geometry == null)
                            continue;

                        if (node.Geometry.MeshLists[0] != null)
                        {
                            for (var meshIndex = 0; meshIndex < node.Geometry.MeshLists[0].Count; meshIndex++)
                            {
                                var mesh = node.Geometry.MeshLists[0][meshIndex];
                                writer.WriteLine($"// node '{node.Name}' mesh ({mesh.Type}) #{meshIndex}");
                                WriteMesh(writer, model, node, mesh, meshIndex, false, ref vertexBaseIndex);
                            }
                        }

                        if (node.Geometry.MeshLists[1] != null)
                        {
                            for (var meshIndex = 0; meshIndex < node.Geometry.MeshLists[1].Count; meshIndex++)
                            {
                                var mesh = node.Geometry.MeshLists[1][meshIndex];
                                writer.WriteLine($"// node '{node.Name}' mesh(2) ({mesh.Type}) #{meshIndex}");
                                WriteMesh(writer, model, node, mesh, meshIndex, true, ref vertexBaseIndex);
                            }
                        }


                        if (node.Geometry.MeshLists[2] != null)
                        {
                            for (var meshIndex = 0; meshIndex < node.Geometry.MeshLists[3].Count; meshIndex++)
                            {
                                var mesh = node.Geometry.MeshLists[3][meshIndex];
                                writer.WriteLine($"// node '{node.Name}' mesh(3) ({mesh.Type}) #{meshIndex}");
                                WriteMesh(writer, model, node, mesh, meshIndex, true, ref vertexBaseIndex);
                            }
                        }
                    }
                }
            }
        }

        public static void ExportObj(Model model)
        {
            var vertexBaseIndex = 0;

            using (var writer = File.CreateText("test.obj"))
            {
                foreach (var node in model.Nodes)
                {
                    if (node.Geometry == null)
                        continue;

                    if (node.Geometry.MeshLists[0] != null)
                    {
                        for (var meshIndex = 0; meshIndex < node.Geometry.MeshLists[0].Count; meshIndex++)
                        {
                            var mesh = node.Geometry.MeshLists[0][meshIndex];
                            writer.WriteLine($"// node '{node.Name}' mesh ({mesh.Type}) #{meshIndex}");
                            WriteMesh(writer, model, node, mesh, meshIndex, false, ref vertexBaseIndex);
                        }
                    }

                    if (node.Geometry.MeshLists[1] != null)
                    {
                        for (var meshIndex = 0; meshIndex < node.Geometry.MeshLists[0].Count; meshIndex++)
                        {
                            var mesh = node.Geometry.MeshLists[1][meshIndex];
                            writer.WriteLine($"// node '{node.Name}' mesh(2) ({mesh.Type}) #{meshIndex}");
                            WriteMesh(writer, model, node, mesh, meshIndex, true, ref vertexBaseIndex);
                        }
                    }


                    if (node.Geometry.MeshLists[2] != null)
                    {
                        for (var meshIndex = 0; meshIndex < node.Geometry.MeshLists[2].Count; meshIndex++)
                        {
                            var mesh = node.Geometry.MeshLists[2][meshIndex];
                            writer.WriteLine($"// node '{node.Name}' mesh(3) ({mesh.Type}) #{meshIndex}");
                            WriteMesh(writer, model, node, mesh, meshIndex, true, ref vertexBaseIndex);
                        }
                    }
                }
            }
        }

        public static void ExportObj(FieldScene field, string fileName = "test.obj")
        {
            var vertexBaseIndex = 0;
            Directory.CreateDirectory("obj");

            using (var writer = File.CreateText(Path.Combine("obj\\", fileName)))
            {
                foreach (var obj in field.Objects)
                {
                    if (obj.ResourceType != FieldObjectResourceType.Model || obj.Resource == null)
                    {
                        continue;
                    }

                    var model = (Model)obj.Resource;
                    writer.WriteLine($"// field model '{obj.Name}'");
                    foreach (var node in model.Nodes)
                    {
                        if (node.Geometry == null)
                            continue;

                        if (node.Geometry.MeshLists[0] != null)
                        {
                            for (var meshIndex = 0; meshIndex < node.Geometry.MeshLists[0].Count; meshIndex++)
                            {
                                var mesh = node.Geometry.MeshLists[0][meshIndex];
                                writer.WriteLine($"// node '{node.Name}' mesh ({mesh.Type}) #{meshIndex}");
                                WriteMesh(writer, model, node, mesh, meshIndex, false, ref vertexBaseIndex, obj);
                            }
                        }

                        if (node.Geometry.MeshLists[1] != null)
                        {
                            for (var meshIndex = 0; meshIndex < node.Geometry.MeshLists[1].Count; meshIndex++)
                            {
                                var mesh = node.Geometry.MeshLists[1][meshIndex];
                                writer.WriteLine($"// node '{node.Name}' mesh(2) ({mesh.Type}) #{meshIndex}");
                                WriteMesh(writer, model, node, mesh, meshIndex, true, ref vertexBaseIndex, obj);
                            }
                        }

                        if (node.Geometry.MeshLists[2] != null)
                        {
                            for (var meshIndex = 0; meshIndex < node.Geometry.MeshLists[2].Count; meshIndex++)
                            {
                                var mesh = node.Geometry.MeshLists[2][meshIndex];
                                writer.WriteLine($"// node '{node.Name}' mesh(3) ({mesh.Type}) #{meshIndex}");
                                WriteMesh(writer, model, node, mesh, meshIndex, true, ref vertexBaseIndex, obj);
                            }
                        }
                    }
                }
            }
        }

        private static void OpenAndSaveModelPackTest()
        {
            var modelPack = new ModelPack(@"..\..\..\..\Resources\player_a.PB");
            modelPack.Save(@"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\model\field\player_a.PB");
        }

        private static string GetChecksum(string file)
        {
            using (FileStream stream = File.OpenRead(file))
            {
                return GetChecksum(stream);
            }
        }

        private static string GetChecksum(Stream stream)
        {
            var sha = new SHA256Managed();
            byte[] checksum = sha.ComputeHash(stream);
            return BitConverter.ToString(checksum).Replace("-", String.Empty);
        }

        private static void FindUniqueFiles(string outDirectory, string searchDirectory, string extension)
        {
            Directory.CreateDirectory(outDirectory);

            var checksums = new HashSet<string>();
            var paths = Directory.EnumerateFiles(searchDirectory, "*" + extension, SearchOption.AllDirectories).ToList();
            var done = 0;
            Parallel.ForEach(paths, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, (path) =>
            {
                Console.WriteLine($"{done}/{paths.Count} {path}");

                var checksum = GetChecksum(path);

                lock (checksums)
                {
                    if (!checksums.Contains(checksum))
                    {
                        File.Copy(path, Path.Combine(outDirectory, Path.GetFileNameWithoutExtension(path) + "_" + checksum + extension), true);
                        checksums.Add(checksum);
                    }
                }

                ++done;
            });
        }

        private static void OpenAndSaveModelPackBatchTest()
        {
            if (!Directory.Exists("unique_models"))
                FindUniqueFiles("unique_models", @"D:\Modding\DDS3", ".PB");

            if (!Directory.Exists("unique_lb"))
            {
                FindUniqueFiles("unique_lb", @"D:\Modding\DDS3", ".LB");
            }

            if (!Directory.Exists("unique_lb_extracted"))
            {
                Directory.CreateDirectory("unique_lb_extracted");
                var checksums = new HashSet<string>();
                Parallel.ForEach(Directory.EnumerateFiles("unique_lb"), (path) =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    var outPath = $"unique_lb_extracted\\";
                    Directory.CreateDirectory(outPath);

                    using (var lb = new AtlusFileSystemLibrary.FileSystems.LB.LBFileSystem())
                    {
                        lb.Load(path);

                        foreach (var file in lb.EnumerateFiles())
                        {
                            var info = lb.GetInfo(file);
                            using (var stream = lb.OpenFile(file))
                            {
                                var checksum = GetChecksum(stream);
                                stream.Position = 0;
                                var extract = false;

                                lock (checksums)
                                {
                                    if (!checksums.Contains(checksum))
                                    {
                                        checksums.Add(checksum);
                                        extract = true;
                                    }
                                }

                                if (extract)
                                {
                                    var nameParts = Path.GetFileNameWithoutExtension(path).Split(new[] { '_' });
                                    Array.Resize(ref nameParts, nameParts.Length - 1);

                                    using (var fileStream =
                                        File.Create(Path.Combine(outPath, string.Concat(nameParts) + "_" + file + "_" + checksum + "." + info.Extension)
                                                   ))
                                    {
                                        Console.WriteLine($"Extracting: {fileName} #{file} ({info.UserId:D2}, {info.Extension})");
                                        stream.CopyTo(fileStream);
                                    }
                                }
                            }
                        }
                    }
                });
            }

            var uniqueValues = new HashSet<MeshFlags>();
            var frequencyMap = new ConcurrentDictionary<MeshFlags, int>();

            var paths = Directory.EnumerateFiles("unique_models", "*.PB", SearchOption.AllDirectories).ToList();
            var done = 0;
            //foreach ( var path in paths )
            Parallel.ForEach(paths, new ParallelOptions() { MaxDegreeOfParallelism = 16 }, (path) =>
            {
                Console.WriteLine(Path.GetFileName(path));
                var modelPack = new ModelPack(path);
                new ModelPack(modelPack.Save());
                //modelPack.Save( path + ".out" );
                //new ModelPack( path + ".out" );
                //ExportObj( modelPack, Path.GetFileNameWithoutExtension( path ) + ".obj" );
            }
             );

            //var modelPack = new ModelPack( @"D:\Programming\Repos\DDS3-Model-Studio\Source\DDS3ModelLibraryCLI\bin\Debug\unique_models\tiaki_DF1D93D2EC3ED36D63239A1A2B27ED1745DA9991F231884EA7BC5FAF291F74AA.PB" );
            //modelPack.Save( @"D:\Programming\Repos\DDS3-Model-Studio\Source\DDS3ModelLibraryCLI\bin\Debug\unique_models\tiaki_DF1D93D2EC3ED36D63239A1A2B27ED1745DA9991F231884EA7BC5FAF291F74AA.PB" + ".out" );


            //foreach ( var kvp in frequencyMap.OrderBy( x => x.Value ) )
            //{
            //    Console.WriteLine( kvp.Value + " " + kvp.Key );
            //}

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

        private static void OpenAndSaveFieldSceneBatchTest()
        {
            if (!Directory.Exists("unique_f1"))
                FindUniqueFiles("unique_f1", @"D:\Modding\DDS3", ".F1");

            if (!Directory.Exists("unique_lb"))
                FindUniqueFiles("unique_lb", @"D:\Modding\DDS3", ".LB");

            if (!Directory.Exists("unique_lb_extracted"))
            {
                Directory.CreateDirectory("unique_lb_extracted");
                var checksums = new HashSet<string>();
                Parallel.ForEach(Directory.EnumerateFiles("unique_lb"), (path) =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    var outPath = $"unique_lb_extracted\\";
                    Directory.CreateDirectory(outPath);

                    using (var lb = new AtlusFileSystemLibrary.FileSystems.LB.LBFileSystem())
                    {
                        lb.Load(path);

                        foreach (var file in lb.EnumerateFiles())
                        {
                            var info = lb.GetInfo(file);
                            using (var stream = lb.OpenFile(file))
                            {
                                var checksum = GetChecksum(stream);
                                stream.Position = 0;
                                var extract = false;

                                lock (checksums)
                                {
                                    if (!checksums.Contains(checksum))
                                    {
                                        checksums.Add(checksum);
                                        extract = true;
                                    }
                                }

                                if (extract)
                                {
                                    var nameParts = Path.GetFileNameWithoutExtension(path).Split(new[] { '_' });
                                    Array.Resize(ref nameParts, nameParts.Length - 1);

                                    using (var fileStream =
                                        File.Create(Path.Combine(outPath, string.Concat(nameParts) + "_" + file + "_" + checksum + "." + info.Extension)
                                                   ))
                                    {
                                        Console.WriteLine($"Extracting: {fileName} #{file} ({info.UserId:D2}, {info.Extension})");
                                        stream.CopyTo(fileStream);
                                    }
                                }
                            }
                        }
                    }
                });
            }

            //FindUniqueFiles( "unique_f1", "unique_lb_extracted", ".F1" );


            var uniqueValues = new HashSet<MeshFlags>();
            var frequencyMap = new ConcurrentDictionary<MeshFlags, int>();

            var paths = Directory.EnumerateFiles("unique_f1", "*.F1", SearchOption.AllDirectories).ToList();
            var done = 0;
            Parallel.ForEach(paths, new ParallelOptions() { MaxDegreeOfParallelism = 16 }, (path) =>
            {
                if (path != "unique_f1\\f500_001_9E6A1BA4D63DD8AA05144CEF3768A380EEAFD2E6D620C24CE85DC173533B5992.F1")
                {
                    Console.WriteLine(Path.GetFileName(path));
                    var field = new FieldScene(path);
                    //ExportObj( field, Path.GetFileNameWithoutExtension( path ) + ".obj" );
                    new FieldScene(field.Save());
                    //field.Save( path + ".out" );
                    //new FieldScene( path + ".out" );
                    //File.Delete( path + ".out" );
                }
            });

            foreach (var kvp in frequencyMap.OrderBy(x => x.Value))
            {
                Console.WriteLine(kvp.Value + " " + kvp.Key);
            }
        }

        private static void GenerateMaterialPresets()
        {
            var paths = Directory.EnumerateFiles("unique_models", "*.PB", SearchOption.AllDirectories).ToList();
            Directory.CreateDirectory("material_presets");

            var materialCache = new Dictionary<int, (int Id, bool IsTextured, bool HasOverlay)>();
            var done = 0;
            Parallel.ForEach(paths, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, (path) =>
            {
                Console.WriteLine(Path.GetFileName(path));
                var modelPack = new ModelPack(path);
                foreach (var model in modelPack.Models)
                {
                    foreach (var material in model.Materials)
                    {
                        var materialHash = material.GetPresetHashCode();
                        var isTextured = material.TextureId.HasValue;
                        var hasOverlay = material.OverlayTextureIds != null;

                        lock (materialCache)
                        {
                            var inCache = materialCache.ContainsKey(materialHash);
                            if (!inCache || materialCache[materialHash].IsTextured != isTextured || materialCache[materialHash].HasOverlay != hasOverlay)
                            {
                                var id = materialCache.Count;
                                if (inCache)
                                {
                                    id = materialCache[materialHash].Id;
                                }

                                var json = JsonSerializer.Serialize(material, new JsonSerializerOptions() { WriteIndented = true });
                                var name = id.ToString();

                                if (isTextured)
                                    name += "_d";

                                if (hasOverlay)
                                    name += "_o";

                                File.WriteAllText($"material_presets\\{name}.json",
                                                   json);

                                if (!inCache)
                                {
                                    materialCache[materialHash] = (materialCache.Count, isTextured, hasOverlay);
                                }
                            }
                        }
                    }
                }
            });

            var materialIdLookup = new Dictionary<int, int>();
            foreach (var tuple in materialCache)
            {
                materialIdLookup[tuple.Key] = tuple.Value.Id;
            }

            File.WriteAllText("material_presets\\index.json", JsonSerializer.Serialize(materialIdLookup, new JsonSerializerOptions() { WriteIndented = true }));
        }

        private static void ReplaceModelTest()
        {
            var modelPack = new ModelPack(@"..\..\..\..\Resources\player_a.PB");
            modelPack.Replace("player_a_test.fbx");
            modelPack.Save(@"D:\Modding\DDS3\Nocturne\_HostRoot\dds3data\model\field\player_a.PB");
        }
    }
}
