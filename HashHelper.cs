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
        const string cacheFile = "hashcache.json";
        static Dictionary<string, Tuple<DateTime, string>> cache;

        static HashHelper()
        {
            var cacheData = ReadCache();
            if (!string.IsNullOrWhiteSpace(cacheData))
                cache = JsonConvert.DeserializeObject<Dictionary<string, Tuple<DateTime, string>>>(cacheData);
            else
                cache = new Dictionary<string, Tuple<DateTime, string>>();
        }

        public static async Task ResetCacheAsync()
        {
            var deleteTask = FileHelper.Delete(cacheFile, Windows.Storage.StorageDeleteOption.Default);
            cache = new Dictionary<string, Tuple<DateTime, string>>();
            await deleteTask;
        }

        public static async Task CacheHash()
        {
            await FileHelper.Write(cacheFile, JsonConvert.SerializeObject(cache));
        }

        static string ReadCache() => FileHelper.Read(cacheFile).Result;

        public static string GetFileHash(string fileName)
        {
            if (SearchInfo.Instance.DupCriteria == DuplicationMarkingCriteria.FileName) return Path.GetFileName(fileName);

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
    }
}
