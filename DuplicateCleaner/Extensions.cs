using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DuplicateCleaner
{
    public static class Extensions
    {
        public static string ToHumanTimeString(this TimeSpan span, int significantDigits = 3)
        {
            var format = "G" + significantDigits;
            return span.TotalMilliseconds < 1000 ? span.TotalMilliseconds.ToString(format) + "ms"
                : (span.TotalSeconds < 60 ? span.TotalSeconds.ToString(format) + "s"
                    : (span.TotalMinutes < 60 ? span.TotalMinutes.ToString(format) + "m"
                        : (span.TotalHours < 24 ? span.TotalHours.ToString(format) + "h"
                                                : span.TotalDays.ToString(format) + "d")));
        }

        public class LocationOptimizationResult
        {
            public IEnumerable<Location> UniqueFolders { get; set; }

            public HashSet<string> ExcludedInTreeList { get; set; }
        }

        public static LocationOptimizationResult GetOptimizedFolders(IEnumerable<Location> paths)
        {
            var result = new LocationOptimizationResult
            {
                ExcludedInTreeList = new HashSet<string>()
            };
            var map = new HashSet<Location>();
            paths = paths.OrderBy(x => x.Name);
            foreach (var path in paths)
            {
                if (path.Include) 
                {
                    if (TryMatch(map, path.Name, out Location match))
                    {
                        if (match.IncludeSubfolders.HasValue && !match.IncludeSubfolders.Value)
                            map.Add(path);
                    }
                    else
                        map.Add(path);
                }
                else
                {
                    result.ExcludedInTreeList.Add(path.Name);
                }
            }
            result.UniqueFolders = map.ToList();
            return result;
        }

        static bool TryMatch(HashSet<Location> map, string path, out Location match)
        {
            match = null;
            foreach (var item in map)
            {
                if (path.IndexOf(item.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    match = item;
                    return true;
                }
            }
            return false;
        }
    }
}
