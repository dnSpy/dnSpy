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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Controls;
using dnSpy.Debugger.Evaluation.UI;

namespace dnSpy.Debugger.ToolWindows.Watch {
	abstract class WatchContentFactory {
		public abstract bool TryGetContent(int index, out WatchContent watchContent);
		public abstract WatchContent GetContent(int index);
	}

	[Export(typeof(WatchContentFactory))]
	sealed class WatchContentFactoryImpl : WatchContentFactory {
		readonly WatchContent[] contents;
		readonly IWpfCommandService wpfCommandService;
		readonly Lazy<VariablesWindowVMFactory> variablesWindowVMFactory;
		readonly Lazy<WatchVariablesWindowValueNodesProviderService> watchVariablesWindowValueNodesProviderService;

		[ImportingConstructor]
		WatchContentFactoryImpl(IWpfCommandService wpfCommandService, Lazy<VariablesWindowVMFactory> variablesWindowVMFactory, Lazy<WatchVariablesWindowValueNodesProviderService> watchVariablesWindowValueNodesProviderService) {
			contents = new WatchContent[WatchWindowsHelper.NUMBER_OF_WATCH_WINDOWS];
			this.wpfCommandService = wpfCommandService;
			this.variablesWindowVMFactory = variablesWindowVMFactory;
			this.watchVariablesWindowValueNodesProviderService = watchVariablesWindowValueNodesProviderService;
		}

		public override bool TryGetContent(int index, out WatchContent watchContent) {
			watchContent = contents[index];
			return watchContent is not null;
		}

		public override WatchContent GetContent(int index) {
			var content = contents[index];
			if (content is null)
				contents[index] = content = new WatchContent(index, watchVariablesWindowValueNodesProviderService.Value.Get(index), wpfCommandService, variablesWindowVMFactory.Value);
			return content;
		}
	}
}
