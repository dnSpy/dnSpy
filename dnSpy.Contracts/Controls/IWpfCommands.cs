/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

namespace dnSpy.Contracts.Controls {
	/// <summary>
	/// Stores commands and bindings added to a control
	/// </summary>
	public interface IWpfCommands {
		/// <summary>
		/// Gets the guid
		/// </summary>
		Guid Guid { get; }

		/// <summary>
		/// Adds a key binding
		/// </summary>
		/// <param name="command">Gets called when the key combination is pressed</param>
		/// <param name="modifiers">Modifiers</param>
		/// <param name="key">Key</param>
		void Add(ICommand command, ModifierKeys modifiers, Key key);

		/// <summary>
		/// Adds a command and key binding
		/// </summary>
		/// <param name="command">The routed command</param>
		/// <param name="realCommand">The real command that will handle <paramref name="command"/> events</param>
		/// <param name="modifiers">Modifiers</param>
		/// <param name="key">Key</param>
		void Add(RoutedCommand command, ICommand realCommand, ModifierKeys modifiers = ModifierKeys.None, Key key = Key.None);

		/// <summary>
		/// Adds a command and key binding(s)
		/// </summary>
		/// <param name="command">The routed command</param>
		/// <param name="exec">Executes the command</param>
		/// <param name="canExec">Can-execute handler</param>
		/// <param name="modifiers1">Modifiers</param>
		/// <param name="key1">Key</param>
		/// <param name="modifiers2">Modifiers</param>
		/// <param name="key2">Key</param>
		/// <param name="modifiers3">Modifiers</param>
		/// <param name="key3">Key</param>
		void Add(RoutedCommand command, ExecutedRoutedEventHandler exec, CanExecuteRoutedEventHandler canExec, ModifierKeys modifiers1 = ModifierKeys.None, Key key1 = Key.None, ModifierKeys modifiers2 = ModifierKeys.None, Key key2 = Key.None, ModifierKeys modifiers3 = ModifierKeys.None, Key key3 = Key.None);
	}
}
