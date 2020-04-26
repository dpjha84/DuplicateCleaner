using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateCleaner
{
    public class SafeFileEnumerator1
    {
        public static IEnumerable<string> EnumerateDirectories(string parentDirectory, string searchPattern, SearchOption searchOpt)
        {
            try
            {
                var directories = Enumerable.Empty<string>();
                if (searchOpt == SearchOption.AllDirectories)
                {
                    directories = Directory.EnumerateDirectories(parentDirectory)
                        .SelectMany(x => EnumerateDirectories(x, searchPattern, searchOpt));
                }
                return directories.Concat(Directory.EnumerateDirectories(parentDirectory, searchPattern));
            }
            catch (UnauthorizedAccessException)
            {
                return Enumerable.Empty<string>();
            }
            catch (PathTooLongException)
            {
                return Enumerable.Empty<string>();
            }
        }

        DirectorySearchInfo searchFileInfo;

        public static SafeFileEnumerator1 Create(DirectorySearchInfo searchFileInfo)
        {
            var obj = new SafeFileEnumerator1();
            obj.searchFileInfo = searchFileInfo;
            return obj;
        }

        IEnumerable<string> Test(string x)
        {
            searchFileInfo.DirectoryName = x;
            return EnumerateFiles();
        }

        public IEnumerable<string> EnumerateFiles()
        {
            //return Directory.EnumerateFiles(path, "*.*", searchOpt);
            try
            {
                var path = searchFileInfo.DirectoryName;
                var searchOpt = searchFileInfo.SearchOption;
                var dirFiles = Enumerable.Empty<string>();
                if (searchOpt == SearchOption.AllDirectories)
                {
                    dirFiles = EnumerateDirectories(path, "*.*", searchOpt)
                                        .SelectMany(x => Test(x));
                }
                //var files = Directory.EnumerateFiles(path, "*.*").(x => searchFileInfo.FileMatchers.)
                foreach (var file in Directory.EnumerateFiles(path, "*.*"))
                {
                    bool match = true;
                    foreach (var filter in searchFileInfo.FileMatchers)
                    {
                        if (!filter.Match(file)) { match = false; break; }                         
                    }
                    if(match)
                        dirFiles = dirFiles.Concat(new[] { file });
                }
                return dirFiles;
            }
            catch (UnauthorizedAccessException)
            {
                return Enumerable.Empty<string>();
            }
            catch (PathTooLongException)
            {
                return Enumerable.Empty<string>();
            }
        }
        static bool MatchingExtension(string file, HashSet<string> extList)
        {
            return extList.Count == 0 ? true : extList.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase));
        }
    }
}