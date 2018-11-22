using System.Drawing;

namespace DDS3ModelLibrary.Textures.Processing.WuQuantizer
{
    public interface IWuQuantizer
    {
        Image QuantizeImage(Bitmap image, int alphaThreshold, int alphaFader);
    }
}