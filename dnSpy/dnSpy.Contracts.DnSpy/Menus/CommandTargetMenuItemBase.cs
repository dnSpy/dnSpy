/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Command;

namespace dnSpy.Contracts.Menus {
	/// <summary>
	/// A menu item base class for <see cref="ICommandTarget"/> commands
	/// </summary>
	public abstract class CommandTargetMenuItemBase : MenuItemBase {
		readonly Guid group;
		readonly int cmdId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cmdId">Command id</param>
		protected CommandTargetMenuItemBase(StandardIds cmdId)
			: this(CommandConstants.StandardGroup, (int)cmdId) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cmdId">Command id</param>
		protected CommandTargetMenuItemBase(TextEditorIds cmdId)
			: this(CommandConstants.TextEditorGroup, (int)cmdId) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="group">Command group, eg. <see cref="CommandConstants.StandardGroup"/></param>
		/// <param name="cmdId">Command ID</param>
		protected CommandTargetMenuItemBase(Guid group, int cmdId) {
			this.group = group;
			this.cmdId = cmdId;
		}

		/// <summary>
		/// Returns the <see cref="ICommandTarget"/> or null if none
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		protected abstract ICommandTarget? GetCommandTarget(IMenuItemContext context);

		/// <inheritdoc/>
		public override bool IsVisible(IMenuItemContext context) => !(GetCommandTarget(context) is null);

		/// <inheritdoc/>
		public override bool IsEnabled(IMenuItemContext context) => GetCommandTarget(context)?.CanExecute(group, cmdId) == CommandTargetStatus.Handled;

		/// <inheritdoc/>
		public override void Execute(IMenuItemContext context) => GetCommandTarget(context)?.Execute(group, cmdId);
	}

	/// <summary>
	/// A menu item base class for <see cref="ICommandTarget"/> commands
	/// </summary>
	public abstract class CommandTargetMenuItemBase<TContext> : MenuItemBase<TContext> where TContext : class {
		readonly Guid group;
		readonly int cmdId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cmdId">Command id</param>
		protected CommandTargetMenuItemBase(StandardIds cmdId)
			: this(CommandConstants.StandardGroup, (int)cmdId) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cmdId">Command id</param>
		protected CommandTargetMenuItemBase(TextEditorIds cmdId)
			: this(CommandConstants.TextEditorGroup, (int)cmdId) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="group">Command group, eg. <see cref="CommandConstants.StandardGroup"/></param>
		/// <param name="cmdId">Command ID</param>
		protected CommandTargetMenuItemBase(Guid group, int cmdId) {
			this.group = group;
			this.cmdId = cmdId;
		}

		/// <summary>
		/// Returns the <see cref="ICommandTarget"/> or null if none
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		protected abstract ICommandTarget? GetCommandTarget(TContext context);

		/// <inheritdoc/>
		public override bool IsVisible(TContext context) => !(GetCommandTarget(context) is null);

		/// <inheritdoc/>
		public override bool IsEnabled(TContext context) => GetCommandTarget(context)?.CanExecute(group, cmdId) == CommandTargetStatus.Handled;

		/// <inheritdoc/>
		public override void Execute(TContext context) => GetCommandTarget(context)?.Execute(group, cmdId);
	}
}
