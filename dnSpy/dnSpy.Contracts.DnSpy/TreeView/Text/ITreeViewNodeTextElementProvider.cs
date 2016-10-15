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

using System.Windows;

namespace dnSpy.Contracts.TreeView.Text {
	/// <summary>
	/// Creates WPF text elements for treeview nodes
	/// </summary>
	public interface ITreeViewNodeTextElementProvider {
		/// <summary>
		/// Creates a WPF text element
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="contentType">Treeview node content type, eg. <see cref="TreeViewContentTypes.TreeViewNodeAssemblyExplorer"/></param>
		/// <param name="filterOutNewLines">true to filter out newlines from the string</param>
		/// <param name="useNewFormatter">true to use the new formatter, it doesn't support all characters (eg. chinese chars) or word wrap</param>
		/// <returns></returns>
		FrameworkElement CreateTextElement(TreeViewNodeClassifierContext context, string contentType, bool filterOutNewLines, bool useNewFormatter);
	}
}
