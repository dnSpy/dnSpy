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

namespace dnSpy.Contracts.Files.Tabs {
	/// <summary>
	/// Show tab content event args
	/// </summary>
	public sealed class ShowTabContentEventArgs : EventArgs {
		/// <summary>
		/// true if the content was shown successfully (eg. no exceptions when decompiling code)
		/// </summary>
		public bool Success { get; private set; }

		/// <summary>
		/// Set to true if the caret has been moved by a previous handler
		/// </summary>
		public bool HasMovedCaret { get; set; }

		/// <summary>
		/// Gets the tab
		/// </summary>
		public IFileTab Tab { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="success">See <see cref="Success"/></param>
		/// <param name="tab">Tab</param>
		public ShowTabContentEventArgs(bool success, IFileTab tab) {
			this.Success = success;
			this.HasMovedCaret = false;
			this.Tab = tab;
		}
	}
}
