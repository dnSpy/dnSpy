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
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Debugger.Evaluation.UI;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.Watch {
	[Export, Export(typeof(IToolWindowContentProvider))]
	sealed class WatchToolWindowContentProvider : VariablesWindowToolWindowContentProviderBase {
		static readonly Guid THE_GUID = new Guid("A05AF486-59A0-4A2F-AB61-FC25151426C3");

		readonly Lazy<WatchContentFactory> watchContentFactory;

		[ImportingConstructor]
		WatchToolWindowContentProvider(Lazy<WatchContentFactory> watchContentFactory)
			: base(WatchWindowsHelper.NUMBER_OF_WATCH_WINDOWS, THE_GUID, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_WATCH) =>
			this.watchContentFactory = watchContentFactory;

		protected override string GetWindowTitle(int windowIndex) {
			if ((uint)windowIndex >= (uint)WatchWindowsHelper.NUMBER_OF_WATCH_WINDOWS)
				throw new ArgumentOutOfRangeException(nameof(windowIndex));
			return string.Format(dnSpy_Debugger_Resources.Window_Watch_N, windowIndex + 1);
		}

		protected override Lazy<IVariablesWindowContent> CreateVariablesWindowContent(int windowIndex) {
			if ((uint)windowIndex >= (uint)WatchWindowsHelper.NUMBER_OF_WATCH_WINDOWS)
				throw new ArgumentOutOfRangeException(nameof(windowIndex));
			return new Lazy<IVariablesWindowContent>(() => watchContentFactory.Value.GetContent(windowIndex));
		}
	}
}
