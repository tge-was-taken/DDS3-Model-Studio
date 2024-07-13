using DDS3ModelLibrary.Utilities;

namespace DDS3ModelLibrary.Motions.Conversion
{
    public interface IMotionImporter
    {
        Motion Import(string filepath);
    }

    public abstract class MotionImporter<T, TConfig> : Singleton<T>, IMotionImporter where T : class, new()
                                                                    where TConfig : class, new()
    {
        public virtual Motion Import(string filepath) => Import(filepath, new TConfig());

        public abstract Motion Import(string filepath, TConfig config);
    }
}
