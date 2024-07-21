using AtlusFileSystemLibrary;
using AtlusFileSystemLibrary.FileSystems.LB;
using DDS3ModelLibrary.IO;
using DDS3ModelLibrary.Models;
using DDS3ModelLibrary.Models.Conversion;
using DDS3ModelLibrary.Models.Field;
using DDS3ModelLibrary.Motions.Conversion;
using DDS3ModelLibrary.Textures;
using System;
using System.IO;
using TGE.SimpleCommandLine;

namespace DDS3ModelConverter
{
    public enum InputFormat
    {
        Unknown,
        PB,
        MB,
        F1,
        TB,
        OBJ,
        DAE,
        FBX,
    }

    public enum OutputFormat
    {
        Unknown,
        PB,
        MB,
        F1,
        OBJ,
        DAE,
        FBX,
        Folder
    }

    internal class Program
    {
        public static string About { get; } = SimpleCommandLineFormatter.Default.FormatAbout<ProgramOptions>(
            "TGE", "A model converter for DDS3 engine games.");

        public static ProgramOptions Options { get; set; }

        public static FbxModelExporterConfig FbxConfig { get; } =
            new FbxModelExporterConfig()
            {
                ConvertBlendShapesToMeshes = true,
                ExportMultipleUvLayers = true,
                MergeMeshes = true
            };

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(About);
                return;
            }

            if (!ParseArgs(args))
            {
                Console.WriteLine(About);
                return;
            }

#if !DEBUG
            try
#endif
            {
                switch (Options.InputFormat)
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
                    case InputFormat.TB:
                        {
                            var textures = Resource.Load<TexturePack>(Options.Input);
                            var outDirPath = GetDirectoryPath(Options.Output);
                            for (int i = 0; i < textures.Count; i++)
                            {
                                Texture item = (Texture)textures[i];
                                var name = $"texture_{i:D2}.png";
                                item.GetBitmap().Save(Path.Combine(outDirPath, name));
                            }
                        }
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

            Console.WriteLine("Done");
        }

        private static void ConvertPB()
        {
            var modelPack = new ModelPack(Options.Input);

            switch (Options.OutputFormat)
            {
                case OutputFormat.PB:
                    modelPack.Save(Options.Output);
                    break;
                case OutputFormat.MB:
                    for (int i = 0; i < modelPack.Models.Count; i++)
                        modelPack.Models[i].Save(modelPack.Models.Count == 1 ? Options.Output : $"{Path.GetFileNameWithoutExtension(Options.Output)}_{i}.MB");
                    break;
                case OutputFormat.OBJ:
                case OutputFormat.DAE:
                case OutputFormat.FBX:
                    for (int i = 0; i < modelPack.Models.Count; i++)
                    {
                        var modelOutfilePath = modelPack.Models.Count == 1 ?
                                Options.Output :
                                $"{Path.GetFileNameWithoutExtension(Options.Output)}_{i}.{Options.OutputFormat}";

                        if (!Options.Assimp.UseLegacyFbxExporter && (Options.OutputFormat == OutputFormat.DAE || Options.OutputFormat == OutputFormat.FBX))
                            FbxModelExporter.Instance.Export(modelPack.Models[i], modelOutfilePath, FbxConfig, modelPack.TexturePack);
                        else
                            AssimpModelExporter.Instance.Export(modelPack.Models[i], modelOutfilePath, modelPack.TexturePack);

                        if (Options.Assimp.OutputPbMotion)
                        {
                            for (int j = 0; j < modelPack.MotionPacks.Count; j++)
                            {
                                for (int k = 0; k < modelPack.MotionPacks[j].Motions.Count; k++)
                                {
                                    var outfilePath = modelPack.MotionPacks.Count == 1 ?
                                            $"{Path.GetFileNameWithoutExtension(Options.Output)}_m_{j}.{Options.OutputFormat}" :
                                            $"{Path.GetFileNameWithoutExtension(Options.Output)}_mp_{j}_m_{k}.{Options.OutputFormat}";

                                    AssimpMotionExporter.Instance.Export(modelPack.Models[i], modelPack.MotionPacks[j].Motions[k], outfilePath);
                                }
                            }
                        }
                    }
                    break;
                default:
                    throw new Exception("Unsupported output format");
            }
        }

