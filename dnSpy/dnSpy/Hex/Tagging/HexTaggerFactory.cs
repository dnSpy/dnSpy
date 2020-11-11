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
using System.Linq;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Tagging;
using dnSpy.Hex.MEF;

namespace dnSpy.Hex.Tagging {
	[Export(typeof(HexTaggerFactory))]
	sealed class HexTaggerFactory {
		readonly Lazy<HexTaggerProvider, INamedTaggerMetadata>[] hexBufferTaggerProviders;
		readonly Lazy<HexViewTaggerProvider, IViewTaggerMetadata>[] hexViewTaggerProviders;

		[ImportingConstructor]
		HexTaggerFactory([ImportMany] IEnumerable<Lazy<HexTaggerProvider, INamedTaggerMetadata>> hexBufferTaggerProviders, [ImportMany] IEnumerable<Lazy<HexViewTaggerProvider, IViewTaggerMetadata>> hexViewTaggerProviders) {
			this.hexBufferTaggerProviders = hexBufferTaggerProviders.ToArray();
			this.hexViewTaggerProviders = hexViewTaggerProviders.ToArray();
		}

		public IEnumerable<IHexTagger<T>> Create<T>(HexView hexView, HexBuffer buffer) where T : HexTag {
			foreach (var t in Create<T>(buffer))
				yield return t;

			var type = typeof(T);
			foreach (var info in hexViewTaggerProviders) {
				if (info.Metadata.TextViewRoles is not null && !hexView.Roles.ContainsAny(info.Metadata.TextViewRoles))
					continue;
				if (CanCreateTagger(type, info.Metadata.TagTypes)) {
					var tagger = info.Value.CreateTagger<T>(hexView, buffer);
					if (tagger is not null)
						yield return tagger;
				}
			}
		}

		public IEnumerable<IHexTagger<T>> Create<T>(HexBuffer buffer) where T : HexTag {
			var type = typeof(T);
			foreach (var info in hexBufferTaggerProviders) {
				if (CanCreateTagger(type, info.Metadata.TagTypes)) {
					var tagger = info.Value.CreateTagger<T>(buffer);
					if (tagger is not null)
						yield return tagger;
				}
			}
		}

		static bool CanCreateTagger(Type type, IEnumerable<Type> types) {
			foreach (var t in types) {
				if (type.IsAssignableFrom(t))
					return true;
			}
			return false;
		}
	}
}
