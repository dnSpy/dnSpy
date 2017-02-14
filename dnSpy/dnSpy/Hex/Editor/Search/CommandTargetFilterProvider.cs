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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Hex.Editor.Search {
	[ExportCommandTargetFilterProvider(CommandTargetFilterOrder.HexViewSearchService)]
	sealed class CommandTargetFilterProvider : ICommandTargetFilterProvider {
		readonly Lazy<HexViewSearchServiceProvider> hexViewSearchServiceProvider;

		[ImportingConstructor]
		CommandTargetFilterProvider(Lazy<HexViewSearchServiceProvider> hexViewSearchServiceProvider) => this.hexViewSearchServiceProvider = hexViewSearchServiceProvider;

		public ICommandTargetFilter Create(object target) {
			var hexView = target as WpfHexView;
			if (hexView?.Roles.Contains(PredefinedHexViewRoles.Interactive) != true)
				return null;

			return new CommandTargetFilter(hexViewSearchServiceProvider.Value, hexView);
		}
	}

	[ExportCommandTargetFilterProvider(CommandTargetFilterOrder.HexViewSearchServiceFocused)]
	sealed class CommandTargetFilterProviderFocus : ICommandTargetFilterProvider {
		readonly Lazy<HexViewSearchServiceProvider> hexViewSearchServiceProvider;

		[ImportingConstructor]
		CommandTargetFilterProviderFocus(Lazy<HexViewSearchServiceProvider> hexViewSearchServiceProvider) => this.hexViewSearchServiceProvider = hexViewSearchServiceProvider;

		public ICommandTargetFilter Create(object target) {
			var hexView = target as WpfHexView;
			if (hexView?.Roles.Contains(PredefinedHexViewRoles.Interactive) != true)
				return null;

			return new CommandTargetFilterFocus(hexViewSearchServiceProvider.Value, hexView);
		}
	}
}
