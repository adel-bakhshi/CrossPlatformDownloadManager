namespace MultipartDownloader.Core;

public class Packet
{
    #region Properties

    public Memory<byte> Data { get; private set; }
    public int Offset { get; private set; }
    public int Length { get; private set; }

    #endregion Properties

    public Packet(byte[] data, int offset, int length)
    {
        Offset = offset;
        Length = length;
        Data = data.AsMemory(Offset, Length);
    }

    public void Clear()
    {
        Data = null;
        Offset = Length = 0;
    }
}