using System;
using System.IO;

namespace DuplicateCleaner
{
    public class SizeHelper
    {
        static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string Suffix(Int64 value)
        {
            if (value < 0) { return "-" + Suffix(-value); }
            if (value == 0) { return "0.0 bytes"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
        }

        public static long GetSizeSafe(string file)
        {
            try
            {
                return new FileInfo(file).Length;
            }
            catch (FileNotFoundException)
            {
                return -1;
            }
            catch (PathTooLongException)
            {
                return -1;
            }
        }

        public static long GetSizeInBytes(string val, string unit)
        {
            return GetSizeInBytes(int.Parse(val), unit);
        }
        public static long GetSizeInBytes(int val, string unit)
        {
            switch (unit)
            {
                case "KB":
                    return val * 1024;
                case "MB":
                    return val * 1024 * 1024;
                case "GB":
                    return val * 1024 * 1024 * 1024;
            }
            return 0;
        }
    }
}
