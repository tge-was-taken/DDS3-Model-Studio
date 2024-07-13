using DDS3ModelLibrary.Models;
using DDS3ModelLibrary.Utilities;

namespace DDS3ModelLibrary.Motions.Conversion
{
    public interface IMotionExporter
    {
        void Export(Model model, Motion motion, string filepath);
    }

    public abstract class MotionExporter<T, TConfig> : Singleton<T>, IMotionExporter where T : class, new()
                                                                    where TConfig : class, new()
    {
        public virtual void Export(Model model, Motion motion, string filepath) => Export(model, motion, filepath, new TConfig());

        public abstract void Export(Model model, Motion motion, string filepath, TConfig config);
    }
}