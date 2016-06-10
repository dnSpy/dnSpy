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
using dnSpy.Contracts.Text;

namespace dnSpy.Text.Editor {
	sealed class ProviderSelector<TProvider> {
		readonly Dictionary<IContentType, List<TProvider>> dict;

		public ProviderSelector(IEnumerable<TProvider> providers, Func<TProvider, IEnumerable<IContentType>> getContentTypes) {
			if (getContentTypes == null)
				throw new ArgumentNullException(nameof(getContentTypes));
			this.dict = CreateDictionary(providers, getContentTypes);
		}

		public IEnumerable<TProvider> GetProviders(IContentType contentType) {
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));

			var ctDict = GetContentTypes(contentType);
			foreach (var c in ctDict.OrderBy(a => a.Value)) {
				List<TProvider> list;
				if (dict.TryGetValue(c.Key, out list))
					return list;
			}
			return Array.Empty<TProvider>();
		}

		static Dictionary<IContentType, int> GetContentTypes(IContentType contentType) {
			var dict = new Dictionary<IContentType, int>();
			GetContentTypes(dict, contentType, 0);
			return dict;
		}

		static void GetContentTypes(Dictionary<IContentType, int> dict, IContentType contentType, int depth) {
			if (dict.ContainsKey(contentType))
				return;
			dict.Add(contentType, depth);
			foreach (var c in contentType.BaseTypes)
				GetContentTypes(dict, c, depth + 1);
		}

		static Dictionary<IContentType, List<TProvider>> CreateDictionary(IEnumerable<TProvider> providers, Func<TProvider, IEnumerable<IContentType>> getContentTypes) {
			var dict = new Dictionary<IContentType, List<TProvider>>();
			var hash = new HashSet<IContentType>();
			var stack = new Stack<IContentType>();
			foreach (var p in providers) {
				foreach (var c in getContentTypes(p)) {
					Debug.Assert(c != null);
					if (c == null)
						break;
					Add(dict, p, c, true);
					foreach (var bc in GetAllBaseTypes(c, hash, stack)) {
						Debug.Assert(bc != c);
						if (bc == c)
							continue;
						Add(dict, p, bc, false);
					}
				}
			}
			return dict;
		}

		static IEnumerable<IContentType> GetAllBaseTypes(IContentType contentType, HashSet<IContentType> hash, Stack<IContentType> stack) {
			hash.Clear();
			stack.Clear();
			hash.Add(contentType);
			foreach (var c in contentType.BaseTypes)
				stack.Push(c);
			while (stack.Count > 0) {
				contentType = stack.Pop();
				if (hash.Contains(contentType))
					continue;
				yield return contentType;
				hash.Add(contentType);
				foreach (var c in contentType.BaseTypes)
					stack.Push(c);
			}
		}

		static void Add(Dictionary<IContentType, List<TProvider>> dict, TProvider textStructureNavigatorProvider, IContentType contentType, bool isDef) {
			List<TProvider> list;
			if (!dict.TryGetValue(contentType, out list))
				dict.Add(contentType, list = new List<TProvider>());
			if (isDef)
				list.Insert(0, textStructureNavigatorProvider);
			else
				list.Add(textStructureNavigatorProvider);
		}
	}
}
