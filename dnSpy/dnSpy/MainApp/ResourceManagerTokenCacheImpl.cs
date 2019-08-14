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
using dnSpy.Contracts.Resources;

namespace dnSpy.MainApp {
	sealed class ResourceManagerTokenCacheImpl : ResourceManagerTokenCache {
		readonly object lockObj;
		readonly Dictionary<Assembly, int> tokensDict;

		public event EventHandler? TokensUpdated;

		public ResourceManagerTokenCacheImpl() {
			lockObj = new object();
			tokensDict = new Dictionary<Assembly, int>();
		}

		public override bool TryGetResourceManagerGetMethodMetadataToken(Assembly assembly, out int getMethodMetadataToken) {
			lock (lockObj)
				return tokensDict.TryGetValue(assembly, out getMethodMetadataToken) && getMethodMetadataToken != 0;
		}

		public override void SetResourceManagerGetMethodMetadataToken(Assembly assembly, int getMethodMetadataToken) {
			lock (lockObj)
				tokensDict[assembly] = getMethodMetadataToken;
			TokensUpdated?.Invoke(this, EventArgs.Empty);
		}

		public void SetTokens(Assembly[] assemblies, int[] tokens) {
			Debug2.Assert(!(assemblies is null));
			Debug2.Assert(!(tokens is null));
			Debug.Assert(assemblies.Length == tokens.Length);
			lock (lockObj) {
				for (int i = 0; i < assemblies.Length; i++) {
					Debug.Assert(!tokensDict.ContainsKey(assemblies[i]));
					tokensDict[assemblies[i]] = tokens[i];
				}
			}
		}

		public int[] GetTokens(Assembly[] assemblies) {
			var tokens = new int[assemblies.Length];
			lock (lockObj) {
				for (int i = 0; i < assemblies.Length; i++) {
					if (tokensDict.TryGetValue(assemblies[i], out int token))
						tokens[i] = token;
				}
			}
			return tokens;
		}
	}
}
