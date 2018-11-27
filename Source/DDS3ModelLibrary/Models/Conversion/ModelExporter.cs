using DDS3ModelLibrary.Textures;
using DDS3ModelLibrary.Utilities;

namespace DDS3ModelLibrary.Models.Conversion
{
    public interface IModelExporter
    {
        void Export( Model model, string filepath, TexturePack textures = null );
    }

    public abstract class ModelExporter<T, TConfig> : Singleton<T>, IModelExporter where T : class, new()
                                                                                     where TConfig : class, new()
    {
        public virtual void Export( Model model, string filepath, TexturePack textures = null ) => Export( model, filepath, new TConfig(), textures );

        public abstract void Export( Model model, string filepath, TConfig config, TexturePack textures = null );
    }
}
