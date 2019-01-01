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
using System.Windows.Input;

namespace dnSpy.Contracts.Command {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public readonly struct KeyInput : IEquatable<KeyInput> {
		public static readonly KeyInput Default = new KeyInput(Key.None, ModifierKeys.None);

		public Key Key { get; }
		public ModifierKeys Modifiers { get; }

		public static KeyInput Create(Key key) => new KeyInput(key, ModifierKeys.None);
		public static KeyInput Control(Key key) => new KeyInput(key, ModifierKeys.Control);
		public static KeyInput Shift(Key key) => new KeyInput(key, ModifierKeys.Shift);
		public static KeyInput Alt(Key key) => new KeyInput(key, ModifierKeys.Alt);
		public static KeyInput ShiftAlt(Key key) => new KeyInput(key, ModifierKeys.Alt | ModifierKeys.Shift);
		public static KeyInput CtrlShift(Key key) => new KeyInput(key, ModifierKeys.Control | ModifierKeys.Shift);
		public static KeyInput CtrlAlt(Key key) => new KeyInput(key, ModifierKeys.Control | ModifierKeys.Alt);
		public static KeyInput CtrlShiftAlt(Key key) => new KeyInput(key, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt);

		public KeyInput(Key key, ModifierKeys modifiers) {
			Key = key;
			Modifiers = modifiers;
		}

		public KeyInput(KeyEventArgs e) {
			Key = e.Key == Key.System ? e.SystemKey : e.Key;
			Modifiers = e.KeyboardDevice.Modifiers;
		}

		public static bool operator ==(KeyInput a, KeyInput b) => a.Equals(b);
		public static bool operator !=(KeyInput a, KeyInput b) => !a.Equals(b);
		public bool Equals(KeyInput other) => Key == other.Key && Modifiers == other.Modifiers;
		public override bool Equals(object obj) => obj is KeyInput && Equals((KeyInput)obj);
		public override int GetHashCode() => ((int)Key).GetHashCode() ^ ((int)Modifiers).GetHashCode();
		public override string ToString() => Modifiers == ModifierKeys.None ? GetKeyName() : $"{GetModifiers()}+{GetKeyName()}";

		string GetModifiers() {
			switch (Modifiers) {
			case ModifierKeys.None: return string.Empty;
			case ModifierKeys.Control: return "Ctrl";
			case ModifierKeys.Shift: return "Shift";
			case ModifierKeys.Alt: return "Alt";
			case ModifierKeys.Control | ModifierKeys.Shift: return "Ctrl+Shift";
			case ModifierKeys.Control | ModifierKeys.Alt: return "Ctrl+Alt";
			case ModifierKeys.Shift | ModifierKeys.Alt: return "Shift+Alt";
			case ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt: return "Ctrl+Shift+Alt";
			default: return "???";
			}
		}

		string GetKeyName() {
			if (Key == Key.PageDown)
				return nameof(Key.PageDown);
			if (Key == Key.PageUp)
				return nameof(Key.PageUp);
			return Key.ToString();
		}
	}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
