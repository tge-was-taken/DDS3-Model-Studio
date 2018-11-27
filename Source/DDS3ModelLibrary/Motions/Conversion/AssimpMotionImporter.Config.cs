using System;

namespace DDS3ModelLibrary.Motions.Conversion
{
    public partial class AssimpMotionImporter
    {
        public class Config
        {
            public Func<string, int> NodeIndexResolver { get; set; }

            public Config()
            {
                // Default config
            }
        }
    }
}