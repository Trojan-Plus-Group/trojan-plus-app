/*
 * This file is part of the Trojan Plus project.
 * Trojan is an unidentifiable mechanism that helps you bypass GFW.
 * Trojan Plus is derived from original trojan project and writing
 * for more experimental features.
 * Copyright (C) 2020 The Trojan Plus Group Authors.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

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
