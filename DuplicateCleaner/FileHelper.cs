using Microsoft.VisualBasic.FileIO;
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
    public class FileHelper
    {
        public static HashSet<string> Pics { get; } = new HashSet<string>(new[] { ".jpg", ".png", ".jpeg", ".bmp", ".gif", ".tif", ".pcx", ".ico", ".tga" }, StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> Audio { get; } = new HashSet<string>(new[] { ".mp3", ".ogg", ".wma", ".wav", ".ape", ".flac", ".m4p", ".m4a", ".aac" }, StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> Video { get; } = new HashSet<string>(new[] { ".mov", ".avi", ".mp4", ".mpg", ".wmv", ".flv", ".3pg", ".asf" }, StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> Docs { get; } = new HashSet<string>(new[] { ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".accdb", ".pdf", ".txt", ".odt", ".ods" }, StringComparer.OrdinalIgnoreCase);

        public static FileType GetFileType(string fullName)
        {
            var extn = Path.GetExtension(fullName);
            if (Pics.Contains(extn)) return FileType.Image;
            if (Audio.Contains(extn)) return FileType.Audio;
            if (Video.Contains(extn)) return FileType.Video;
            if (Docs.Contains(extn)) return FileType.Document;
            return FileType.Custom;
        }

        public static async Task Write(string fileName, string data)
        {
            var file = await GetFile(fileName, CreationCollisionOption.ReplaceExisting).ConfigureAwait(false);
            await FileIO.WriteTextAsync(file, data).AsTask().ConfigureAwait(false);
        }

        public static async Task DeleteFileAsync(string item, DeleteOption option)
        {
            await Task.Run(() => FileSystem.DeleteFile(item, UIOption.OnlyErrorDialogs, 
                option == DeleteOption.PermanentDelete ? RecycleOption.DeletePermanently : RecycleOption.SendToRecycleBin,
                UICancelOption.DoNothing));
        }

        public static async Task Delete(string fileName, StorageDeleteOption option)
        {
            var file = await GetFile(fileName, CreationCollisionOption.ReplaceExisting).ConfigureAwait(false);
            await file.DeleteAsync(option);
        }

        //public static async Task DeleteFromPath(string filePath)
        //{
        //    var file = await StorageFile.GetFileFromPathAsync(filePath);
        //    await file.DeleteAsync(SearchInfo.Instance.DeleteOption == Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin ?
        //        StorageDeleteOption.Default : StorageDeleteOption.PermanentDelete);
        //}

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