        private static void ConvertMB()
        {
            var model = Resource.Load<Model>(Options.Input);

            switch (Options.OutputFormat)
            {
                case OutputFormat.PB:
                    var modelPack = new ModelPack();
                    modelPack.Models.Add(model);
                    modelPack.Save(Options.Output);
                    break;
                case OutputFormat.MB:
                    model.Save(Options.Output);
                    break;
                case OutputFormat.OBJ:
                case OutputFormat.DAE:
                case OutputFormat.FBX:
                    if (!Options.Assimp.UseLegacyFbxExporter && (Options.OutputFormat == OutputFormat.DAE || Options.OutputFormat == OutputFormat.FBX))
                        FbxModelExporter.Instance.Export(model, Options.Output, FbxConfig, null);
                    else
                        AssimpModelExporter.Instance.Export(model, Options.Output);
                    break;
                default:
                    throw new Exception("Unsupported output format");
            }
        }

        private static void ConvertF1()
        {
            var fieldScene = new FieldScene(Options.Input);

            switch (Options.OutputFormat)
            {
                case OutputFormat.F1:
                    fieldScene.Save(Options.Output);
                    break;
                case OutputFormat.OBJ:
                case OutputFormat.DAE:
                case OutputFormat.FBX:
                    {
                        var outDirPath = GetDirectoryPath(Options.Output);
                        foreach (var obj in fieldScene.Objects)
                        {
                            if (obj.ResourceType != FieldObjectResourceType.Model || obj.Resource == null)
                                continue;

                            var model = (Model)obj.Resource;
                            model.Nodes[0].Transform *= obj.Transform.Matrix;

                            var outFilePath = Path.Combine(outDirPath, obj.Name + Path.GetExtension(Options.Output));
                            if (!Options.Assimp.UseLegacyFbxExporter && (Options.OutputFormat == OutputFormat.DAE || Options.OutputFormat == OutputFormat.FBX))
                                FbxModelExporter.Instance.Export(model, outFilePath, FbxConfig, null);
                            else
                                AssimpModelExporter.Instance.Export(model, outFilePath);
                        }
                    }
                    break;
                default:
                    throw new Exception("Unsupported output format");
            }
        }

        private static string GetDirectoryPath(string path)
        {
            var outDirPath = Path.HasExtension(path) ?
                Path.GetDirectoryName(path) :
                path;
            Directory.CreateDirectory(outDirPath);
            return outDirPath;
        }

