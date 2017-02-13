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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text {
	[Export(typeof(IContentTypeRegistryService))]
	sealed class ContentTypeRegistryService : IContentTypeRegistryService {
		const string UnknownContentTypeName = "UNKNOWN";

		readonly object lockObj = new object();
		Dictionary<string, ContentType> contentTypes;

		public IEnumerable<IContentType> ContentTypes {
			get {
				lock (lockObj)
					return contentTypes.Values.ToArray();
			}
		}

		public IContentType UnknownContentType {
			get {
				lock (lockObj)
					return contentTypes[UnknownContentTypeName];
			}
		}

		sealed class ContentTypeCreator {
			readonly ContentTypeRegistryService owner;
			readonly Dictionary<string, RawContentType> rawContentTypes;

			sealed class RawContentType {
				public string Typename { get; }
				public string[] BaseTypes { get; }

				public RawContentType(string guid, string[] baseGuids) {
					Typename = guid;
					BaseTypes = baseGuids;
				}
			}

			public ContentTypeCreator(ContentTypeRegistryService owner, IEnumerable<Lazy<ContentTypeDefinition, IContentTypeDefinitionMetadata>> contentTypeDefinitions) {
				this.owner = owner;
				rawContentTypes = new Dictionary<string, RawContentType>(StringComparer.OrdinalIgnoreCase);
				foreach (var md in contentTypeDefinitions.Select(a => a.Metadata)) {
					var typeName = md.Name;
					Debug.Assert(typeName != null);
					if (typeName == null)
						continue;
					Debug.Assert(!rawContentTypes.ContainsKey(typeName));
					if (rawContentTypes.ContainsKey(typeName))
						continue;
					var baseTypes = (md.BaseDefinition ?? Array.Empty<string>()).ToArray();
					Debug.Assert(baseTypes != null);
					if (baseTypes == null)
						continue;
					var rawCt = new RawContentType(typeName, baseTypes);
					rawContentTypes.Add(rawCt.Typename, rawCt);
				}
				var list = rawContentTypes.Values.Select(a => a.Typename).ToArray();
				foreach (var typeName in list)
					TryCreate(typeName, 0);
			}

			ContentType TryGet(string typeName) {
				owner.contentTypes.TryGetValue(typeName, out var contentType);
				return contentType;
			}

			ContentType TryCreate(string typeName, int recurse) {
				var ct = TryGet(typeName);
				if (ct != null)
					return ct;

				const int MAX_RECURSE = 1000;
				Debug.Assert(recurse <= MAX_RECURSE);
				if (recurse > MAX_RECURSE)
					return null;

				bool b = rawContentTypes.TryGetValue(typeName, out var rawCt);
				Debug.Assert(b);
				if (!b)
					return null;
				b = rawContentTypes.Remove(rawCt.Typename);
				Debug.Assert(b);

				var baseTypes = new ContentType[rawCt.BaseTypes.Length];
				for (int i = 0; i < baseTypes.Length; i++) {
					var btContentType = TryCreate(rawCt.BaseTypes[i], recurse + 1);
					if (btContentType == null)
						return null;
					baseTypes[i] = btContentType;
				}

				ct = new ContentType(rawCt.Typename, baseTypes);
				owner.contentTypes.Add(ct.TypeName, ct);
				return ct;
			}
		}

		[ImportingConstructor]
		ContentTypeRegistryService([ImportMany] IEnumerable<Lazy<ContentTypeDefinition, IContentTypeDefinitionMetadata>> contentTypeDefinitions) {
			contentTypes = new Dictionary<string, ContentType>(StringComparer.OrdinalIgnoreCase);
			AddContentTypeInternal_NoLock(UnknownContentTypeName, Array.Empty<string>());
			new ContentTypeCreator(this, contentTypeDefinitions);
		}

		public IContentType AddContentType(string typeName, IEnumerable<string> baseTypes) {
			if (StringComparer.OrdinalIgnoreCase.Equals(typeName, UnknownContentTypeName))
				throw new ArgumentException("Guid is reserved", nameof(typeName));
			lock (lockObj)
				return AddContentTypeInternal_NoLock(typeName, baseTypes);
		}

		IContentType AddContentTypeInternal_NoLock(string typeName, IEnumerable<string> baseTypesEnumerable) {
			if (contentTypes.ContainsKey(typeName))
				throw new ArgumentException("Content type already exists", nameof(typeName));
			var btGuids = baseTypesEnumerable.ToArray();
			if (btGuids.Any(a => a == UnknownContentTypeName))
				throw new ArgumentException("Can't derive from the unknown content type", nameof(baseTypesEnumerable));
			var baseTypes = baseTypesEnumerable.Select(a => GetContentType(a)).ToArray();
			var ct = new ContentType(typeName, baseTypes);
			contentTypes.Add(ct.TypeName, ct);
			return ct;
		}

		public IContentType GetContentType(string typeName) {
			ContentType contentType;
			lock (lockObj)
				contentTypes.TryGetValue(typeName, out contentType);
			return contentType;
		}

		public void RemoveContentType(string typeName) {
			if (StringComparer.OrdinalIgnoreCase.Equals(typeName, UnknownContentTypeName))
				throw new ArgumentException("Guid is reserved", nameof(typeName));
			lock (lockObj)
				contentTypes.Remove(typeName);
		}
	}
}
