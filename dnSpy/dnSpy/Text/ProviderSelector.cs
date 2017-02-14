/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text {
	sealed class ProviderSelector<TProvider, TProviderMetadata> where TProviderMetadata : IContentTypeMetadata {
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly Dictionary<IContentType, Lazy<TProvider, TProviderMetadata>[]> dict;
		readonly Lazy<TProvider, TProviderMetadata>[] providers;

		public ProviderSelector(IContentTypeRegistryService contentTypeRegistryService, IEnumerable<Lazy<TProvider, TProviderMetadata>> providers) {
			this.contentTypeRegistryService = contentTypeRegistryService ?? throw new ArgumentNullException(nameof(contentTypeRegistryService));
			dict = new Dictionary<IContentType, Lazy<TProvider, TProviderMetadata>[]>();
			this.providers = providers.ToArray();
		}

		public IEnumerable<Lazy<TProvider, TProviderMetadata>> GetProviders(IContentType contentType) {
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));

			if (!dict.TryGetValue(contentType, out var result))
				dict[contentType] = result = CreateProviderList(contentType);
			return result;
		}

		Lazy<TProvider, TProviderMetadata>[] CreateProviderList(IContentType contentType) {
			List<(Lazy<TProvider, TProviderMetadata> lz, int dist)> list = null;

			// We only allow a provider to match if its supported content type equals the
			// requested content type or if it's a child of the requested content type.
			// Eg. "CSharp" only matches "CSharp" (since nothing derives from it), so the
			// Roslyn text structure navigator provider won't match "text" content types.

			foreach (var provider in providers) {
				foreach (var ctString in provider.Metadata.ContentTypes) {
					var ct = contentTypeRegistryService.GetContentType(ctString);
					Debug.Assert(ct != null);
					if (ct == null)
						continue;
					int dist = GetDistance(ct, contentType);
					if (dist < 0)
						continue;
					if (list == null)
						list = new List<(Lazy<TProvider, TProviderMetadata>, int)>();
					list.Add((provider, dist));
				}
			}

			if (list == null)
				return Array.Empty<Lazy<TProvider, TProviderMetadata>>();
			list.Sort((a, b) => a.dist - b.dist);
			return list.Select(a => a.lz).ToArray();
		}

		int GetDistance(IContentType baseType, IContentType other) {
			if (baseType == other)
				return 0;
			foreach (var bt in other.BaseTypes) {
				int dist = GetDistance(baseType, bt);
				if (dist >= 0)
					return dist + 1;
			}
			return -1;
		}
	}
}
