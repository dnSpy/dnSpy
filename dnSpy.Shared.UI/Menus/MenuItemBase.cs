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

using dnSpy.Contracts.Menus;

namespace dnSpy.Shared.UI.Menus {
	public abstract class MenuItemBase : IMenuItem, IMenuItem2 {
		public abstract void Execute(IMenuItemContext context);

		public virtual bool IsEnabled(IMenuItemContext context) {
			return true;
		}

		public virtual bool IsVisible(IMenuItemContext context) {
			return true;
		}

		public virtual string GetHeader(IMenuItemContext context) {
			return null;
		}

		public virtual string GetIcon(IMenuItemContext context) {
			return null;
		}

		public virtual string GetInputGestureText(IMenuItemContext context) {
			return null;
		}

		public virtual bool IsChecked(IMenuItemContext context) {
			return false;
		}
	}

	public abstract class MenuItemBase<TContext> : IMenuItem, IMenuItem2 where TContext : class {
		protected abstract TContext CreateContext(IMenuItemContext context);

		protected abstract object CachedContextKey { get; }

		protected TContext GetCachedContext(IMenuItemContext context) {
			var key = CachedContextKey;
			if (key == null)
				return CreateContext(context);

			return context.GetOrCreateState(key, () => CreateContext(context));
		}

		void IMenuItem.Execute(IMenuItemContext context) {
			var ctx = GetCachedContext(context);
			if (ctx != null)
				Execute(ctx);
		}

		bool IMenuItem.IsEnabled(IMenuItemContext context) {
			var ctx = GetCachedContext(context);
			return ctx != null && IsEnabled(ctx);
		}

		bool IMenuItem.IsVisible(IMenuItemContext context) {
			var ctx = GetCachedContext(context);
			return ctx != null && IsVisible(ctx);
		}

		string IMenuItem2.GetHeader(IMenuItemContext context) {
			var ctx = GetCachedContext(context);
			return ctx != null ? GetHeader(ctx) : null;
		}

		string IMenuItem2.GetIcon(IMenuItemContext context) {
			var ctx = GetCachedContext(context);
			return ctx != null ? GetIcon(ctx) : null;
		}

		string IMenuItem2.GetInputGestureText(IMenuItemContext context) {
			var ctx = GetCachedContext(context);
			return ctx != null ? GetInputGestureText(ctx) : null;
		}

		bool IMenuItem2.IsChecked(IMenuItemContext context) {
			var ctx = GetCachedContext(context);
			return ctx != null ? IsChecked(ctx) : false;
		}

		public abstract void Execute(TContext context);

		public virtual bool IsEnabled(TContext context) {
			return true;
		}

		public virtual bool IsVisible(TContext context) {
			return true;
		}

		public virtual string GetHeader(TContext context) {
			return null;
		}

		public virtual string GetIcon(TContext context) {
			return null;
		}

		public virtual string GetInputGestureText(TContext context) {
			return null;
		}

		public virtual bool IsChecked(TContext context) {
			return false;
		}
	}
}
