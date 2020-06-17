using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateCleaner
{
    public class HashHelper
    {
        static Dictionary<string, Tuple<DateTime, string>> cache;

        static HashHelper()
        {
            var cacheData = ReadCache().Result;
            if (!string.IsNullOrWhiteSpace(cacheData))
                cache = JsonConvert.DeserializeObject<Dictionary<string, Tuple<DateTime, string>>>(cacheData);
            else
                cache = new Dictionary<string, Tuple<DateTime, string>>();
        }

        public static async Task CacheHash()
        {
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile sampleFile = await storageFolder.CreateFileAsync("hashcache.json",
                    Windows.Storage.CreationCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(false);

            var data = JsonConvert.SerializeObject(cache);
            await Windows.Storage.FileIO.WriteTextAsync(sampleFile, data).AsTask().ConfigureAwait(false);
        }

        static async Task<string> ReadCache()
        {
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile sampleFile = await storageFolder.CreateFileAsync("hashcache.json",
                    Windows.Storage.CreationCollisionOption.OpenIfExists).AsTask().ConfigureAwait(false); ;

            var data = await Windows.Storage.FileIO.ReadTextAsync(sampleFile);
            return data;
        }

        public static string GetFileHash(string fileName)
        {
            var lastModifiedTime = new FileInfoWrapper(fileName).DateModified;
            if (cache.ContainsKey(fileName))
            {
                if (lastModifiedTime > cache[fileName].Item1)
                    cache.Remove(fileName);
                else
                    return cache[fileName].Item2;
            }
            try
            {
                using (var md5 = MD5.Create())
                {
                    const int kb = 1024;
                    const int mb = kb * kb;
                    var length = new FileInfo(fileName).Length;
                    if (length > 100 * mb)
                    {
                        int i = 1;
                        const int bytesPerChunk = 4 * mb;
                        const int chunkCount = 10;
                        long chunkStartPosition = length / (chunkCount * mb);
                        var sb = new StringBuilder();

                        using (BinaryReader fileData = new BinaryReader(File.OpenRead(fileName)))
                        {
                            while (i <= chunkCount)
                            {
                                fileData.BaseStream.Position = chunkStartPosition * i * mb;
                                var bytes = fileData.ReadBytes(bytesPerChunk);
                                sb.Append(Encoding.Default.GetString(md5.ComputeHash(bytes)));
                                i++;
                            }
                        }
                        sb.Append(length);
                        var data = sb.ToString();
                        cache.Add(fileName, Tuple.Create(lastModifiedTime, data));
                        return data;
                    }
                    using (var stream = File.OpenRead(fileName))
                    {
                        var data = Encoding.Default.GetString(md5.ComputeHash(stream));
                        cache.Add(fileName, Tuple.Create(lastModifiedTime, data));
                        return data;
                    }
                }
            }
            catch (Exception)
            {
                return null;
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
