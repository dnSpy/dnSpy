/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Reflection;
using System.Resources;

namespace dnSpy.Contracts.Resources {
	/// <summary>
	/// Caches the info so we don't have to call <see cref="Module.GetTypes"/> which is very slow
	/// </summary>
	abstract class ResourceManagerTokenCache {
		public abstract bool TryGetResourceManagerGetMethodMetadataToken(Assembly assembly, out int getMethodMetadataToken);
		public abstract void SetResourceManagerGetMethodMetadataToken(Assembly assembly, int getMethodMetadataToken);
	}

	/// <summary>
	/// Converts strings to resource strings
	/// </summary>
	public static class ResourceHelper {
		const string PREFIX = "res:";

		static ResourceManagerTokenCache? resourceManagerTokenCache;

		internal static void SetResourceManagerTokenCache(ResourceManagerTokenCache tokenCache) {
			if (tokenCache is null)
				throw new ArgumentNullException(nameof(tokenCache));
			if (resourceManagerTokenCache is not null)
				throw new InvalidOperationException();
			resourceManagerTokenCache = tokenCache;
		}

		/// <summary>
		/// Converts <paramref name="value"/> to a string in the resources if it has been prefixed with "res:"
		/// </summary>
		/// <param name="obj">Can be any object in the assembly containing the resources or the assembly itself (<see cref="Assembly"/>).</param>
		/// <param name="value">String</param>
		/// <returns></returns>
		public static string? GetStringOrNull(object obj, string? value) {
			Debug2.Assert(resourceManagerTokenCache is not null);
			if (obj is null)
				throw new ArgumentNullException(nameof(obj));
			if (value is null)
				return value;
			return GetString(obj, value);
		}

		/// <summary>
		/// Converts <paramref name="value"/> to a string in the resources if it has been prefixed with "res:"
		/// </summary>
		/// <param name="obj">Can be any object in the assembly containing the resources or the assembly itself (<see cref="Assembly"/>).</param>
		/// <param name="value">String</param>
		/// <returns></returns>
		public static string GetString(object obj, string value) {
			Debug2.Assert(resourceManagerTokenCache is not null);
			if (obj is null)
				throw new ArgumentNullException(nameof(obj));
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			if (!value.StartsWith(PREFIX))
				return value;
			var key = value.Substring(PREFIX.Length);
			var mgr = GetResourceManager(obj as Assembly ?? obj.GetType().Assembly);
			if (mgr is null)
				return "???";
			var s = mgr.GetString(key);
			Debug2.Assert(s is not null);
			return s ?? "???";
		}

		static ResourceManager? GetResourceManager(Assembly assembly) {
			if (asmToMgr.TryGetValue(assembly, out var mgr))
				return mgr;

			var tokenCache = resourceManagerTokenCache;
			Debug2.Assert(tokenCache is not null);
			if (tokenCache is not null) {
				if (tokenCache.TryGetResourceManagerGetMethodMetadataToken(assembly, out int getMethodMetadataToken)) {
					MethodInfo? method;
					try {
						method = assembly.ManifestModule.ResolveMethod(getMethodMetadataToken) as MethodInfo;
					}
					catch (ArgumentException) {
						Debug.Fail("Couldn't resolve resource manager getter method");
						method = null;
					}
					mgr = TrySetResourceManager(assembly, method, save: false);
					if (mgr is not null)
						return mgr;
				}
			}

			foreach (var type in assembly.ManifestModule.GetTypes()) {
				if (type.Namespace is null || !type.Namespace.EndsWith(".Properties", StringComparison.InvariantCultureIgnoreCase))
					continue;
				var prop = type.GetProperty("ResourceManager", BindingFlags.Public | BindingFlags.Static);
				if (prop is null)
					continue;
				var m = prop.GetGetMethod();
				mgr = TrySetResourceManager(assembly, m, save: true);
				if (mgr is not null)
					return mgr;
			}

			Debug.Fail($"Failed to find the class with the ResourceManager property in assembly {assembly}");
			return null;
		}

		static ResourceManager? TrySetResourceManager(Assembly assembly, MethodInfo? m, bool save) {
			if (m is null)
				return null;
			if (!m.IsStatic)
				return null;
			if (m.ReturnType != typeof(ResourceManager))
				return null;
			if (m.GetParameters().Length != 0)
				return null;

			ResourceManager? mgr;
			try {
				mgr = m.Invoke(null, Array.Empty<object>()) as ResourceManager;
			}
			catch {
				return null;
			}
			if (save)
				resourceManagerTokenCache?.SetResourceManagerGetMethodMetadataToken(assembly, m.MetadataToken);
			asmToMgr[assembly] = mgr;
			return mgr;
		}

		static readonly Dictionary<Assembly, ResourceManager?> asmToMgr = new Dictionary<Assembly, ResourceManager?>();
	}
}
