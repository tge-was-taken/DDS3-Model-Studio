using System;

namespace DDS3ModelLibrary.Textures.Processing.WuQuantizer
{
    [Serializable]
    public class QuantizationException : ApplicationException
    {
        public QuantizationException(string message) : base(message)
        {

        }
    }
}
