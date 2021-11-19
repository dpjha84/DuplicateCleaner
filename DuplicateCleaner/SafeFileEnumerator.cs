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
                if ((!filter.IncludeHiddenFolders && (dir.Parent != null && dir.Attributes.HasFlag(FileAttributes.Hidden))))
                    return Enumerable.Empty<string>();

                if (!filter.IncludeSystemFolders && 
                        ((dir.FullName == Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.System)).FullName) ||
                        (dir.FullName == Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) ||
                        (dir.FullName == Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)))))
                    return Enumerable.Empty<string>();

                var dirFiles = Enumerable.Empty<string>();
                var dirName = Path.GetFileName(path);
                if (filter.ExcludedList.Contains(path)
                    || filter.ExcludedList.Where((x) => x.StartsWith(path.ToLowerInvariant() + "\\")).ToList().Count > 0)
                    return dirFiles;
                if (filter.SearchOption == SearchOption.AllDirectories)
                {
                    dirFiles = Directory.EnumerateDirectories(path)
                                        .SelectMany(x => EnumerateFiles(x, filter, token));
                }
                return dirFiles.Concat(Directory.EnumerateFiles(path, "*.*")
                    .Where(file => (MatchingExtension(file, filter.ExtensionList)
                        && (filter.MinSize == -1 || SizeHelper.GetSizeSafe(file) >= filter.MinSize)
                        && (filter.MaxSize == -1 || SizeHelper.GetSizeSafe(file) <= filter.MaxSize)
                        && (filter.ModifyAfter == null || new FileInfo(file).LastWriteTime >= filter.ModifyAfter.Value)
                        && (filter.ModifyBefore == null || new FileInfo(file).LastWriteTime <= filter.ModifyBefore.Value)
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
            return extList.Count != 0 && (extList.Contains(".*") || extList.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase)));
        }
    }
}