        private static void ConvertAssimpModel()
        {
            switch (Options.OutputFormat)
            {
                case OutputFormat.PB:
                    {
                        if (Options.PackedModel.ReplaceInput == null)
                            throw new Exception("You must specify a PB replacement input for conversion to PB");

                        var modelPack = new ModelPack();
                        if (Options.PackedModel.ReplaceInput != null)
                            modelPack.Load(Options.PackedModel.ReplaceInput);

                        if (!Options.Assimp.TreatInputAsAnimation)
                        {
                            modelPack.Replace(Options.Input, Options.TmxScale, Options.Model.EnableMaterialOverlays, Options.Model.WeightedMeshType,
                                Options.Model.UnweightedMeshType, Options.Model.MeshWeightLimit, Options.Model.BatchVertexLimit);
                        }
                        else
                        {
                            var newMotion =
                        AssimpMotionImporter.Instance.Import(Options.Input,
                                                          new AssimpMotionImporter.Config
                                                          {
                                                              NodeIndexResolver = n => modelPack.Models[Options.PackedModel.ReplaceMotionModelIndex].Nodes.FindIndex(x => x.Name == n)
                                                          });

                            if (Options.PackedModel.ReplaceMotionIndex < 0 || (Options.PackedModel.ReplaceMotionIndex + 1) >
                                modelPack.MotionPacks[Options.PackedModel.ReplaceMotionPackIndex].Motions.Count)
                            {
                                modelPack.MotionPacks[Options.PackedModel.ReplaceMotionPackIndex].Motions[Options.PackedModel.ReplaceMotionIndex] = newMotion;
                            }
                        }

                        modelPack.Save(Options.Output);
                    }
                    break;
                case OutputFormat.F1:
                    {
                        var modelPack = new ModelPack();
                        var model = new Model();
                        model.Nodes.Add(new Node { Name = "model" });
                        modelPack.Models.Add(model);
                        modelPack.Replace(Options.Input, Options.TmxScale, Options.Model.EnableMaterialOverlays,
                            Options.Model.WeightedMeshType, Options.Model.UnweightedMeshType, Options.Model.MeshWeightLimit, Options.Model.BatchVertexLimit);

                        var lb = new LBFileSystem();
                        lb.Load(Options.Field.LbReplaceInput);

                        var f1Handle = lb.GetHandle("F1");

                        FieldScene f1;
                        using (var stream = lb.OpenFile(f1Handle))
                            f1 = new FieldScene(stream, true);

                        f1.Objects.RemoveAll(x => x.ResourceType == FieldObjectResourceType.Model);
                        f1.Objects.Clear();
                        f1.Objects.Add(new FieldObject() { Id = 0, Name = "model", Transform = new FieldObjectTransform(), Resource = modelPack.Models[0] });

                        lb.AddFile(f1Handle, f1.Save(), true, ConflictPolicy.Replace);

                        if (modelPack.TexturePack != null)
                        {
                            var tbHandle = lb.GetHandle("TBN");
                            lb.AddFile(tbHandle, modelPack.TexturePack.Save(), true, ConflictPolicy.Replace);
                        }

                        lb.Save(Options.Output);
                    }
                    break;
                default:
                    throw new Exception("Unsupported output format");
            }
        }

