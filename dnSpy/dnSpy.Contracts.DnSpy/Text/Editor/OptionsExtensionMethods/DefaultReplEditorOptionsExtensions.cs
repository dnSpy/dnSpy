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
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Text.Editor.OptionsExtensionMethods {
	/// <summary>
	/// <see cref="DefaultReplEditorOptions"/> extension methods
	/// </summary>
	public static class DefaultReplEditorOptionsExtensions {
		/// <summary>
		/// Returns true if refresh-screen-on-change is enabled
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsReplRefreshScreenOnChangeEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultReplEditorOptions.RefreshScreenOnChangeId);
		}

		/// <summary>
		/// Returns the number of milliseconds to wait before refreshing the screen after the document gets changed
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static int GetReplRefreshScreenOnChangeWaitMilliSeconds(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultReplEditorOptions.RefreshScreenOnChangeWaitMilliSecondsId);
		}
	}
}
