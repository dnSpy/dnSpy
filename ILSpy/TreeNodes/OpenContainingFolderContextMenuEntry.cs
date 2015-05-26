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
using System.Diagnostics;
using System.IO;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.TreeNodes
{
	[ExportContextMenuEntryAttribute(Header = "_Open Containing Folder", Order = 920, Category = "Other")]
	class OpenContainingFolderContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.SelectedTreeNodes != null &&
				context.SelectedTreeNodes.Length == 1 &&
				context.SelectedTreeNodes[0] is AssemblyTreeNode &&
				!string.IsNullOrWhiteSpace(((AssemblyTreeNode)context.SelectedTreeNodes[0]).LoadedAssembly.FileName);
		}

		public bool IsEnabled(TextViewContext context)
		{
			return IsVisible(context);
		}

		public void Execute(TextViewContext context)
		{
			// Known problem: explorer can't show files in the .NET 2.0 GAC.
			var asmNode = (AssemblyTreeNode)context.SelectedTreeNodes[0];
			var filename = asmNode.LoadedAssembly.FileName;
			var args = string.Format("/select,{0}", filename);
			try {
				Process.Start(new ProcessStartInfo("explorer.exe", args));
			}
			catch (IOException) {
			}
			catch (Win32Exception) {
			}
		}
	}
}
