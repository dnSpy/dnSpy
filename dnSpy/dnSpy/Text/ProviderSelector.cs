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
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text {
	sealed class ProviderSelector<TProvider, TProviderMetadata> {
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly Dictionary<IContentType, List<Lazy<TProvider, TProviderMetadata>>> dict;

		public ProviderSelector(IContentTypeRegistryService contentTypeRegistryService, IEnumerable<Lazy<TProvider, TProviderMetadata>> providers, Func<Lazy<TProvider, TProviderMetadata>, IEnumerable<string>> getContentTypes) {
			if (contentTypeRegistryService == null)
				throw new ArgumentNullException(nameof(contentTypeRegistryService));
			if (getContentTypes == null)
				throw new ArgumentNullException(nameof(getContentTypes));
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.dict = CreateDictionary(providers, getContentTypes);
		}

		public IEnumerable<Lazy<TProvider, TProviderMetadata>> GetProviders(IContentType contentType) {
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));

			var ctDict = GetContentTypes(contentType);
			foreach (var c in ctDict.OrderBy(a => a.Value)) {
				List<Lazy<TProvider, TProviderMetadata>> list;
				if (dict.TryGetValue(c.Key, out list))
					return list;
			}
			return Array.Empty<Lazy<TProvider, TProviderMetadata>>();
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

		Dictionary<IContentType, List<Lazy<TProvider, TProviderMetadata>>> CreateDictionary(IEnumerable<Lazy<TProvider, TProviderMetadata>> providers, Func<Lazy<TProvider, TProviderMetadata>, IEnumerable<string>> getContentTypes) {
			var dict = new Dictionary<IContentType, List<Lazy<TProvider, TProviderMetadata>>>();
			var hash = new HashSet<IContentType>();
			var stack = new Stack<IContentType>();
			foreach (var p in providers) {
				foreach (var cs in getContentTypes(p)) {
					var c = contentTypeRegistryService.GetContentType(cs);
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

		static void Add(Dictionary<IContentType, List<Lazy<TProvider, TProviderMetadata>>> dict, Lazy<TProvider, TProviderMetadata> provider, IContentType contentType, bool isDef) {
			List<Lazy<TProvider, TProviderMetadata>> list;
			if (!dict.TryGetValue(contentType, out list))
				dict.Add(contentType, list = new List<Lazy<TProvider, TProviderMetadata>>());
			if (isDef)
				list.Insert(0, provider);
			else
				list.Add(provider);
		}
	}
}
