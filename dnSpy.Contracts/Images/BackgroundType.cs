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

namespace dnSpy.Contracts.Images {
	/// <summary>
	/// Image background
	/// </summary>
	public enum BackgroundType {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		// ********************************************************************************
		// ONLY APPEND VALUES. DON'T REMOVE SORT OR RENAME ANYTHING. (unless you can recompile
		// EVERYTHING that depend on these values)
		// ********************************************************************************
		Button,
		TextEditor,
		DialogWindow,
		TextBox,
		TreeNode,
		Search,
		ComboBox,
		ToolBar,
		AppMenuMenuItem,
		ContextMenuItem,
		GridViewItem,
		CodeToolTip,
		TitleAreaActive,
		TitleAreaInactive,
		CommandBar,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
