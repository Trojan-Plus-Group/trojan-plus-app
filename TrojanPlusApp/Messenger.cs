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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrojanPlusApp
{
    public static class Messenger
    {
        private static readonly Dictionary<string, List<Delegate>> Subscribers = new Dictionary<string, List<Delegate>>();

        public static void Send<T>(string message, T args)
        {
            if (Subscribers.TryGetValue(message, out var list))
            {
                foreach (var subscriber in list)
                {
                    if (subscriber is Action<T> action)
                    {
                        action(args);
                    }
                    else if (subscriber is Func<object, T, Task> func && args != null)
                    {
                        Task.Run(() => func(null, args));
                    }
                }
            }
        }

        public static void Subscribe<T>(string message, Action<T> callback)
        {
            if (!Subscribers.ContainsKey(message))
            {
                Subscribers[message] = new List<Delegate>();
            }
            Subscribers[message].Add(callback);
        }

        public static void Subscribe<T>(string message, Func<object, T, Task> callback)
        {
            if (!Subscribers.ContainsKey(message))
            {
                Subscribers[message] = new List<Delegate>();
            }
            Subscribers[message].Add(callback);
        }
    }
}