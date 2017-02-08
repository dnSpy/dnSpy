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
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Settings.HexGroups;

namespace dnSpy.Hex.HexGroups {
	[Export(typeof(HexEditorFactoryServiceListener))]
	sealed class HexEditorFactoryServiceListenerImpl : HexEditorFactoryServiceListener {
		readonly HexViewOptionsGroupServiceImpl hexViewOptionsGroupServiceImpl;

		[ImportingConstructor]
		HexEditorFactoryServiceListenerImpl(HexViewOptionsGroupServiceImpl hexViewOptionsGroupServiceImpl) {
			this.hexViewOptionsGroupServiceImpl = hexViewOptionsGroupServiceImpl;
		}

		public override void HexViewCreated(WpfHexView hexView) => hexViewOptionsGroupServiceImpl.HexViewCreated(hexView);
	}

	[Export(typeof(HexViewOptionsGroupService))]
	[Export(typeof(HexViewOptionsGroupServiceImpl))]
	sealed class HexViewOptionsGroupServiceImpl : HexViewOptionsGroupService {
		readonly Lazy<HexViewOptionsGroupNameProvider, IHexViewOptionsGroupNameProviderMetadata>[] hexViewOptionsGroupNameProviders;
		readonly Lazy<TagOptionDefinitionProvider, ITagOptionDefinitionProviderMetadata>[] tagOptionDefinitionProviders;
		readonly Dictionary<string, HexViewOptionsGroupImpl> nameToGroup;
		readonly OptionsStorage optionsStorage;

		[ImportingConstructor]
		HexViewOptionsGroupServiceImpl(ISettingsService settingsService, [ImportMany] IEnumerable<Lazy<HexViewOptionsGroupNameProvider, IHexViewOptionsGroupNameProviderMetadata>> hexViewOptionsGroupNameProviders, [ImportMany] IEnumerable<Lazy<TagOptionDefinitionProvider, ITagOptionDefinitionProviderMetadata>> tagOptionDefinitionProviders) {
			nameToGroup = new Dictionary<string, HexViewOptionsGroupImpl>(StringComparer.Ordinal);
			this.hexViewOptionsGroupNameProviders = hexViewOptionsGroupNameProviders.OrderBy(a => a.Metadata.Order).ToArray();
			this.tagOptionDefinitionProviders = tagOptionDefinitionProviders.OrderBy(a => a.Metadata.Order).ToArray();
			optionsStorage = new OptionsStorage(settingsService);
		}

		internal string GetSubGroup(WpfHexView hexView) {
			foreach (var lz in tagOptionDefinitionProviders) {
				var subGroup = lz.Value.GetSubGroup(hexView);
				if (subGroup != null)
					return subGroup;
			}
			return null;
		}

		public override HexViewOptionsGroup GetGroup(string name) => GetGroupCore(name);
		HexViewOptionsGroupImpl GetGroupCore(string name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			HexViewOptionsGroupImpl group;
			if (!nameToGroup.TryGetValue(name, out group)) {
				var defaultOptions = GetDefaultOptions(name);
				nameToGroup.Add(name, group = new HexViewOptionsGroupImpl(this, name, defaultOptions, optionsStorage));
			}
			return group;
		}

		TagOptionDefinition[] GetDefaultOptions(string groupName) {
			var options = new List<TagOptionDefinition>();
			foreach (var lz in tagOptionDefinitionProviders) {
				if (lz.Metadata.Group != groupName)
					continue;
				options.AddRange(lz.Value.GetOptions());
			}
			return options.Where(a => a.SubGroup != null && a.Name != null && a.Type != null).ToArray();
		}

		internal void HexViewCreated(WpfHexView hexView) {
			Debug.Assert(!hexView.IsClosed);
			if (hexView.IsClosed)
				return;

			foreach (var lz in hexViewOptionsGroupNameProviders) {
				var name = lz.Value.TryGetGroupName(hexView);
				if (name != null) {
					var group = GetGroupCore(name);
					group.HexViewCreated(hexView);
					break;
				}
			}
		}
	}
}
