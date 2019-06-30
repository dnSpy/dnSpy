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
	public readonly struct KeyShortcut : IEquatable<KeyShortcut> {
		public KeyInput KeyInput1 { get; }
		public KeyInput KeyInput2 { get; }

		public bool HasTwoKeyInputs => KeyInput2 != KeyInput.Default;

		public static KeyShortcut Create(Key key) => new KeyShortcut(key, ModifierKeys.None);
		public static KeyShortcut Control(Key key) => new KeyShortcut(key, ModifierKeys.Control);
		public static KeyShortcut Shift(Key key) => new KeyShortcut(key, ModifierKeys.Shift);
		public static KeyShortcut Alt(Key key) => new KeyShortcut(key, ModifierKeys.Alt);
		public static KeyShortcut ShiftAlt(Key key) => new KeyShortcut(key, ModifierKeys.Alt | ModifierKeys.Shift);
		public static KeyShortcut CtrlShift(Key key) => new KeyShortcut(key, ModifierKeys.Control | ModifierKeys.Shift);
		public static KeyShortcut CtrlAlt(Key key) => new KeyShortcut(key, ModifierKeys.Control | ModifierKeys.Alt);
		public static KeyShortcut CtrlShiftAlt(Key key) => new KeyShortcut(key, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt);

		public KeyShortcut(Key key, ModifierKeys modifiers) {
			KeyInput1 = new KeyInput(key, modifiers);
			KeyInput2 = KeyInput.Default;
		}

		public KeyShortcut(KeyInput keyInput1, KeyInput keyInput2) {
			KeyInput1 = keyInput1;
			KeyInput2 = keyInput2;
		}

		public static bool operator ==(KeyShortcut a, KeyShortcut b) => a.Equals(b);
		public static bool operator !=(KeyShortcut a, KeyShortcut b) => !a.Equals(b);
		public bool Equals(KeyShortcut other) => KeyInput1 == other.KeyInput1 && KeyInput2 == other.KeyInput2;
		public override bool Equals(object? obj) => obj is KeyShortcut && Equals((KeyShortcut)obj);
		public override int GetHashCode() => KeyInput1.GetHashCode() ^ KeyInput2.GetHashCode();
		public override string ToString() => HasTwoKeyInputs ? $"{KeyInput1}, {KeyInput2}" : KeyInput1.ToString();
	}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
