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

using System.Windows.Input;

namespace dnSpy.Contracts.Command {
	/// <summary>
	/// Keyboard shortcut and command
	/// </summary>
	public readonly struct CommandShortcut {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public KeyShortcut KeyShortcut { get; }
		public CommandInfo CommandInfo { get; }

		public static CommandShortcut Create(Key key, CommandInfo cmd) => new CommandShortcut(new KeyShortcut(key, ModifierKeys.None), cmd);
		public static CommandShortcut Control(Key key, CommandInfo cmd) => new CommandShortcut(new KeyShortcut(key, ModifierKeys.Control), cmd);
		public static CommandShortcut Shift(Key key, CommandInfo cmd) => new CommandShortcut(new KeyShortcut(key, ModifierKeys.Shift), cmd);
		public static CommandShortcut Alt(Key key, CommandInfo cmd) => new CommandShortcut(new KeyShortcut(key, ModifierKeys.Alt), cmd);
		public static CommandShortcut ShiftAlt(Key key, CommandInfo cmd) => new CommandShortcut(new KeyShortcut(key, ModifierKeys.Alt | ModifierKeys.Shift), cmd);
		public static CommandShortcut CtrlShift(Key key, CommandInfo cmd) => new CommandShortcut(new KeyShortcut(key, ModifierKeys.Control | ModifierKeys.Shift), cmd);
		public static CommandShortcut CtrlAlt(Key key, CommandInfo cmd) => new CommandShortcut(new KeyShortcut(key, ModifierKeys.Control | ModifierKeys.Alt), cmd);
		public static CommandShortcut CtrlShiftAlt(Key key, CommandInfo cmd) => new CommandShortcut(new KeyShortcut(key, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt), cmd);
		public static CommandShortcut Create(KeyInput key1, KeyInput key2, CommandInfo cmd) => new CommandShortcut(new KeyShortcut(key1, key2), cmd);

		public CommandShortcut(KeyShortcut shortcut, CommandInfo cmd) {
			KeyShortcut = shortcut;
			CommandInfo = cmd;
		}
	}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
