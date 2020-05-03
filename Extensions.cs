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
        public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        public static string ToHumanTimeString(this TimeSpan span, int significantDigits = 3)
        {
            var format = "G" + significantDigits;
            return span.TotalMilliseconds < 1000 ? span.TotalMilliseconds.ToString(format) + "ms"
                : (span.TotalSeconds < 60 ? span.TotalSeconds.ToString(format) + "s"
                    : (span.TotalMinutes < 60 ? span.TotalMinutes.ToString(format) + "m"
                        : (span.TotalHours < 24 ? span.TotalHours.ToString(format) + "h"
                                                : span.TotalDays.ToString(format) + "d")));
        }

        public static bool IsSubfolderOf(this DirectoryInfo probableChild, DirectoryInfo probableParent)
        {
            while (probableParent.Parent != null)
            {
                if (probableParent.Parent.FullName == probableChild.FullName)
                    return true;
                probableParent = probableParent.Parent;
            }
            return false;
        }
        public class LocationOptimizationResult
        {
            public IEnumerable<Location> UniqueFolders { get; set; }

            public HashSet<string> ExcludedInTreeList { get; set; }
        }
        public static LocationOptimizationResult GetOptimizedFolders(IEnumerable<Location> paths)
        {
            var result = new LocationOptimizationResult();
            result.ExcludedInTreeList = new HashSet<string>();
            var map = new HashSet<Location>();
            paths = paths.OrderBy(x => x.Name);
            foreach (var path in paths)
            {
                if (!map.Contains(path, new MyEqualityComparer()))
                {
                    map.Add(path);
                }
                else if (path.Exclude)
                {
                    result.ExcludedInTreeList.Add(path.Name);
                }
            }
            result.UniqueFolders = map.ToList();
            return result;
        }
        public class MyEqualityComparer : IEqualityComparer<Location>
        {
            public bool Equals(Location x, Location y)
            {
                return !x.Exclude && y.Name.IndexOf(x.Name, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            public int GetHashCode(Location obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
