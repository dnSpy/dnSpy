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

namespace dnSpy.Contracts.Disassembly.Viewer {
	/// <summary>
	/// Creates the text shown in the disassembly window and notifies listeners when the text is changed.
	/// Created by <see cref="DisassemblyContentProviderFactory"/>
	/// </summary>
	public abstract class DisassemblyContentProvider {
		/// <summary>
		/// Gets the title or null. This can be shown in the UI
		/// </summary>
		public virtual string? Title => null;

		/// <summary>
		/// Gets a few lines that can be shown in a UI tooltip or null to not show anything
		/// </summary>
		public virtual string? Description => null;

		/// <summary>
		/// Raised when the content is changed
		/// </summary>
		public abstract event EventHandler? OnContentChanged;

		/// <summary>
		/// Gets the content
		/// </summary>
		/// <returns></returns>
		public abstract DisassemblyContent GetContent();

		/// <summary>
		/// Called when it's no longer used by a view
		/// </summary>
		public abstract void Dispose();

		/// <summary>
		/// Clones this instance so it can be shown in a new tab
		/// </summary>
		/// <returns></returns>
		public abstract DisassemblyContentProvider Clone();
	}
}
