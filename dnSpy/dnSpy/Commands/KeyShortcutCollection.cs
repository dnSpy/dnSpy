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
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Command;

namespace dnSpy.Commands {
	sealed class KeyShortcutCollection {
		readonly HashSet<KeyInput> twoKeyCombos;
		readonly Dictionary<KeyShortcut, List<ProviderAndCommand>> dict;

		public KeyShortcutCollection() {
			twoKeyCombos = new HashSet<KeyInput>();
			dict = new Dictionary<KeyShortcut, List<ProviderAndCommand>>();
		}

		public void Add(ICommandInfoProvider provider, object target) {
			foreach (var t in provider.GetCommandShortcuts(target)) {
				if (!dict.TryGetValue(t.KeyShortcut, out var list))
					dict.Add(t.KeyShortcut, list = new List<ProviderAndCommand>());
				list.Add(new ProviderAndCommand(provider, t.CommandInfo));
				if (t.KeyShortcut.HasTwoKeyInputs)
					twoKeyCombos.Add(t.KeyShortcut.KeyInput1);
			}
		}

		public IEnumerable<ProviderAndCommand> GetTwoKeyShortcuts(KeyShortcut keyShortcut) {
			Debug.Assert(keyShortcut.HasTwoKeyInputs);
			if (dict.TryGetValue(keyShortcut, out var list))
				return list;
			return Array.Empty<ProviderAndCommand>();
		}

		public bool IsTwoKeyCombo(KeyInput keyInput) => twoKeyCombos.Contains(keyInput);

		public IEnumerable<ProviderAndCommand> GetOneKeyShortcuts(KeyInput keyInput) {
			var keyShortcut = new KeyShortcut(keyInput, KeyInput.Default);
			if (dict.TryGetValue(keyShortcut, out var list))
				return list;
			return Array.Empty<ProviderAndCommand>();
		}
	}
}
