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

namespace dnSpy.Contracts.Files.Tabs.TextEditor {
	/// <summary>
	/// <see cref="ITextLineObject"/> list modified event args
	/// </summary>
	public sealed class TextLineObjectListModifiedEventArgs : EventArgs {
		/// <summary>
		/// Added/removed object
		/// </summary>
		public ITextLineObject TextLineObject { get; }

		/// <summary>
		/// true if added, false if removed
		/// </summary>
		public bool Added { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="obj">Object</param>
		/// <param name="added">true if added</param>
		public TextLineObjectListModifiedEventArgs(ITextLineObject obj, bool added) {
			this.TextLineObject = obj;
			this.Added = added;
		}
	}
}
