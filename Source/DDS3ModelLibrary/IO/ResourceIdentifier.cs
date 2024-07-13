namespace DDS3ModelLibrary.IO
{
    public enum ResourceIdentifier : uint
    {
        ModelPackInfo = 0x30424950,
        TexturePack = 0x30505854,
        Texture = 0x30584D54,
        Model = 0x3030444D,
        MotionPack = 0x3030544D,
        ModelPackEnd = 0x30444E45,
        Particle = 0x00503344,
        Video = 0x00555049,
        FieldScene = 0x31444C46,
        FieldResource2 = 0x32444C46,
    }
}