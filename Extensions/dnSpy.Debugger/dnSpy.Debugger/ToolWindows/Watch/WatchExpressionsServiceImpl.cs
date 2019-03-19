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
using dnSpy.Debugger.Evaluation.Watch;

namespace dnSpy.Debugger.ToolWindows.Watch {
	[Export(typeof(WatchExpressionsService))]
	sealed class WatchExpressionsServiceImpl : WatchExpressionsService {
		const int WATCH_WINDOW_INDEX = 0;
		readonly Lazy<ToolWindowsOperations> toolWindowsOperations;
		readonly Lazy<WatchContentFactory> watchContentFactory;

		[ImportingConstructor]
		WatchExpressionsServiceImpl(Lazy<ToolWindowsOperations> toolWindowsOperations, Lazy<WatchContentFactory> watchContentFactory) {
			this.toolWindowsOperations = toolWindowsOperations;
			this.watchContentFactory = watchContentFactory;
		}

		public override void AddExpressions(string[] expressions) {
			if (expressions == null)
				throw new ArgumentNullException(nameof(expressions));
			if (expressions.Length == 0)
				return;

			if (!toolWindowsOperations.Value.CanShowWatch(WATCH_WINDOW_INDEX))
				return;
			toolWindowsOperations.Value.ShowWatch(WATCH_WINDOW_INDEX);
			var vm = watchContentFactory.Value.GetContent(WATCH_WINDOW_INDEX).VM;
			vm.VM.AddExpressions(expressions, select: true);
		}
	}
}
