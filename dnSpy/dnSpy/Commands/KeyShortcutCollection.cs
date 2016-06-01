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
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Command;

namespace dnSpy.Commands {
	sealed class KeyShortcutCollection {
		readonly HashSet<KeyInput> twoKeyCombos;
		readonly Dictionary<KeyShortcut, List<CreatorAndCommand>> dict;

		public KeyShortcutCollection() {
			this.twoKeyCombos = new HashSet<KeyInput>();
			this.dict = new Dictionary<KeyShortcut, List<CreatorAndCommand>>();
		}

		public void Add(ICommandInfoCreator creator, object target) {
			foreach (var t in creator.GetCommandShortcuts(target)) {
				List<CreatorAndCommand> list;
				if (!dict.TryGetValue(t.KeyShortcut, out list))
					dict.Add(t.KeyShortcut, list = new List<CreatorAndCommand>());
				list.Add(new CreatorAndCommand(creator, t.CommandInfo));
				if (t.KeyShortcut.HasTwoKeyInputs)
					twoKeyCombos.Add(t.KeyShortcut.KeyInput1);
			}
		}

		public IEnumerable<CreatorAndCommand> GetTwoKeyShortcuts(KeyShortcut keyShortcut) {
			Debug.Assert(keyShortcut.HasTwoKeyInputs);
			List<CreatorAndCommand> list;
			if (dict.TryGetValue(keyShortcut, out list))
				return list;
			return Array.Empty<CreatorAndCommand>();
		}

		public bool IsTwoKeyCombo(KeyInput keyInput) => twoKeyCombos.Contains(keyInput);

		public IEnumerable<CreatorAndCommand> GetOneKeyShortcuts(KeyInput keyInput) {
			var keyShortcut = new KeyShortcut(keyInput, KeyInput.Default);
			List<CreatorAndCommand> list;
			if (dict.TryGetValue(keyShortcut, out list))
				return list;
			return Array.Empty<CreatorAndCommand>();
		}
	}
}
