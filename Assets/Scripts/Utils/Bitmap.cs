public static class Bitmap
{
    public static long SetTrue(long acc, int index)
    {
        return acc | (0x1L << (index - 1));
    }

    public static long SetFalse(long acc, int index)
    {
        return acc & ~(0x1L << (index - 1));
    }

    public static bool GetBool(long acc, int index)
    {
        return (acc & (0x1L << (index - 1))) != 0;
    }
}
