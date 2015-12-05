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

using System;
using dnSpy.Contracts.Menus;
using dnSpy.Shared.UI.Menus;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor {
	sealed class CodeContext {
		public readonly ILSpyTreeNode[] Nodes;
		public readonly bool IsLocalTarget;

		public CodeContext(ILSpyTreeNode[] nodes, bool isLocalTarget) {
			this.Nodes = nodes ?? new ILSpyTreeNode[0];
			this.IsLocalTarget = isLocalTarget;
		}
	}

	abstract class CodeContextMenuHandler : MenuItemBase<CodeContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		public sealed override bool IsVisible(CodeContext context) {
			return IsEnabled(context);
		}

		protected sealed override CodeContext CreateContext(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID))
				return null;
			var refSeg = context.FindByType<CodeReferenceSegment>();
			if (refSeg == null)
				return null;
			var node = MainWindow.Instance.FindTreeNode(refSeg.Reference);
			var nodes = node == null ? new ILSpyTreeNode[0] : new ILSpyTreeNode[] { node };
			return new CodeContext(nodes, refSeg.IsLocalTarget);
		}
	}
}
