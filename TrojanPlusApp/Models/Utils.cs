using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System;
using System.Linq;
using TrojanPlusApp.ViewModels;
using Xamarin.Forms;

namespace TrojanPlusApp.Models
{
    public static class Utils
    {
        public static int RemoveAll<T>(this ObservableCollection<T> coll, Func<T, bool> condition)
        {
            var itemsToRemove = coll.Where(condition).ToList();

            foreach (var itemToRemove in itemsToRemove)
            {
                coll.Remove(itemToRemove);
            }

            return itemsToRemove.Count;
        }

        public static string ToLowerString(this bool b)
        {
            return b.ToString().ToLower();
        }

        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType
                   && (type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        public static T FindOrNull<T>(this IList<T> list, Predicate<T> pre)
            where T : class
        {
            foreach (T i in list)
            {
                if (pre(i))
                {
                    return i;
                }
            }

            return null;
        }
    }
}
