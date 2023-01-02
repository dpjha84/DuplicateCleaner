using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateCleaner
{
    public class HashHelper
    {
        const string cacheFile = "hashcache.json";
        static ConcurrentDictionary<string, Tuple<DateTime, string>> cache;

        static HashHelper()
        {
            var cacheData = ReadCacheAsync().Result;
            if (!string.IsNullOrWhiteSpace(cacheData))
                cache = JsonConvert.DeserializeObject<ConcurrentDictionary<string, Tuple<DateTime, string>>>(cacheData);
            else
                cache = new ConcurrentDictionary<string, Tuple<DateTime, string>>();
        }

        public static void ResetCache()
        {
            File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cacheFile));
            //var deleteTask = File.Delete(cacheFile);
            //cache = new Dictionary<string, Tuple<DateTime, string>>();
            //await deleteTask;
        }

        public static async Task CacheHashAsync()
        {
            await File.WriteAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cacheFile), JsonConvert.SerializeObject(cache));
        }

        static async Task<string> ReadCacheAsync()
        {
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cacheFile))) return "";
            return  File.ReadAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cacheFile)).Result;
        }

        public static string GetFileHash(string fileName)
        {
            if (SearchInfo.Instance.DupCriteria == DuplicationMarkingCriteria.FileName) return Path.GetFileName(fileName);

            var lastModifiedTime = new FileInfoWrapper(fileName).DateModified;
            if (cache.ContainsKey(fileName))
            {
                if (lastModifiedTime > cache[fileName].Item1)
                    cache.TryRemove(fileName, out _);
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
                        cache[fileName] = Tuple.Create(lastModifiedTime, data);
                        return data;
                    }
                    using (var stream = File.OpenRead(fileName))
                    {
                        var data = Encoding.Default.GetString(md5.ComputeHash(stream));
                        cache[fileName] = Tuple.Create(lastModifiedTime, data);
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
