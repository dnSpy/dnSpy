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
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Classification {
	[Export(typeof(IClassificationTypeRegistryService))]
	sealed class ClassificationTypeRegistryService : IClassificationTypeRegistryService {
		readonly Dictionary<string, IClassificationType> toClassificationType;
		readonly Dictionary<string, IClassificationType> transientNameToType;
		readonly IClassificationType transientClassificationType;

		// From ContentTypeRegistryService
		sealed class ClassificationTypeCreator {
			readonly ClassificationTypeRegistryService owner;
			readonly Dictionary<string, RawClassificationType> rawClassificationTypes;

			sealed class RawClassificationType {
				public string Type { get; }
				public string[] BaseTypes { get; }

				public RawClassificationType(string guid, string[] baseGuids) {
					Type = guid;
					BaseTypes = baseGuids;
				}
			}

			public ClassificationTypeCreator(ClassificationTypeRegistryService owner, IEnumerable<Lazy<ClassificationTypeDefinition, IClassificationTypeDefinitionMetadata>> classificationTypeDefinitions) {
				this.owner = owner;
				this.rawClassificationTypes = new Dictionary<string, RawClassificationType>();
				foreach (var md in classificationTypeDefinitions.Select(a => a.Metadata)) {
					var type = md.Name;
					Debug.Assert(type != null);
					if (type == null)
						continue;
					Debug.Assert(!rawClassificationTypes.ContainsKey(type));
					if (rawClassificationTypes.ContainsKey(type))
						continue;
					var baseTypes = (md.BaseDefinition ?? Array.Empty<string>()).ToArray();
					var rawCt = new RawClassificationType(type, baseTypes);
					rawClassificationTypes.Add(rawCt.Type, rawCt);
				}
				var list = rawClassificationTypes.Values.Select(a => a.Type).ToArray();
				foreach (var type in list)
					TryCreate(type, 0);
			}

			IClassificationType TryGet(string type) {
				IClassificationType classificationType;
				owner.toClassificationType.TryGetValue(type, out classificationType);
				return classificationType;
			}

			IClassificationType TryCreate(string type, int recurse) {
				var ct = TryGet(type);
				if (ct != null)
					return ct;

				const int MAX_RECURSE = 1000;
				Debug.Assert(recurse <= MAX_RECURSE);
				if (recurse > MAX_RECURSE)
					return null;

				RawClassificationType rawCt;
				bool b = rawClassificationTypes.TryGetValue(type, out rawCt);
				Debug.Assert(b);
				if (!b)
					return null;
				b = rawClassificationTypes.Remove(rawCt.Type);
				Debug.Assert(b);

				var baseTypes = new IClassificationType[rawCt.BaseTypes.Length];
				for (int i = 0; i < baseTypes.Length; i++) {
					var btClassificationType = TryCreate(rawCt.BaseTypes[i], recurse + 1);
					if (btClassificationType == null)
						return null;
					baseTypes[i] = btClassificationType;
				}

				ct = new ClassificationType(rawCt.Type, baseTypes);
				owner.toClassificationType.Add(ct.Classification, ct);
				return ct;
			}
		}

		[ImportingConstructor]
		ClassificationTypeRegistryService([ImportMany] IEnumerable<Lazy<ClassificationTypeDefinition, IClassificationTypeDefinitionMetadata>> classificationTypeDefinitions) {
			this.toClassificationType = new Dictionary<string, IClassificationType>();
			this.transientNameToType = new Dictionary<string, IClassificationType>();
			new ClassificationTypeCreator(this, classificationTypeDefinitions);
			this.transientClassificationType = GetClassificationType(TRANSIENT_NAME);
			if (this.transientClassificationType == null)
				throw new InvalidOperationException();
		}

		const string TRANSIENT_NAME = "(TRANSIENT)";
#pragma warning disable 0169
		[Export, Name(TRANSIENT_NAME)]
		static ClassificationTypeDefinition _transientClassificationTypeDefinition;
#pragma warning restore 0169

		public IClassificationType CreateClassificationType(string type, IEnumerable<IClassificationType> baseTypes) {
			if (baseTypes == null)
				throw new ArgumentNullException(nameof(baseTypes));
			if (toClassificationType.ContainsKey(type))
				throw new InvalidOperationException();
			var ct = new ClassificationType(type, baseTypes);
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
			var name = GetTransientName(bts);
			IClassificationType ct;
			if (transientNameToType.TryGetValue(name, out ct))
				return ct;

			ct = new ClassificationType(name, baseTypes);
			transientNameToType.Add(name, ct);
			return ct;
		}

		string GetTransientName(IEnumerable<IClassificationType> baseTypes) {
			var sb = new StringBuilder();
			foreach (var bt in baseTypes) {
				sb.Append(bt.Classification);
				sb.Append(" - ");
			}
			sb.Append(TRANSIENT_NAME);
			return sb.ToString();
		}

		public IClassificationType GetClassificationType(string type) {
			IClassificationType ct;
			toClassificationType.TryGetValue(type, out ct);
			return ct;
		}
	}
}
