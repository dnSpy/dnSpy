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

using System.Collections.Generic;

namespace dnSpy.Text.Editor {
	sealed class ReplCommands {
		const int MAX_COMMANDS = 100;
		readonly List<string> commands = new List<string>();
		int firstIndex;
		int? selectedIndex;

		public bool HasCommands => commands.Count > 0;

		public string SelectedCommand {
			get {
				if (!HasCommands)
					return null;
				if (selectedIndex == null)
					return null;
				return commands[selectedIndex.Value];
			}
		}

		int LastCommandIndex {
			get {
				if (!HasCommands)
					return -1;
				return (firstIndex + commands.Count - 1) % commands.Count;
			}
		}

		public bool CanSelectPrevious => HasCommands;

		IEnumerable<(int index, string command)> PreviousCommands {
			get {
				if (!HasCommands)
					yield break;
				if (selectedIndex == null)
					yield return (LastCommandIndex, commands[LastCommandIndex]);
				int index = selectedIndex == null ? LastCommandIndex : selectedIndex.Value;
				while (index != firstIndex) {
					index = (index - 1 + commands.Count) % commands.Count;
					yield return (index, commands[index]);
				}
			}
		}

		IEnumerable<(int index, string command)> NextCommands {
			get {
				if (!HasCommands)
					yield break;
				if (selectedIndex == null)
					yield break;
				int index = selectedIndex.Value;
				int last = LastCommandIndex;
				if (index == last)
					yield break;
				do {
					index = (index + 1) % commands.Count;
					yield return (index, commands[index]);
				} while (last != index);
			}
		}

		public bool SelectPrevious(string text = null) {
			foreach (var t in PreviousCommands) {
				if (string.IsNullOrEmpty(text) || t.command.Contains(text)) {
					selectedIndex = t.index;
					return true;
				}
			}
			return false;
		}

		public bool CanSelectNext => HasCommands && selectedIndex != null;

		public bool SelectNext(string text = null) {
			foreach (var t in NextCommands) {
				if (string.IsNullOrEmpty(text) || t.command.Contains(text)) {
					selectedIndex = t.index;
					return true;
				}
			}
			return false;
		}

		public void Add(string command) {
			if (commands.Count >= MAX_COMMANDS) {
				commands[firstIndex] = command;
				firstIndex = (firstIndex + 1) % commands.Count;
			}
			else
				commands.Add(command);
			selectedIndex = null;
		}
	}
}
