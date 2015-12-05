/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using System.ComponentModel;

namespace dnSpy.Contracts.App {
	/// <summary>
	/// Application settings
	/// </summary>
	public interface IAppSettings : INotifyPropertyChanged {
		/// <summary>
		/// Text Editor: true to use the new optimized renderer. It doesn't support all unicode chars or word wrapping
		/// </summary>
		bool UseNewRenderer_TextEditor { get; }

		/// <summary>
		/// Hex Editor: true to use the new optimized renderer. It doesn't support all unicode chars or word wrapping
		/// </summary>
		bool UseNewRenderer_HexEditor { get; }

		/// <summary>
		/// File TreeView: true to use the new optimized renderer. It doesn't support all unicode chars or word wrapping
		/// </summary>
		bool UseNewRenderer_FileTreeView { get; }
	}
}
