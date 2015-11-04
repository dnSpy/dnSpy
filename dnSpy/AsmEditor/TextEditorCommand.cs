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

using System.Windows.Controls;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor {
	public abstract class TextEditorCommand : IContextMenuEntry2 {
		protected struct Context {
			public ReferenceSegment ReferenceSegment;
			public ILSpyTreeNode[] Nodes;

			public Context(ReferenceSegment regSeg) {
				this.ReferenceSegment = regSeg;
				var node = MainWindow.Instance.FindTreeNode(regSeg.Reference);
				this.Nodes = node == null ? new ILSpyTreeNode[0] : new ILSpyTreeNode[] { node };
			}
		}

		static Context? CreateContext(ContextMenuEntryContext context) {
			if (!(context.Element is DecompilerTextView) || context.Reference == null)
				return null;

			return new Context(context.Reference);
		}

		protected virtual bool IsVisible(Context ctx) {
			return CanExecute(ctx);
		}

		protected virtual bool IsEnabled(Context ctx) {
			return CanExecute(ctx);
		}

		protected abstract bool CanExecute(Context ctx);
		protected abstract void Execute(Context ctx);

		protected virtual void Initialize(Context ctx, MenuItem menuItem) {
		}

		bool IContextMenuEntry<ContextMenuEntryContext>.IsVisible(ContextMenuEntryContext context) {
			var ctx = CreateContext(context);
			return ctx != null && IsVisible(ctx.Value);
		}

		bool IContextMenuEntry<ContextMenuEntryContext>.IsEnabled(ContextMenuEntryContext context) {
			var ctx = CreateContext(context);
			return ctx != null && IsEnabled(ctx.Value);
		}

		void IContextMenuEntry<ContextMenuEntryContext>.Execute(ContextMenuEntryContext context) {
			var ctx = CreateContext(context);
			if (ctx != null && CanExecute(ctx.Value))
				Execute(ctx.Value);
		}

		void IContextMenuEntry2<ContextMenuEntryContext>.Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
			var ctx = CreateContext(context);
			if (ctx != null)
				Initialize(ctx.Value, menuItem);
		}
	}
}
