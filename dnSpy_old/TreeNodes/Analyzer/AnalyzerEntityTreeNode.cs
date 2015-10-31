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

using System.Collections.Generic;
using System.Windows.Input;
using ICSharpCode.TreeView;
using dnlib.DotNet;
using dnSpy.Files;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer {
	/// <summary>
	/// Base class for entity nodes.
	/// </summary>
	public abstract class AnalyzerEntityTreeNode : AnalyzerTreeNode, IMemberTreeNode {
		public abstract IMemberRef Member { get; }
		public abstract IMDTokenProvider MDTokenProvider { get; }

		public override void ActivateItem(System.Windows.RoutedEventArgs e) {
			e.Handled = true;
			if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift)
				MainWindow.Instance.OpenNewEmptyTab();
			MainWindow.Instance.JumpToReference(this.Member);
		}

		public override bool HandleAssemblyListChanged(ICollection<DnSpyFile> removedAssemblies, ICollection<DnSpyFile> addedAssemblies) {
			foreach (DnSpyFile asm in removedAssemblies) {
				if (this.Member.Module == asm.ModuleDef)
					return false; // remove this node
			}
			this.Children.RemoveAll(
				delegate (SharpTreeNode n) {
					AnalyzerTreeNode an = n as AnalyzerTreeNode;
					return an == null || !an.HandleAssemblyListChanged(removedAssemblies, addedAssemblies);
				});
			return true;
		}

		public override bool HandleModelUpdated(DnSpyFile asm) {
			if (this.Member.Module == null)
				return false; // remove this node
			if ((this.Member is IField || this.Member is IMethod || this.Member is PropertyDef || this.Member is EventDef) &&
				this.Member.DeclaringType == null)
				return false;
			this.Children.RemoveAll(
				delegate (SharpTreeNode n) {
					AnalyzerTreeNode an = n as AnalyzerTreeNode;
					return an == null || !an.HandleModelUpdated(asm);
				});
			return true;
		}
	}
}
