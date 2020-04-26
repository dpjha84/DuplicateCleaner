using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateCleaner
{
    public static class SafeFileEnumerator
    {
        public static IEnumerable<string> EnumerateDirectories(string parentDirectory, string searchPattern, SearchOption searchOpt,
            List<string> exclusions)
        {
            try
            {
                var directories = Enumerable.Empty<string>();
                if (exclusions.Contains(parentDirectory))
                    return directories;
                if (searchOpt == SearchOption.AllDirectories)
                {
                    directories = Directory.EnumerateDirectories(parentDirectory)
                        .SelectMany(x => EnumerateDirectories(x, searchPattern, searchOpt, exclusions));
                }
                return directories.Concat(Directory.EnumerateDirectories(parentDirectory, searchPattern));
            }
            catch (UnauthorizedAccessException)
            {
                return Enumerable.Empty<string>();
            }
        }

        public static IEnumerable<string> EnumerateFiles(string path, SearchOption searchOpt, HashSet<string> extList, 
            HashSet<string> exc, long minSize, long maxSize, DateTime? modifyAfter, DateTime? modifyBefore, bool includeHiddenFolders)
        {
            try
            {
                var dir = new DirectoryInfo(path);
                if (!includeHiddenFolders && (dir.Parent != null && dir.Attributes.HasFlag(FileAttributes.Hidden)))
                    return Enumerable.Empty<string>();

                var dirFiles = Enumerable.Empty<string>();
                var dirName = Path.GetFileName(path);
                if (exc.Contains(path)
                    || exc.Where((x) => x.StartsWith(path.ToLowerInvariant() + "\\")).ToList().Count > 0
                    //|| (!string.IsNullOrWhiteSpace(dirName) && dirName.StartsWith("$"))
                    )
                    return dirFiles;
                if (searchOpt == SearchOption.AllDirectories)
                {
                    dirFiles = Directory.EnumerateDirectories(path)
                                        .SelectMany(x => EnumerateFiles(x, searchOpt, extList, exc, minSize, maxSize, modifyAfter, modifyBefore, includeHiddenFolders));
                }
                return dirFiles.Concat(Directory.EnumerateFiles(path, "*.*")
                    .Where(file => (MatchingExtension(file, extList)
                        && (minSize == 0 || SizeHelper.GetSizeSafe(file) >= minSize)
                        && (maxSize == 0 || SizeHelper.GetSizeSafe(file) <= maxSize)
                        && (modifyAfter == null || new FileInfo(file).LastWriteTime >= modifyAfter.Value)
                        && (modifyBefore == null || new FileInfo(file).LastWriteTime <= modifyBefore.Value)
                        )));
            }
            catch (UnauthorizedAccessException)
            {
                return Enumerable.Empty<string>();
            }
            catch (PathTooLongException)
            {
                return Enumerable.Empty<string>();
            }
            catch (FileNotFoundException)
            {
                return Enumerable.Empty<string>();
            }
            catch (IOException)
            {
                return Enumerable.Empty<string>();
            }
        }
        static bool MatchingExtension(string file, HashSet<string> extList)
        {
            return extList.Count == 0 ? false : extList.Contains(".*") ? true : extList.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase));
        }
    }
}