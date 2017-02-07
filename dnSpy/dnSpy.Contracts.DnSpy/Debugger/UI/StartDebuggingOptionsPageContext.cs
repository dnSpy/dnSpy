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

namespace dnSpy.Contracts.Debugger.UI {
	/// <summary>
	/// Passed to a <see cref="StartDebuggingOptionsPageProvider"/> instance
	/// </summary>
	public sealed class StartDebuggingOptionsPageContext {
		/// <summary>
		/// Filename of the current selected file or an empty string
		/// </summary>
		public string CurrentFilename { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="currentFilename">Filename of the current selected file or an empty string</param>
		public StartDebuggingOptionsPageContext(string currentFilename) {
			if (currentFilename == null)
				throw new ArgumentNullException(nameof(currentFilename));
			CurrentFilename = currentFilename;
		}
	}
}
