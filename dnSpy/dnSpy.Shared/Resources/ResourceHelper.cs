/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace dnSpy.Shared.Resources {
	public static class ResourceHelper {
		const string PREFIX = "res:";

		public static string GetString(object obj, string value) {
			if (value == null || !value.StartsWith(PREFIX))
				return value;
			var key = value.Substring(PREFIX.Length);
			var mgr = GetResourceManager(obj.GetType().Assembly);
			if (mgr == null)
				return "???";
			var s = mgr.GetString(key);
			Debug.Assert(s != null);
			return s ?? "???";
		}

		static ResourceManager GetResourceManager(Assembly assembly) {
			ResourceManager mgr;
			if (asmToMgr.TryGetValue(assembly, out mgr))
				return mgr;

			foreach (var type in assembly.ManifestModule.GetTypes().Where(a => a.Namespace != null && a.Namespace.Contains("Properties"))) {
				var prop = type.GetProperty("ResourceManager", BindingFlags.Public | BindingFlags.Static);
				if (prop == null)
					continue;
				var m = prop.GetGetMethod();
				if (m == null)
					continue;
				if (m.ReturnType != typeof(ResourceManager))
					continue;
				if (m.GetParameters().Length != 0)
					continue;

				try {
					mgr = m.Invoke(null, Array.Empty<object>()) as ResourceManager;
				}
				catch {
					mgr = null;
				}
				if (mgr != null) {
					asmToMgr[assembly] = mgr;
					return mgr;
				}
			}

			Debug.Fail(string.Format("Failed to find the class with the ResourceManager property in assembly {0}", assembly));
			return null;
		}

		static readonly Dictionary<Assembly, ResourceManager> asmToMgr = new Dictionary<Assembly, ResourceManager>();
	}
}
