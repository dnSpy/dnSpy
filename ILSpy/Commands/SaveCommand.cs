// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Windows.Input;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy
{
	[ExportMainMenuCommand(Menu = "_File", MenuHeader = "_Save Code…", MenuIcon = "Save", MenuCategory = "Save", MenuOrder = 1000)]
	[ExportContextMenuEntry(Header = "Save Code…", Icon = "Save", Category = "Save", InputGestureText = "Ctrl+S", Order = 250)]
	sealed class SaveCommand : CommandWrapper, IContextMenuEntry2
	{
		public SaveCommand()
			: base(ApplicationCommands.Save)
		{
		}

		/// <remarks>Copied from SaveModuleCommand.cs, move to helper/util class?</remarks>
		HashSet<LoadedAssembly> GetAssemblyNodes(SharpTreeNode[] nodes)
		{
			var hash = new HashSet<LoadedAssembly>();
			foreach (var node in nodes) {
				var asmNode = ILSpyTreeNode.GetNode<AssemblyTreeNode>(node);
				if (asmNode != null && asmNode.LoadedAssembly.ModuleDefinition != null)
					hash.Add(asmNode.LoadedAssembly);
			}
			return hash;
		}

		public void Initialize(TextViewContext context, System.Windows.Controls.MenuItem menuItem)
		{
		}

		public void Execute(TextViewContext context)
		{
			base.Execute(null);
		}

		public bool IsEnabled(TextViewContext context)
		{
			return GetAssemblyNodes(context.SelectedTreeNodes).Count == 1;
		}

		public bool IsVisible(TextViewContext context)
		{
			return context.TreeView != null;
		}
	}
}
