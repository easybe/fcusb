using System;


namespace FlashcatUSB
{
    static class ArrayExtensions
    {
        public static byte[] Slice<T>(this T[] data, int start_index, int length)
        {
            var d = new byte[length];
            Array.Copy(data, start_index, d, 0, length);
            return d;
        }

        public static byte[] Reverse<T>(this T[] data)
        {
            var d = new byte[data.Length];
            Array.Copy(data, 0, d, 0, data.Length);
            Array.Reverse(d);
            return d;
        }
    }
}