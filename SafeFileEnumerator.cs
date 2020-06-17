using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DuplicateCleaner
{
    public static class SafeFileEnumerator
    {
        public static IEnumerable<string> EnumerateFiles(string path, FileSearchFilter filter, CancellationToken token)
        {
            try
            {
                if (token.IsCancellationRequested) return Enumerable.Empty<string>();
                var dir = new DirectoryInfo(path);
                if (!filter.includeHiddenFolders && (dir.Parent != null && dir.Attributes.HasFlag(FileAttributes.Hidden)))
                    return Enumerable.Empty<string>();

                var dirFiles = Enumerable.Empty<string>();
                var dirName = Path.GetFileName(path);
                if (filter.exc.Contains(path)
                    || filter.exc.Where((x) => x.StartsWith(path.ToLowerInvariant() + "\\")).ToList().Count > 0)
                    return dirFiles;
                if (filter.searchOpt == SearchOption.AllDirectories)
                {
                    dirFiles = Directory.EnumerateDirectories(path)
                                        .SelectMany(x => EnumerateFiles(x, filter, token));
                }
                return dirFiles.Concat(Directory.EnumerateFiles(path, "*.*")
                    .Where(file => (MatchingExtension(file, filter.extList)
                        && (filter.minSize == 0 || SizeHelper.GetSizeSafe(file) >= filter.minSize)
                        && (filter.maxSize == 0 || SizeHelper.GetSizeSafe(file) <= filter.maxSize)
                        && (filter.modifyAfter == null || new FileInfo(file).LastWriteTime >= filter.modifyAfter.Value)
                        && (filter.modifyBefore == null || new FileInfo(file).LastWriteTime <= filter.modifyBefore.Value)
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
        
        private static bool MatchingExtension(string file, HashSet<string> extList)
        {
            return extList.Count == 0 ? false : extList.Contains(".*") ? true : extList.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase));
        }
    }
}