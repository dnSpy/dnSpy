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

namespace dnSpy.MVVM {
	abstract class ContextMenuEntryBase<TContext> : IContextMenuEntry2 {
		protected abstract TContext CreateContext(ContextMenuEntryContext context);

		void IContextMenuEntry<ContextMenuEntryContext>.Execute(ContextMenuEntryContext context) {
			var ctx = CreateContext(context);
			if (ctx != null)
				Execute(ctx);
		}

		void IContextMenuEntry2<ContextMenuEntryContext>.Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
			var ctx = CreateContext(context);
			if (ctx != null)
				Initialize(ctx, menuItem);
		}

		bool IContextMenuEntry<ContextMenuEntryContext>.IsEnabled(ContextMenuEntryContext context) {
			var ctx = CreateContext(context);
			return ctx != null && IsEnabled(ctx);
		}

		bool IContextMenuEntry<ContextMenuEntryContext>.IsVisible(ContextMenuEntryContext context) {
			var ctx = CreateContext(context);
			return ctx != null && IsVisible(ctx);
		}

		protected abstract void Execute(TContext context);

		protected virtual void Initialize(TContext context, MenuItem menuItem) {
		}

		protected virtual bool IsEnabled(TContext context) {
			return true;
		}

		protected virtual bool IsVisible(TContext context) {
			return true;
		}
	}
}
