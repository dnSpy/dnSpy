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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Command;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor.Search {
	[ExportCommandTargetFilterProvider(CommandConstants.CMDTARGETFILTER_ORDER_SEARCH)]
	sealed class CommandTargetFilterProvider : ICommandTargetFilterProvider {
		readonly Lazy<ISearchServiceProvider> searchServiceProvider;

		[ImportingConstructor]
		CommandTargetFilterProvider(Lazy<ISearchServiceProvider> searchServiceProvider) {
			this.searchServiceProvider = searchServiceProvider;
		}

		public ICommandTargetFilter Create(object target) {
			var textView = target as IWpfTextView;
			if (textView?.Roles.Contains(PredefinedTextViewRoles.Interactive) != true)
				return null;

			return new CommandTargetFilter(searchServiceProvider.Value, textView);
		}
	}

	[ExportCommandTargetFilterProvider(CommandConstants.CMDTARGETFILTER_ORDER_SEARCH_FOCUS)]
	sealed class CommandTargetFilterProviderFocus : ICommandTargetFilterProvider {
		readonly Lazy<ISearchServiceProvider> searchServiceProvider;

		[ImportingConstructor]
		CommandTargetFilterProviderFocus(Lazy<ISearchServiceProvider> searchServiceProvider) {
			this.searchServiceProvider = searchServiceProvider;
		}

		public ICommandTargetFilter Create(object target) {
			var textView = target as IWpfTextView;
			if (textView?.Roles.Contains(PredefinedTextViewRoles.Interactive) != true)
				return null;

			return new CommandTargetFilterFocus(searchServiceProvider.Value, textView);
		}
	}
}
