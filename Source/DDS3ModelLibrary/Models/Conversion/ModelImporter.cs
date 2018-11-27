using DDS3ModelLibrary.Utilities;

namespace DDS3ModelLibrary.Models.Conversion
{
    public interface IModelImporter
    {
        Model Import( string filepath );
    }

    public abstract class ModelImporter<T, TConfig> : Singleton<T>, IModelImporter where T : class, new()
                                                                                   where TConfig : class, new()
    {
        public virtual Model Import( string filepath ) => Import( filepath, new TConfig() );

        public abstract Model Import( string filepath, TConfig config );
    }
}