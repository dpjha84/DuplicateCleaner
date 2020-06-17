using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace DuplicateCleaner
{
    public struct FileSearchFilter
    {
        public SearchOption searchOpt { get; set; }
        
        public HashSet<string> extList { get; set; }

        public HashSet<string> exc { get; set; }

        public long minSize { get; set; }

        public long maxSize { get; set; }

        public DateTime? modifyAfter { get; set; }

        public DateTime? modifyBefore { get; set; }

        public bool includeHiddenFolders { get; set; }
    }
    public class FileHelper
    {
        public static async Task Write(string fileName, string data)
        {
            var file = await GetFile(fileName, CreationCollisionOption.ReplaceExisting).ConfigureAwait(false);
            await FileIO.WriteTextAsync(file, data).AsTask().ConfigureAwait(false);
        }

        public static async Task<string> Read(string fileName)
        {
            var file = await GetFile(fileName, CreationCollisionOption.OpenIfExists).ConfigureAwait(false);
            return await FileIO.ReadTextAsync(file);
        }

        private static async Task<StorageFile> GetFile(string fileName, CreationCollisionOption option)
        {
            return await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, option).AsTask().ConfigureAwait(false);
        }
    }
}
