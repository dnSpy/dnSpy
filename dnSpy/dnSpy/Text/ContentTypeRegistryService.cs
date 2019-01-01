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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text {
	[Export(typeof(IContentTypeRegistryService))]
	[Export(typeof(IContentTypeRegistryService2))]
	sealed class ContentTypeRegistryService : IContentTypeRegistryService2 {
		const string UnknownContentTypeName = "UNKNOWN";
		const string TextPrefix = "text/";
		const string MimeTypePrefix = TextPrefix + "x-";

		readonly object lockObj = new object();
		readonly Dictionary<string, ContentType> contentTypes;
		readonly Dictionary<string, ContentType> mimeTypeToContentType;

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
				public string MimeType { get; }

				public RawContentType(string guid, string[] baseGuids, string mimeType) {
					Typename = guid;
					BaseTypes = baseGuids;
					MimeType = mimeType;
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
					var rawCt = new RawContentType(typeName, baseTypes, md.MimeType);
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

				return owner.AddContentType_NoLock(rawCt.Typename, baseTypes, rawCt.MimeType);
			}
		}

		[ImportingConstructor]
		ContentTypeRegistryService([ImportMany] IEnumerable<Lazy<ContentTypeDefinition, IContentTypeDefinitionMetadata>> contentTypeDefinitions) {
			contentTypes = new Dictionary<string, ContentType>(StringComparer.OrdinalIgnoreCase);
			mimeTypeToContentType = new Dictionary<string, ContentType>(StringComparer.Ordinal);
			const string mimeType = null;
			AddContentTypeInternal_NoLock(UnknownContentTypeName, Array.Empty<string>(), mimeType);
			new ContentTypeCreator(this, contentTypeDefinitions);
		}

		public IContentType AddContentType(string typeName, IEnumerable<string> baseTypes) {
			if (StringComparer.OrdinalIgnoreCase.Equals(typeName, UnknownContentTypeName))
				throw new ArgumentException("Guid is reserved", nameof(typeName));
			const string mimeType = null;
			lock (lockObj)
				return AddContentTypeInternal_NoLock(typeName, baseTypes, mimeType);
		}

		IContentType AddContentTypeInternal_NoLock(string typeName, IEnumerable<string> baseTypesEnumerable, string mimeType) {
			if (contentTypes.ContainsKey(typeName))
				throw new ArgumentException("Content type already exists", nameof(typeName));
			var btGuids = baseTypesEnumerable.ToArray();
			if (btGuids.Any(a => a == UnknownContentTypeName))
				throw new ArgumentException("Can't derive from the unknown content type", nameof(baseTypesEnumerable));
			var baseTypes = baseTypesEnumerable.Select(a => GetContentType(a)).ToArray();
			return AddContentType_NoLock(typeName, baseTypes, mimeType);
		}

		ContentType AddContentType_NoLock(string typeName, IContentType[] baseTypes, string mimeType) {
			bool addMimeType;
			if (string.IsNullOrWhiteSpace(mimeType)) {
				addMimeType = false;
				mimeType = MimeTypePrefix + typeName.ToLowerInvariant();
			}
			else if (mimeTypeToContentType.ContainsKey(mimeType)) {
				addMimeType = false;
				mimeType = null;
			}
			else
				addMimeType = true;
			var ct = new ContentType(typeName, mimeType, baseTypes);
			contentTypes.Add(typeName, ct);
			if (addMimeType)
				mimeTypeToContentType.Add(mimeType, ct);
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
			lock (lockObj) {
				if (contentTypes.TryGetValue(typeName, out var ct)) {
					if (ct.MimeType != null)
						mimeTypeToContentType.Remove(ct.MimeType);
					contentTypes.Remove(typeName);
				}
			}
		}

		public IContentType GetContentTypeForMimeType(string mimeType) {
			if (string.IsNullOrWhiteSpace(mimeType))
				throw new ArgumentException();
			lock (lockObj) {
				if (mimeTypeToContentType.TryGetValue(mimeType, out var ct))
					return ct;
				if (!mimeType.StartsWith(TextPrefix))
					return null;
				if (mimeType.StartsWith(MimeTypePrefix) && contentTypes.TryGetValue(mimeType.Substring(MimeTypePrefix.Length), out ct))
					return ct;
				if (contentTypes.TryGetValue(mimeType.Substring(TextPrefix.Length), out ct))
					return ct;
				return null;
			}
		}

		public string GetMimeType(IContentType type) {
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			if (type is ContentType ct)
				return ct.MimeType;
			throw new ArgumentException();
		}
	}
}
