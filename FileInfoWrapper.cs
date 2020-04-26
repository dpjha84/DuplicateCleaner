using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DuplicateCleaner
{
    public class DeletedFile
    {
        public string Name { get; set; }

        public string Size { get; set; }

        public string ActionTaken { get; set; }
    }
    public class FileInfoWrapper
    {
        public bool IsAlternate => Group%2 == 0;
        public int Group { get; set; }
        readonly FileInfo fileInfo;
        readonly string hash;

        public FileInfoWrapper(string filePath)
        {
            fileInfo = new FileInfo(filePath);
        }

        public FileInfoWrapper(string filePath, string hash)
        {
            fileInfo = new FileInfo(filePath);
            this.hash = hash;
        }

        public bool Deleted { get; set; }

        public string Hash => hash;

        public string Name => fileInfo.Name;

        public string FullName => fileInfo.FullName;

        public string DirectoryName => fileInfo.DirectoryName;

        public long Length => fileInfo.Length;

        public string Size => $"{Math.Ceiling((double)Length / 1024)} KB";

        public DateTime DateModified => fileInfo.LastWriteTime;

        public BitmapSource Icon
        {
            get
            {
                using (var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(fileInfo.FullName))
                {
                    var bmpSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            sysicon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                    return bmpSrc;
                }
            }
        }
    }
}