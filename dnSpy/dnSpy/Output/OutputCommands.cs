/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Output {
	static class OutputCommands {
		public static readonly RoutedCommand CopyCommand = new RoutedCommand("CopyCommand", typeof(OutputCommands));
		public static readonly RoutedCommand ClearAllCommand = new RoutedCommand("ClearAllCommand", typeof(OutputCommands));
		public static readonly RoutedCommand ToggleWordWrapCommand = new RoutedCommand("ToggleWordWrapCommand", typeof(OutputCommands));
		public static readonly RoutedCommand ToggleShowLineNumbersCommand = new RoutedCommand("ToggleShowLineNumbersCommand", typeof(OutputCommands));
		public static readonly RoutedCommand ToggleShowTimestampsCommand = new RoutedCommand("ToggleShowTimestampsCommand", typeof(OutputCommands));
		public static readonly RoutedCommand[] SelectLogWindowCommands;

		static OutputCommands() {
			SelectLogWindowCommands = new RoutedCommand[10];
			for (int i = 0; i < SelectLogWindowCommands.Length; i++) {
				int num = i + 1;
				var cmd = new RoutedCommand("SelectLogWindowCommand" + num, typeof(OutputCommands));
				SelectLogWindowCommands[i] = cmd;
				if (i < 10)
					cmd.InputGestures.Add(new KeyGesture(Key.D0 + num % 10, ModifierKeys.Control));
			}
		}
	}
}
