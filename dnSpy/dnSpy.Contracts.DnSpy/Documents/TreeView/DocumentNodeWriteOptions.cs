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

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// Options used when writing <see cref="DocumentTreeNodeData"/> nodes
	/// </summary>
	[Flags]
	public enum DocumentNodeWriteOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None						= 0,

		/// <summary>
		/// The text will be shown in a tab. Less important information such as metadata tokens
		/// should normally not be written.
		/// </summary>
		Title						= 0x00000001,

		/// <summary>
		/// The text will be shown in a tooltip
		/// </summary>
		ToolTip						= 0x00000002,
	}
}
