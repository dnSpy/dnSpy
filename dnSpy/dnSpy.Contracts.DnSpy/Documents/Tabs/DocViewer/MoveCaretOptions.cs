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

namespace dnSpy.Contracts.Documents.Tabs.DocViewer {
	/// <summary>
	/// Move caret options
	/// </summary>
	[Flags]
	public enum MoveCaretOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None				= 0,

		/// <summary>
		/// Select the span
		/// </summary>
		Select				= 0x00000001,

		/// <summary>
		/// Give the text viewer focus
		/// </summary>
		Focus				= 0x00000002,

		/// <summary>
		/// Always center the caret in the view
		/// </summary>
		Center				= 0x00000004,
	}
}
