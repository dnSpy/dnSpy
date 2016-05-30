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

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Command;

namespace dnSpy.Commands {
	sealed class RegisteredCommandElement : IRegisteredCommandElement {
		public ICommandTarget CommandTarget { get; }
		readonly CommandManager commandManager;
		WeakReference weakSourceElement;
		WeakReference weakOwner;

		sealed class MyCommandTarget : ICommandTarget {
			RegisteredCommandElement registeredCommandElement;

			public double Order => double.NegativeInfinity;

			public MyCommandTarget(RegisteredCommandElement registeredCommandElement) {
				this.registeredCommandElement = registeredCommandElement;
			}

			public CommandTargetStatus CanExecute(Guid group, int cmdId) {
				if (registeredCommandElement?.TryGetOwnerOrUnregister() == null) {
					registeredCommandElement = null;
					return CommandTargetStatus.NotHandled;
				}
				return registeredCommandElement.commandManager.CanExecute(group, cmdId);
			}

			public CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) {
				if (registeredCommandElement?.TryGetOwnerOrUnregister() == null) {
					registeredCommandElement = null;
					return CommandTargetStatus.NotHandled;
				}
				return registeredCommandElement.commandManager.Execute(group, cmdId, args, ref result);
			}
		}

		public RegisteredCommandElement(CommandManager commandManager, UIElement sourceElement, object owner) {
			if (commandManager == null)
				throw new ArgumentNullException(nameof(commandManager));
			if (sourceElement == null)
				throw new ArgumentNullException(nameof(sourceElement));
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			this.commandManager = commandManager;
			this.weakSourceElement = new WeakReference(sourceElement);
			this.weakOwner = new WeakReference(owner);
			CommandTarget = new MyCommandTarget(this);
			sourceElement.PreviewKeyDown += SourceElement_PreviewKeyDown;
		}

		UIElement TryGetSourceElement() => weakSourceElement.Target as UIElement;
		object TryGetOwner() => weakOwner.Target as object;

		object TryGetOwnerOrUnregister() {
			var owner = TryGetOwner();
			if (owner != null)
				return owner;

			Unregister();
			return null;
		}

		void SourceElement_PreviewKeyDown(object sender, KeyEventArgs e) {
			var owner = TryGetOwnerOrUnregister();
			if (owner == null)
				return;
			var cmd = commandManager.GetCommand(e, owner);
			if (cmd == null)
				return;
			if (CommandTarget.CanExecute(cmd.Value.Group, cmd.Value.ID) != CommandTargetStatus.Handled)
				return;

			object result = null;
			var res = CommandTarget.Execute(cmd.Value.Group, cmd.Value.ID, cmd.Value.Arguments, ref result);
			Debug.Assert(res == CommandTargetStatus.Handled);
			if (res == CommandTargetStatus.Handled)
				e.Handled = true;
		}

		public void Unregister() {
			var sourceElement = TryGetSourceElement();
			if (sourceElement != null)
				sourceElement.PreviewKeyDown -= SourceElement_PreviewKeyDown;
			weakSourceElement = new WeakReference(null);
			weakOwner = new WeakReference(null);
		}
	}
}
