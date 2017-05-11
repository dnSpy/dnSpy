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
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.ToolWindows.Locals.Shared;

namespace dnSpy.Debugger.ToolWindows.Locals {
	[Export(typeof(IToolWindowContentProvider))]
	sealed class LocalsToolWindowContentProvider : LocalsToolWindowContentProviderBase {
		public static readonly Guid THE_GUID = new Guid("D799829F-CAE3-4F8F-AD81-1732ABC50636");
		[ImportingConstructor]
		LocalsToolWindowContentProvider(Lazy<LocalsContent> localsContent)
			: base(THE_GUID, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_LOCALS, dnSpy_Debugger_Resources.Window_Locals, new Lazy<ILocalsContent>(() => localsContent.Value)) { }
	}
}
