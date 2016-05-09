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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.TextEditor;

namespace dnSpy.TextEditor {
	[Export, Export(typeof(IContentTypeRegistryService)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class ContentTypeRegistryService : IContentTypeRegistryService {
		static readonly Guid UnknownContentTypeGuid = new Guid("CAC55A72-3AD1-4CB3-87BB-B3B607682C0B");

		readonly object lockObj = new object();
		Dictionary<Guid, ContentType> contentTypes;

		public IEnumerable<IContentType> ContentTypes {
			get {
				lock (lockObj)
					return contentTypes.Values.ToArray();
			}
		}

		public IContentType UnknownContentType {
			get {
				lock (lockObj)
					return contentTypes[UnknownContentTypeGuid];
			}
		}

		sealed class ContentTypeCreator {
			readonly ContentTypeRegistryService owner;
			readonly Dictionary<Guid, RawContentType> rawContentTypes;

			sealed class RawContentType {
				public Guid Guid { get; }
				public string DisplayName { get; }
				public Guid[] BaseGuids { get; }

				public RawContentType(Guid guid, string displayName, Guid[] baseGuids) {
					Guid = guid;
					DisplayName = displayName;
					BaseGuids = baseGuids;
				}
			}

			public ContentTypeCreator(ContentTypeRegistryService owner, IEnumerable<Lazy<ContentTypeDefinition, IDictionary<string, object>>> contentTypeDefinitions) {
				this.owner = owner;
				this.rawContentTypes = new Dictionary<Guid, RawContentType>();
				foreach (var md in contentTypeDefinitions.Select(a => a.Metadata)) {
					var guid = GetGuid(md);
					Debug.Assert(guid != null);
					if (guid == null)
						continue;
					Debug.Assert(!rawContentTypes.ContainsKey(guid.Value));
					if (rawContentTypes.ContainsKey(guid.Value))
						continue;
					var baseGuids = GetBaseGuids(md);
					Debug.Assert(baseGuids != null);
					if (baseGuids == null)
						continue;
					var displayName = GetDisplayName(md);
					var rawCt = new RawContentType(guid.Value, displayName, baseGuids);
					rawContentTypes.Add(rawCt.Guid, rawCt);
				}
				var list = rawContentTypes.Values.Select(a => a.Guid).ToArray();
				foreach (var guid in list)
					TryCreate(guid, 0);
			}

			ContentType TryGet(Guid guid) {
				ContentType contentType;
				owner.contentTypes.TryGetValue(guid, out contentType);
				return contentType;
			}

			ContentType TryCreate(Guid guid, int recurse) {
				var ct = TryGet(guid);
				if (ct != null)
					return ct;

				const int MAX_RECURSE = 1000;
				Debug.Assert(recurse <= MAX_RECURSE);
				if (recurse > MAX_RECURSE)
					return null;

				RawContentType rawCt;
				bool b = rawContentTypes.TryGetValue(guid, out rawCt);
				Debug.Assert(b);
				if (!b)
					return null;
				b = rawContentTypes.Remove(rawCt.Guid);
				Debug.Assert(b);

				var baseTypes = new ContentType[rawCt.BaseGuids.Length];
				for (int i = 0; i < baseTypes.Length; i++) {
					var btContentType = TryCreate(rawCt.BaseGuids[i], recurse + 1);
					if (btContentType == null)
						return null;
					baseTypes[i] = btContentType;
				}

				ct = new ContentType(rawCt.Guid, GetDisplayNameInternal(rawCt.Guid, rawCt.DisplayName), baseTypes);
				owner.contentTypes.Add(ct.Guid, ct);
				return ct;
			}

			Guid? GetGuid(IDictionary<string, object> md) {
				object obj;
				if (!md.TryGetValue("Guid", out obj))
					return null;
				string s = obj as string;
				if (s == null)
					return null;
				Guid guid;
				if (!Guid.TryParse(s, out guid))
					return null;

				return guid;
			}

			Guid[] GetBaseGuids(IDictionary<string, object> md) {
				object obj;
				if (!md.TryGetValue("BaseDefinition", out obj))
					return Array.Empty<Guid>();
				var guidStrings = obj as string[];
				if (guidStrings == null)
					return Array.Empty<Guid>();
				var guids = new Guid[guidStrings.Length];
				for (int i = 0; i < guidStrings.Length; i++) {
					Guid guid;
					if (!Guid.TryParse(guidStrings[i], out guid))
						return null;
					guids[i] = guid;
				}
				return guids;
			}

			string GetDisplayName(IDictionary<string, object> md) {
				object obj;
				md.TryGetValue("DisplayName", out obj);
				return obj as string;
			}
		}

		[ImportingConstructor]
		ContentTypeRegistryService([ImportMany] IEnumerable<Lazy<ContentTypeDefinition, IDictionary<string, object>>> contentTypeDefinitions) {
			this.contentTypes = new Dictionary<Guid, ContentType>();
			AddContentTypeInternal_NoLock(UnknownContentTypeGuid, "Unknown", Enumerable.Empty<Guid>());
			new ContentTypeCreator(this, contentTypeDefinitions);
		}

		public IContentType AddContentType(Guid guid, IEnumerable<Guid> baseTypeGuids) {
			if (guid == UnknownContentTypeGuid)
				throw new ArgumentException("Guid is reserved", nameof(guid));
			lock (lockObj)
				return AddContentTypeInternal_NoLock(guid, null, baseTypeGuids);
		}

		static string GetDisplayNameInternal(Guid guid, string displayName) => displayName ?? guid.ToString();

		IContentType AddContentTypeInternal_NoLock(Guid guid, string displayName, IEnumerable<Guid> baseTypeGuids) {
			displayName = GetDisplayNameInternal(guid, displayName);
			if (contentTypes.ContainsKey(guid))
				throw new ArgumentException("Content type already exists", nameof(guid));
			var btGuids = baseTypeGuids.ToArray();
			if (btGuids.Any(a => a == UnknownContentTypeGuid))
				throw new ArgumentException("Can't derive from the unknown content type", nameof(baseTypeGuids));
			var baseTypes = baseTypeGuids.Select(a => GetContentType(a)).ToArray();
			var ct = new ContentType(guid, displayName, baseTypes);
			contentTypes.Add(ct.Guid, ct);
			return ct;
		}

		public IContentType GetContentType(Guid guid) {
			ContentType contentType;
			lock (lockObj)
				contentTypes.TryGetValue(guid, out contentType);
			Debug.Assert(contentType != null);
			return contentType;
		}

		public IContentType AddContentType(string guid, IEnumerable<string> baseTypeGuids) =>
			AddContentType(Guid.Parse(guid), baseTypeGuids.Select(a => Guid.Parse(a)));
		public IContentType GetContentType(string guid) => GetContentType(Guid.Parse(guid));

		public IContentType GetContentType(object contentType) {
			var ct = contentType as IContentType;
			if (ct != null)
				return ct;

			var contentTypeGuid = contentType as Guid?;
			if (contentTypeGuid != null)
				return GetContentType(contentTypeGuid.Value);

			var contentTypeString = contentType as string;
			if (contentTypeString != null)
				return GetContentType(contentTypeString);

			return null;
		}
	}
}
