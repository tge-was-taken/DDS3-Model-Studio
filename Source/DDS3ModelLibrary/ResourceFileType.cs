namespace DDS3ModelLibrary
{
    public enum ResourceFileType : byte
    {
        Default      = 1,
        Texture      = 2,
        Model        = 6,
        MotionPack   = 8,
        TexturePack  = 9,
        ModelPackEnd = 0xFF,
    }
}