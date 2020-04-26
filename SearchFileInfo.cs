using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateCleaner
{
    public class FileEnumerator
    {
        public FileEnumerator()
        {
            //Directory.enum
        }

        //public static IEnumerable<string> EnumerateFiles(DirectorySearchInfo searchFileInfo)
        //{
        //    return SafeFileEnumerator1.EnumerateFiles(searchFileInfo);
        //}
    }
    public class DirectorySearchInfo
    {
        public string DirectoryName { get; set; }

        public IEnumerable<string> ExtensionsToInclude { get; set; }
        public IEnumerable<string> ExtensionsToExclude { get; set; }

        public int MinSize { get; set; }

        public int MaxSize { get; set; }

        public SearchOption SearchOption { get; set; }

        public void AddFilter(IFileMatcher filter)
        {
            fileMatchers.Add(filter);
        }

        public IList<IFileMatcher> FileMatchers => fileMatchers;

        readonly List<IFileMatcher> fileMatchers = new List<IFileMatcher>();
    }

    public class IncludeExtensionsFileMatcher : IFileMatcher
    {
        HashSet<string> extensions = new HashSet<string>();
        public IncludeExtensionsFileMatcher(IEnumerable<string> exts)
        {
            foreach (var item in exts)
            {
                extensions.Add(item);
            }
        }
        public bool Match(string filePath)
        {
            return extensions.Contains(Path.GetExtension(filePath));
        }
    }

    public class MinSizeFileMatcher : IFileMatcher
    {
        long minSize;
        public MinSizeFileMatcher(int minSize)
        {
            this.minSize = minSize;
        }
        public bool Match(string filePath)
        {
            return new FileInfo(filePath).Length >= minSize;
        }
    }

    public interface IFileMatcher
    {
        bool Match(string filePath);
    }
}
