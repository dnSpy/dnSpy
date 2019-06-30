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

namespace dnSpy.Contracts.Debugger.Breakpoints.Code.Dialogs {
	/// <summary>
	/// Shows the breakpoint settings dialog box
	/// </summary>
	public abstract class ShowCodeBreakpointSettingsService {
		/// <summary>
		/// Shows the breakpoint settings dialog box and returns the new settings or null if the user canceled
		/// </summary>
		/// <param name="settings">Settings to edit</param>
		/// <returns></returns>
		public abstract DbgCodeBreakpointSettings? Show(DbgCodeBreakpointSettings settings);

		/// <summary>
		/// Edits a breakpoint's settings
		/// </summary>
		/// <param name="breakpoint">Breakpoint</param>
		public void Edit(DbgCodeBreakpoint breakpoint) => Edit(new[] { breakpoint ?? throw new ArgumentNullException(nameof(breakpoint)) });

		/// <summary>
		/// Edits breakpoint settings and writes the new settings to <paramref name="breakpoints"/>
		/// </summary>
		/// <param name="breakpoints">Breakpoints</param>
		public abstract void Edit(DbgCodeBreakpoint[] breakpoints);
	}
}
