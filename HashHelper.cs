using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateCleaner
{
    public class HashHelper
    {
        public static string GetFileHash(string fileName)
        {
            using (var md5 = MD5.Create())
            {
                try
                {
                    using (var stream = new BufferedStream(File.OpenRead(fileName), 2400000))
                    {
                        return Encoding.Default.GetString(md5.ComputeHash(stream));
                    }
                    //using (var stream = File.OpenRead(fileName))
                    //{
                    //    return Encoding.Default.GetString(md5.ComputeHash(stream));
                    //}
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public static async Task<string> GetFileHashAsync(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)) // true means use IO async operations
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    do
                    {
                        bytesRead = await stream.ReadAsync(buffer, 0, 4096);
                        if (bytesRead > 0)
                        {
                            md5.TransformBlock(buffer, 0, bytesRead, null, 0);
                        }
                    } while (bytesRead > 0);

                    md5.TransformFinalBlock(buffer, 0, 0);
                    return BitConverter.ToString(md5.Hash).Replace("-", "").ToUpperInvariant();
                }
            }
        }
    }
}
