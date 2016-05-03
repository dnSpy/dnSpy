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
	/// <see cref="ITextLineObject"/> event args
	/// </summary>
	public sealed class TextLineObjectEventArgs : EventArgs {
		/// <summary>
		/// The object needs to be redrawn
		/// </summary>
		public static readonly string RedrawProperty = "Redraw";

		/// <summary>
		/// Gets the property
		/// </summary>
		public string Property { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="property">Property</param>
		public TextLineObjectEventArgs(string property) {
			this.Property = property;
		}
	}
}
