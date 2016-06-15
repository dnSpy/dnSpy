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
using System.Text;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;

namespace dnSpy.Text.Classification {
	[Export(typeof(IClassificationTypeRegistryService))]
	sealed class ClassificationTypeRegistryService : IClassificationTypeRegistryService {
		readonly Dictionary<Guid, IClassificationType> toClassificationType;
		readonly Dictionary<string, IClassificationType> transientNameToType;
		readonly IClassificationType transientClassificationType;

		// From ContentTypeRegistryService
		sealed class ClassificationTypeCreator {
			readonly ClassificationTypeRegistryService owner;
			readonly Dictionary<Guid, RawClassificationType> rawClassificationTypes;

			sealed class RawClassificationType {
				public Guid Guid { get; }
				public string DisplayName { get; }
				public Guid[] BaseGuids { get; }

				public RawClassificationType(Guid guid, string displayName, Guid[] baseGuids) {
					Guid = guid;
					DisplayName = displayName;
					BaseGuids = baseGuids;
				}
			}

			public ClassificationTypeCreator(ClassificationTypeRegistryService owner, IEnumerable<Lazy<ClassificationTypeDefinition, IDictionary<string, object>>> classificationTypeDefinitions) {
				this.owner = owner;
				this.rawClassificationTypes = new Dictionary<Guid, RawClassificationType>();
				foreach (var md in classificationTypeDefinitions.Select(a => a.Metadata)) {
					var guid = GetGuid(md);
					Debug.Assert(guid != null);
					if (guid == null)
						continue;
					Debug.Assert(!rawClassificationTypes.ContainsKey(guid.Value));
					if (rawClassificationTypes.ContainsKey(guid.Value))
						continue;
					var baseGuids = GetBaseGuids(md);
					Debug.Assert(baseGuids != null);
					if (baseGuids == null)
						continue;
					var displayName = GetDisplayName(md);
					var rawCt = new RawClassificationType(guid.Value, displayName, baseGuids);
					rawClassificationTypes.Add(rawCt.Guid, rawCt);
				}
				var list = rawClassificationTypes.Values.Select(a => a.Guid).ToArray();
				foreach (var guid in list)
					TryCreate(guid, 0);
			}

			IClassificationType TryGet(Guid guid) {
				IClassificationType classificationType;
				owner.toClassificationType.TryGetValue(guid, out classificationType);
				return classificationType;
			}

			IClassificationType TryCreate(Guid guid, int recurse) {
				var ct = TryGet(guid);
				if (ct != null)
					return ct;

				const int MAX_RECURSE = 1000;
				Debug.Assert(recurse <= MAX_RECURSE);
				if (recurse > MAX_RECURSE)
					return null;

				RawClassificationType rawCt;
				bool b = rawClassificationTypes.TryGetValue(guid, out rawCt);
				Debug.Assert(b);
				if (!b)
					return null;
				b = rawClassificationTypes.Remove(rawCt.Guid);
				Debug.Assert(b);

				var baseTypes = new IClassificationType[rawCt.BaseGuids.Length];
				for (int i = 0; i < baseTypes.Length; i++) {
					var btClassificationType = TryCreate(rawCt.BaseGuids[i], recurse + 1);
					if (btClassificationType == null)
						return null;
					baseTypes[i] = btClassificationType;
				}

				ct = new ClassificationType(rawCt.Guid, GetDisplayNameInternal(rawCt.Guid, rawCt.DisplayName), baseTypes);
				owner.toClassificationType.Add(ct.Classification, ct);
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
		ClassificationTypeRegistryService([ImportMany] IEnumerable<Lazy<ClassificationTypeDefinition, IDictionary<string, object>>> classificationTypeDefinitions) {
			this.toClassificationType = new Dictionary<Guid, IClassificationType>();
			this.transientNameToType = new Dictionary<string, IClassificationType>();
			new ClassificationTypeCreator(this, classificationTypeDefinitions);
			this.transientClassificationType = GetClassificationType(TRANSIENT_GUID);
			if (this.transientClassificationType == null)
				throw new InvalidOperationException();
		}

		const string TRANSIENT_GUID = "7CF31DEF-0A08-49E0-994B-2B7B542DA21A";
		[ExportClassificationTypeDefinition(TRANSIENT_GUID)]
		[DisplayName("transient")]
#pragma warning disable CS0169
		static ClassificationTypeDefinition _transientClassificationType;
#pragma warning restore CS0169

		static string GetDisplayNameInternal(Guid guid, string displayName) => displayName ?? guid.ToString();

		public IClassificationType CreateClassificationType(string type, IEnumerable<IClassificationType> baseTypes) =>
			CreateClassificationType(Guid.Parse(type), baseTypes);
		public IClassificationType CreateClassificationType(Guid type, IEnumerable<IClassificationType> baseTypes) {
			if (baseTypes == null)
				throw new ArgumentNullException(nameof(baseTypes));
			if (toClassificationType.ContainsKey(type))
				throw new InvalidOperationException();
			string displayName = null;
			var ct = new ClassificationType(type, GetDisplayNameInternal(type, displayName), baseTypes);
			toClassificationType.Add(type, ct);
			return ct;
		}

		public IClassificationType CreateTransientClassificationType(params IClassificationType[] baseTypes) =>
			CreateTransientClassificationType((IEnumerable<IClassificationType>)baseTypes);
		public IClassificationType CreateTransientClassificationType(IEnumerable<IClassificationType> baseTypes) {
			if (baseTypes == null)
				throw new ArgumentNullException(nameof(baseTypes));
			var bts = baseTypes.ToArray();
			if (bts.Length == 0)
				throw new InvalidOperationException();

			Array.Sort(bts, (a, b) => a.Classification.CompareTo(b.Classification));
			var name = GetTransientDisplayName(bts);
			IClassificationType ct;
			if (transientNameToType.TryGetValue(name, out ct))
				return ct;

			ct = new ClassificationType(Guid.NewGuid(), name, baseTypes);
			transientNameToType.Add(name, ct);
			return ct;
		}

		string GetTransientDisplayName(IEnumerable<IClassificationType> baseTypes) {
			var sb = new StringBuilder();
			foreach (var bt in baseTypes) {
				sb.Append(bt.Classification.ToString());
				sb.Append(" - ");
			}
			sb.Append(transientClassificationType.Classification.ToString());
			return sb.ToString();
		}

		public IClassificationType GetClassificationType(string type) =>
			GetClassificationType(Guid.Parse(type));
		public IClassificationType GetClassificationType(Guid type) {
			IClassificationType ct;
			toClassificationType.TryGetValue(type, out ct);
			return ct;
		}
	}
}
