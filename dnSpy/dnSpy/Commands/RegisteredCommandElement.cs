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
using System.Linq;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Command;

namespace dnSpy.Commands {
	sealed class RegisteredCommandElement : IRegisteredCommandElement {
		public ICommandTarget CommandTarget { get; }
		readonly CommandManager commandManager;
		readonly KeyShortcutCollection keyShortcutCollection;
		readonly ICommandTargetFilter[] commandTargets;
		WeakReference weakSourceElement;
		WeakReference weakTarget;

		sealed class MyCommandTarget : ICommandTarget {
			RegisteredCommandElement registeredCommandElement;

			public MyCommandTarget(RegisteredCommandElement registeredCommandElement) {
				this.registeredCommandElement = registeredCommandElement;
			}

			public CommandTargetStatus CanExecute(Guid group, int cmdId) {
				var target = registeredCommandElement?.TryGetTargetOrUnregister();
				if (target == null) {
					registeredCommandElement = null;
					return CommandTargetStatus.NotHandled;
				}
				return registeredCommandElement.CanExecute(target, group, cmdId);
			}

			public CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) {
				var target = registeredCommandElement?.TryGetTargetOrUnregister();
				if (target == null) {
					registeredCommandElement = null;
					return CommandTargetStatus.NotHandled;
				}
				return registeredCommandElement.Execute(target, group, cmdId, args, ref result);
			}
		}

		public RegisteredCommandElement(CommandManager commandManager, UIElement sourceElement, KeyShortcutCollection keyShortcutCollection, ICommandTargetFilter[] commandTargets, object target) {
			if (commandManager == null)
				throw new ArgumentNullException(nameof(commandManager));
			if (sourceElement == null)
				throw new ArgumentNullException(nameof(sourceElement));
			if (keyShortcutCollection == null)
				throw new ArgumentNullException(nameof(keyShortcutCollection));
			if (commandTargets == null)
				throw new ArgumentNullException(nameof(commandTargets));
			if (target == null)
				throw new ArgumentNullException(nameof(target));
			this.commandManager = commandManager;
			this.weakSourceElement = new WeakReference(sourceElement);
			this.weakTarget = new WeakReference(target);
			this.keyShortcutCollection = keyShortcutCollection;
			this.commandTargets = commandTargets;
			CommandTarget = new MyCommandTarget(this);
			sourceElement.PreviewKeyDown += SourceElement_PreviewKeyDown;
			sourceElement.PreviewTextInput += SourceElement_PreviewTextInput;
		}

		UIElement TryGetSourceElement() => weakSourceElement.Target as UIElement;
		object TryGetTarget() => weakTarget.Target as object;

		object TryGetTargetOrUnregister() {
			var target = TryGetTarget();
			if (target != null)
				return target;

			Unregister();
			return null;
		}

		CommandInfo GetCommand(KeyEventArgs e, object target, out bool waitForSecondKey) {
			var keyInput = new KeyInput(e);
			CreatorAndCommand result;
			if (prevKey != null) {
				waitForSecondKey = false;
				var keyShortcut = new KeyShortcut(prevKey.Value, keyInput);
				result = keyShortcutCollection.GetTwoKeyShortcuts(keyShortcut).FirstOrDefault(a => a.Creator.IsValid(target, keyShortcut));
				prevKey = null;
			}
			else {
				if (keyShortcutCollection.IsTwoKeyCombo(keyInput)) {
					waitForSecondKey = true;
					prevKey = keyInput;
					result = default(CreatorAndCommand);
				}
				else {
					waitForSecondKey = false;
					var keyShortcut = new KeyShortcut(keyInput, KeyInput.Default);
					result = keyShortcutCollection.GetOneKeyShortcuts(keyInput).FirstOrDefault(a => a.Creator.IsValid(target, keyShortcut));
				}
			}
			if (result.IsDefault)
				return DefaultIds.Unknown.ToCommandInfo();
			return result.Command;
		}
		KeyInput? prevKey;

		void SourceElement_PreviewKeyDown(object sender, KeyEventArgs e) {
			var target = TryGetTargetOrUnregister();
			if (target == null)
				return;
			bool waitForSecondKey;
			var cmd = GetCommand(e, target, out waitForSecondKey);
			if (waitForSecondKey) {
				e.Handled = true;
				return;
			}
			ExecuteCommand(cmd, e);
		}

		void ExecuteCommand(CommandInfo cmd, RoutedEventArgs e) {
			if (CommandTarget.CanExecute(cmd.Group, cmd.ID) != CommandTargetStatus.Handled)
				return;

			object result = null;
			var res = CommandTarget.Execute(cmd.Group, cmd.ID, cmd.Arguments, ref result);
			Debug.Assert(res == CommandTargetStatus.Handled);
			if (res == CommandTargetStatus.Handled)
				e.Handled = true;
		}

		void SourceElement_PreviewTextInput(object sender, TextCompositionEventArgs e) {
			Debug.Assert(prevKey == null);
			prevKey = null;
			var target = TryGetTargetOrUnregister();
			if (target == null)
				return;

			var cmd = commandManager.CreateCommandInfo(target, e.Text);
			if (cmd == null)
				return;

			ExecuteCommand(cmd.Value, e);
		}

		CommandTargetStatus CanExecute(object target, Guid group, int cmdId) {
			foreach (var ct in commandTargets) {
				var res = ct.CanExecute(target, group, cmdId);
				if (res == CommandTargetStatus.Handled)
					return res;
				Debug.Assert(res == CommandTargetStatus.NotHandled);
			}
			return CommandTargetStatus.NotHandled;
		}

		CommandTargetStatus Execute(object target, Guid group, int cmdId, object args, ref object result) {
			foreach (var ct in commandTargets) {
				result = null;
				var res = ct.Execute(target, group, cmdId, args, ref result);
				if (res == CommandTargetStatus.Handled)
					return res;
				Debug.Assert(res == CommandTargetStatus.NotHandled);
			}
			result = null;
			return CommandTargetStatus.NotHandled;
		}

		public void Unregister() {
			var sourceElement = TryGetSourceElement();
			if (sourceElement != null) {
				sourceElement.PreviewKeyDown -= SourceElement_PreviewKeyDown;
				sourceElement.PreviewTextInput -= SourceElement_PreviewTextInput;
			}
			weakSourceElement = new WeakReference(null);
			weakTarget = new WeakReference(null);
			foreach (var c in commandTargets)
				c.Dispose();
		}
	}
}