        static bool ParseArgs(string[] args)
        {
            try
            {
                Options = SimpleCommandLineParser.Default.Parse<ProgramOptions>(args);

                //-- Validate given input

                if (string.IsNullOrEmpty(Options.Input))
                {
                    // Use first argument as input when not specified explicitly
                    if (args.Length > 0)
                        Options.Input = args[0];
                    else
                        return false;
                }

                if (Options.InputFormat == InputFormat.Unknown)
                {
                    // Guess input format based on extension
                    var ext = Path.GetExtension(Options.Input);
                    Options.InputFormat = (InputFormat)Enum.Parse(typeof(InputFormat), ext
                        .TrimStart('.')
                        .ToLower(), true);
                }

                if (string.IsNullOrEmpty(Options.Output))
                {
                    // Guess output format based on input format
                    var ext = string.Empty;
                    switch (Options.InputFormat)
                    {
                        case InputFormat.PB:
                        case InputFormat.MB:
                        case InputFormat.F1:
                            ext = ".fbx";
                            Options.OutputFormat = OutputFormat.FBX;
                            break;
                        case InputFormat.TB:
                            ext = null;
                            Options.OutputFormat = OutputFormat.Folder;
                            break;
                        case InputFormat.OBJ:
                            ext = ".f1";
                            Options.OutputFormat = OutputFormat.F1;
                            break;
                        case InputFormat.DAE:
                        case InputFormat.FBX:
                            ext = ".pb";
                            Options.OutputFormat = OutputFormat.FBX;
                            break;
                        default:
                            return false;
                    }

                    var dirPath = Path.Combine(Path.GetDirectoryName(Options.Input), Path.GetFileNameWithoutExtension(Options.Input));
                    if (ext != null)
                        Options.Output = Path.Combine(dirPath, Path.GetFileNameWithoutExtension(Options.Input) + ext);
                    else
                        Options.Output = dirPath;

                    Directory.CreateDirectory(dirPath);
                }

                if (Options.OutputFormat == OutputFormat.Unknown)
                {
                    var ext = Path.GetExtension(Options.Output);
                    if (string.IsNullOrEmpty(ext))
                        Options.OutputFormat = OutputFormat.Folder;
                    else
                        Options.OutputFormat = (OutputFormat)Enum.Parse(typeof(OutputFormat), ext
                            .TrimStart('.')
                            .ToLower(), true);
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }

    public class ProgramOptions
    {
        [Option("i", "input", "filepath", "Specifies the path to the file to use as input.")]
        public string Input { get; set; }

        [Option("if", "input-format", "auto|pb|mb|f1|tb|obj|dae|fbx", "Specifies the input format of the specified input file.")]
        public InputFormat InputFormat { get; set; }

        [Option("o", "output", "filepath", "Specifies the path to the file to save the output to.")]
        public string Output { get; set; }

        [Option("of", "output-format", "auto|pb|mb|f1|obj|dae|fbx", "Specifies the conversion output format.")]
        public OutputFormat OutputFormat { get; set; }

        [Group("ai")]
        public AssimpOptions Assimp { get; set; }

        [Group("pb")]
        public PackedModelOptions PackedModel { get; set; }

        [Option("ts", "tmx-scale", "decimal scale factor", "Specifies the scaling used for texture conversions.", DefaultValue = 1.0f)]
        public float TmxScale { get; set; }

        [Group("mb")]
        public ModelOptions Model { get; set; }

        [Group("f1")]
        public FieldOptions Field { get; set; }

        public class AssimpOptions
        {
            [Option("a", "input-anim", "When specified, the input is treated as an animation file, rather than a model file which affects the conversion process.")]
            public bool TreatInputAsAnimation { get; set; }

            [Option("pbm", "output-pb-motion", "When specified, motions found within the given packed model file are exported when exporting a PB model obj/dae/fbx.")]
            public bool OutputPbMotion { get; set; }

            [Option("legacy-fbx-exp", "use-legacy-fbx-exporter", "When specified, the legacy Assimp FBX exporter is used instead of the FBX SDK.")]
            public bool UseLegacyFbxExporter { get; set; }
        }
        public class PackedModelOptions
        {
            [Option("i", "replace-input", "filepath", "Specifies the base PB file to use for the conversion.")]
            public string ReplaceInput { get; set; }

            [Option("mi", "replace-motion-index", "0-based index", "Specifies the index of the motion in the PB file to replace.")]
            public int ReplaceMotionIndex { get; set; } = -1;

            [Option("mpi", "replace-motion-pack-index", "0-based index", "Specifies the index of the motion pack in the PB file to replace.", DefaultValue = 0)]
            public int ReplaceMotionPackIndex { get; set; }

            [Option("mmi", "replace-motion-model-index", "0-based index", "Specifies the index of the model in the PB file to use when replacing motions.", DefaultValue = 0)]
            public int ReplaceMotionModelIndex { get; set; }
        }

        public class ModelOptions
        {
            [Option("mo", "material-overlays", "When specified, enables the usage of overlay materials.")]
            public bool EnableMaterialOverlays { get; set; }


            [Option("umt", "unweighted-mesh-type", "1|8", "Specifies the mesh type to be used for unweighted meshes.", DefaultValue = 1)]
            public MeshType UnweightedMeshType { get; set; }


            [Option("wmt", "weighted-mesh-type", "2|7", "Specifies the mesh type to be used for weighted meshes.", DefaultValue = 7)]
            public MeshType WeightedMeshType { get; set; }


            [Option("wl", "mesh-weight-limit", "integer", "Specifies the max number of weights to be used per mesh. 3 might give better results.", DefaultValue = 4)]
            public int MeshWeightLimit { get; set; }

            [Option("vl", "batch-vertex-limit", "integer", "Specifies the max number of vertices to be used per batch.", DefaultValue = 24)]
            public int BatchVertexLimit { get; set; }
        }

        public class FieldOptions
        {
            [Option("lbi", "lb-replace-input", "filepath", "Specifies the base field LB file to use for the conversion.")]
            public string LbReplaceInput { get; set; }
        }
    }
}
