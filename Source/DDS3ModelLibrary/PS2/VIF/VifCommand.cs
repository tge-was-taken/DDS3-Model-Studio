namespace DDS3ModelLibrary.PS2.VIF
{
    public enum VifCommand : byte
    {
        NoOperation = 0x00,
        SetCycle = 0x01,
        SetOffset = 0x02,
        SetBase = 0x03,
        SetItops = 0x04,
        SetMode = 0x05,
        MaskPath = 0x06,
        SetMark = 0x07,
        FlushEnd = 0x10,
        Flush = 0x11,
        FlushAll = 0x13,
        ActMicro = 0x14, // MSCAL
        CntMicro = 0x17, // MSCNT
        ActMicroF = 0x15,
        SetMask = 0x20,
        SetRow = 0x30,
        SetCol = 0x31,
        LoadMicro = 0x4A,
        Direct = 0x50,
        DirectHl = 0x51,
        Unpack = 0x60,
        UnpackMask = 0x70
    }
}