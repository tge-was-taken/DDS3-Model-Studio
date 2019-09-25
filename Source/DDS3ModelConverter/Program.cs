using AtlusFileSystemLibrary;
using AtlusFileSystemLibrary.FileSystems.LB;
using DDS3ModelLibrary.IO;
using DDS3ModelLibrary.Models;
using DDS3ModelLibrary.Models.Conversion;
using DDS3ModelLibrary.Models.Field;
using DDS3ModelLibrary.Motions.Conversion;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace DDS3ModelConverter
{
    internal enum InputFormat
    {
        Unknown,
        PB,
        MB,
        F1,
        OBJ,
        DAE,
        FBX,
    }

    internal enum OutputFormat
    {
        Unknown,
        PB,
        MB,
        F1,
        OBJ,
        DAE,
        FBX
    }

    internal class Program
    {
        private static AssemblyName sAssemblyName = Assembly.GetExecutingAssembly().GetName();

        public static string Name => sAssemblyName.Name;

        public static Version Version => sAssemblyName.Version;

        public static string Usage { get; } = $@"
{Name} {Version} by TGE (2019)
A model converter for DDS3 engine games.

Usage:
{Name} <command> <args> [optional parameters]

Commands:
--input <filepath>
Specifies the path to the file to use as input.

--input-format <auto|pb|mb|f1|obj|dae|fbx>
Specifies the input format of the specified input file. Default is auto.

--output <filepath>
Specifies the path to the file to save the output to.

--output-format <auto|pb|mb|f1|obj|dae|fbx>    
Specifies the conversion output format.

--assimp-input-anim
When specifies, the input is treated as an animation file, rather than a model file which affects the conversion process.

--assimp-output-pb-anim
When specified, animations found within the given packed model file are exported when exporting a PB model obj/dae/fbx.

--pb-replace-input <filepath>
Specifies the base PB file to use for the conversion.

--pb-replace-motion-index <index>
Specifies the index of the motion in the PB file to replace.

--pb-replace-motion-pack-index <index>
Specifies the index of the motion pack in the PB file to replace. Default is 0.

--pb-replace-motion-model-index <index>
Specifies the index of the model in the PB file to use when repplacing motions. Default is 0.

--tmx-scale <decimal scale factor>
Specifies the scaling used for texture conversions. Default is 1.0.

--mb-material-overlays
When specified, enables the usage of overlay materials.

--mb-unweighted-mesh-type <1|8>
Specifies the mesh type to be used for unweighted meshes. Default is 1.

--mb-weighted-mesh-type <2|7>
Specifies the mesh type to be used for weighted meshes. Default is 7.

--mb-mesh-weight-limit <integer>
Specifies the max number of weights to be used per mesh. Default is 4. 3 might give better results.

--mb-batch-vertex-limit <integer>
Specifies the max number of vertices to be used per batch. Default is 24.

--f1-lb-replace-input <filepath>
Specifies the base field LB file to use for the conversion.
";

        public static string Input { get; set; }

        public static InputFormat InputFormat { get; set; }

        public static string Output { get; set; }

        public static OutputFormat OutputFormat { get; set; }

        public static bool AssimpInputAnim { get; set; } = false;

        public static bool AssimpOutputPbAnim { get; set; } = false;

        public static string PbReplaceInput { get; set; }

        public static int PbReplaceMotionIndex { get; set; } = -1;

        public static int PbReplaceMotionPackIndex { get; set; } = 0;

        public static int PbReplaceMotionModelIndex { get; set; } = 0;

        public static float TmxScale { get; set; } = 1f;

        public static bool MbMaterialEnableOverlays { get; set; } = false;

        public static MeshType MbWeightedMeshType { get; set; } = MeshType.Type7;

        public static MeshType MbUnweightedMeshType { get; set; } = MeshType.Type1;

        public static int MbMeshWeightLimit { get; set; } = 4;

        public static int MbBatchVertexLimit { get; set; } = 24;

        public static string F1LbReplaceInput { get; set; }

        static void Main( string[] args )
        {
            if ( args.Length == 0 )
            {
                Console.WriteLine( Usage );
                return;
            }

            if ( !ParseArgs( args ) )
            {
                Console.WriteLine( Usage );
                return;
            }

#if !DEBUG
            try
#endif
            {
                switch ( InputFormat )
                {
                    case InputFormat.PB:
                        ConvertPB();
                        break;
                    case InputFormat.MB:
                        ConvertMB();
                        break;
                    case InputFormat.F1:
                        ConvertF1();
                        break;
                    case InputFormat.OBJ:
                    case InputFormat.DAE:
                    case InputFormat.FBX:
                        ConvertAssimpModel();
                        break;
                    default:
                        break;
                }
            }
#if !DEBUG
            catch ( Exception e )
            {
                Console.WriteLine( e.Message );
                return;
            }
#endif

            Console.WriteLine( "Done" );
        }

        private static void ConvertPB()
        {
            var modelPack = new ModelPack(Input);

            switch ( OutputFormat )
            {
                case OutputFormat.PB:
                    modelPack.Save( Output );
                    break;
                case OutputFormat.MB:
                    for ( int i = 0; i < modelPack.Models.Count; i++ )
                        modelPack.Models[i].Save( modelPack.Models.Count == 1 ? Output : $"{Path.GetFileNameWithoutExtension( Output )}_{i}.MB" );
                    break;
                case OutputFormat.OBJ:
                case OutputFormat.DAE:
                case OutputFormat.FBX:
                    for ( int i = 0; i < modelPack.Models.Count; i++ )
                    {
                        var modelOutfilePath = modelPack.Models.Count == 1 ?
                                Output :
                                $"{Path.GetFileNameWithoutExtension( Output )}_{i}.{OutputFormat}";

                        AssimpModelExporter.Instance.Export( modelPack.Models[i], modelOutfilePath, modelPack.TexturePack );

                        if ( AssimpOutputPbAnim )
                        {
                            for ( int j = 0; j < modelPack.MotionPacks.Count; j++ )
                            {
                                for ( int k = 0; k < modelPack.MotionPacks[j].Motions.Count; k++ )
                                {
                                    var outfilePath = modelPack.MotionPacks.Count == 1 ?
                                            $"{Path.GetFileNameWithoutExtension( Output )}_m_{j}.{OutputFormat}" :
                                            $"{Path.GetFileNameWithoutExtension( Output )}_mp_{j}_m_{k}.{OutputFormat}";

                                    AssimpMotionExporter.Instance.Export( modelPack.Models[i], modelPack.MotionPacks[j].Motions[k], outfilePath );
                                }
                            }
                        }
                    }
                    break;
                default:
                    throw new Exception( "Unsupported output format" );
            }
        }

        private static void ConvertMB()
        {
            var model = Resource.Load<Model>(Input);

            switch ( OutputFormat )
            {
                case OutputFormat.PB:
                    var modelPack = new ModelPack();
                    modelPack.Models.Add( model );
                    modelPack.Save( Output );
                    break;
                case OutputFormat.MB:
                    model.Save( Output );
                    break;
                case OutputFormat.OBJ:
                case OutputFormat.DAE:
                case OutputFormat.FBX:
                    AssimpModelExporter.Instance.Export( model, Output );
                    break;
                default:
                    throw new Exception( "Unsupported output format" );
            }
        }

        private static void ConvertF1()
        {
            var fieldScene = new FieldScene(Input);

            switch ( OutputFormat )
            {
                case OutputFormat.F1:
                    fieldScene.Save( Output );
                    break;
                case OutputFormat.OBJ:
                case OutputFormat.DAE:
                case OutputFormat.FBX:
                    {
                        foreach ( var obj in fieldScene.Objects )
                        {
                            if ( obj.ResourceType != FieldObjectResourceType.Model || obj.Resource == null )
                                continue;

                            var model = ( Model )obj.Resource;
                            model.Nodes[0].Transform *= obj.Transform.Matrix;
                            AssimpModelExporter.Instance.Export( model, 
                                Path.Combine( Path.GetDirectoryName( Output ), obj.Name, Path.GetExtension( Output ) ) );
                        }
                    }
                    break;
                default:
                    throw new Exception( "Unsupported output format" );
            }
        }

        private static void ConvertAssimpModel()
        {
            switch ( OutputFormat )
            {
                case OutputFormat.PB:
                    if ( !AssimpInputAnim )
                    {
                        var modelPack = new ModelPack();
                        if ( PbReplaceInput != null )
                            modelPack.Load( PbReplaceInput );

                        modelPack.Replace( Input, TmxScale, MbMaterialEnableOverlays, MbWeightedMeshType, MbUnweightedMeshType, MbMeshWeightLimit, MbBatchVertexLimit );
                    }
                    else
                    {
                        var modelPack = new ModelPack();
                        if ( PbReplaceInput != null )
                            modelPack.Load( PbReplaceInput );

                        var newMotion =
                        AssimpMotionImporter.Instance.Import( Input,
                                                          new AssimpMotionImporter.Config
                                                          {
                                                              NodeIndexResolver = n => modelPack.Models[PbReplaceMotionModelIndex].Nodes.FindIndex( x => x.Name == n )
                                                          });
                        
                        if ( PbReplaceMotionIndex < 0 || ( PbReplaceMotionIndex + 1 ) > modelPack.MotionPacks[PbReplaceMotionPackIndex].Motions.Count )
                        {
                            modelPack.MotionPacks[PbReplaceMotionPackIndex].Motions[PbReplaceMotionIndex] = newMotion;
                            modelPack.Save( Output );
                        }
                    }
                    break;
                case OutputFormat.F1:
                    {
                        var modelPack = new ModelPack();
                        var model = new Model();
                        model.Nodes.Add( new Node { Name = "model" } );
                        modelPack.Models.Add( model );
                        modelPack.Replace( Input, TmxScale, MbMaterialEnableOverlays, MbWeightedMeshType, MbUnweightedMeshType, MbMeshWeightLimit, MbBatchVertexLimit );

                        var lb = new LBFileSystem();
                        lb.Load( F1LbReplaceInput );

                        var f1Handle = lb.GetHandle( "F1" );

                        FieldScene f1;
                        using ( var stream = lb.OpenFile( f1Handle ) )
                            f1 = new FieldScene( stream, true );

                        f1.Objects.RemoveAll( x => x.ResourceType == FieldObjectResourceType.Model );
                        f1.Objects.Clear();
                        f1.Objects.Add( new FieldObject() { Id = 0, Name = "model", Transform = new FieldObjectTransform(), Resource = modelPack.Models[0] } );

                        lb.AddFile( f1Handle, f1.Save(), true, ConflictPolicy.Replace );

                        if ( modelPack.TexturePack != null )
                        {
                            var tbHandle = lb.GetHandle( "TBN" );
                            lb.AddFile( tbHandle, modelPack.TexturePack.Save(), true, ConflictPolicy.Replace );
                        }

                        lb.Save( Output );
                    }
                    break;
                default:
                    throw new Exception( "Unsupported output format" );
            }
        }

        static bool ParseArgs(string[] args)
        {
            try
            {
                string inputFormat = null;
                string outputFormat = null;

                for ( int i = 0; i < args.Length; i++ )
                {
                    var cmd = args[i];

                    string GetNextArg()
                    {
                        if ( ( i + 1 )  > ( args.Length - 1 ) )
                            throw new Exception( $"Missing required argument for {cmd}" );

                        return args[++i];
                    }

                    string TryGetNextParam( string defaultValue = "" )
                    {
                        if ( ( i + 1 ) > ( args.Length - 1 ) )
                            return defaultValue;

                        return args[++i];
                    }

                    switch ( cmd )
                    {
                        case "--input":
                            Input = GetNextArg();
                            break;

                        case "--input-format":
                            inputFormat = GetNextArg();
                            break;

                        case "--output":
                            Output = GetNextArg();
                            break;

                        case "--output-format":
                            outputFormat = GetNextArg();
                            break;

                        case "--assimp-input-anim":
                            AssimpInputAnim = true;
                            break;

                        case "--assimp-output-pb-anim":
                            AssimpOutputPbAnim = true;
                            break;

                        case "--pb-replace-input":
                            PbReplaceInput = GetNextArg();
                            break;

                        case "--pb-replace-animation-index":
                            PbReplaceMotionIndex = int.Parse( GetNextArg() );
                            break;

                        case "--tmx-scale":
                            TmxScale = float.Parse( GetNextArg(), NumberStyles.Float, CultureInfo.InvariantCulture );
                            break;

                        case "--mb-material-overlays":
                            MbMaterialEnableOverlays = true;
                            break;

                        case "--mb-unweighted-mesh-type":
                            MbUnweightedMeshType = ( MeshType )( int.Parse( GetNextArg() ) );
                            break;

                        case "--mb-weighted-mesh-type":
                            MbUnweightedMeshType = ( MeshType )( int.Parse( GetNextArg() ) );
                            break;

                        case "--mb-mesh-weight-limit":
                            MbMeshWeightLimit = int.Parse( GetNextArg() );
                            break;

                        case "--mb-batch-vertex-limit":
                            MbBatchVertexLimit = int.Parse( GetNextArg() );
                            break;

                        case "--f1-lb-replace-input":
                            F1LbReplaceInput = GetNextArg();
                            break;

                        default:
                            Console.WriteLine( $"Unrecognized command: {cmd}" );
                            break;
                    }
                }

                //-- Validate given input

                if ( Input == null ) throw new Exception( "Missing input parameter" );

                if ( inputFormat == null || inputFormat.Equals( "auto", StringComparison.OrdinalIgnoreCase ) )
                {
                    InputFormat = ( InputFormat )Enum.Parse( typeof( InputFormat ), Path.GetExtension( Input )
                        .TrimStart( '.' )
                        .ToLower(), true );
                }

                if ( Output == null ) throw new Exception( "Missing output parameter" );

                if ( outputFormat == null || outputFormat.Equals( "auto", StringComparison.OrdinalIgnoreCase ) )
                {
                    OutputFormat = ( OutputFormat )Enum.Parse( typeof( OutputFormat ), Path.GetExtension( Output )
                        .TrimStart( '.' )
                        .ToLower(), true );
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine( e.Message );
                return false;
            }
        }
    }
}
