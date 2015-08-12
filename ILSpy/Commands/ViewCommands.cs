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

using System.Windows.Data;
using System.Windows.Input;
using ICSharpCode.ILSpy;

namespace dnSpy.Commands {
	[ExportMainMenuCommand(Menu = "_View", MenuCategory = "View1", MenuHeader = "Show _Internal Types and Members", MenuOrder = 3000, MenuIcon = "PrivateInternal")]
	sealed class ShowInternalTypesAndMembersCommand : ICommand, IMainMenuCheckableCommand {
		public bool? IsChecked {
			get { return false; }
		}

		public Binding Binding {
			get {
				return new Binding("FilterSettings.ShowInternalApi") {
					Source = MainWindow.Instance.SessionSettings,
				};
			}
		}

		public bool CanExecute(object parameter) {
			return true;
		}

		public event System.EventHandler CanExecuteChanged {
			add { }
			remove { }
		}

		public void Execute(object parameter) {
		}
	}

	[ExportMainMenuCommand(Menu = "_View", MenuCategory = "View2", MenuHeader = "_Word Wrap", MenuOrder = 3100, MenuIcon = "WordWrap", MenuInputGestureText = "Ctrl+Alt+W")]
	sealed class WordWrapCommand : ICommand, IMainMenuCheckableCommand {
		public bool? IsChecked {
			get { return false; }
		}

		public Binding Binding {
			get {
				return new Binding("WordWrap") {
					Source = MainWindow.Instance.SessionSettings,
				};
			}
		}

		public bool CanExecute(object parameter) {
			return true;
		}

		public event System.EventHandler CanExecuteChanged {
			add { }
			remove { }
		}

		public void Execute(object parameter) {
		}
	}

	[ExportMainMenuCommand(Menu = "_View", MenuCategory = "View2", MenuHeader = "_Highlight Current Line", MenuOrder = 3110)]
	sealed class HighlightCurrentLineCommand : ICommand, IMainMenuCheckableCommand {
		public bool? IsChecked {
			get { return false; }
		}

		public Binding Binding {
			get {
				return new Binding("HighlightCurrentLine") {
					Source = MainWindow.Instance.SessionSettings,
				};
			}
		}

		public bool CanExecute(object parameter) {
			return true;
		}

		public event System.EventHandler CanExecuteChanged {
			add { }
			remove { }
		}

		public void Execute(object parameter) {
		}
	}

	[ExportMainMenuCommand(Menu = "_View", MenuCategory = "View2", MenuHeader = "F_ull Screen", MenuOrder = 3120, MenuInputGestureText = "Shift+Alt+Enter", MenuIcon = "FullScreen")]
	sealed class FullScreenCommand : ICommand, IMainMenuCheckableCommand {
		public bool? IsChecked {
			get { return MainWindow.Instance.IsFullScreen; }
		}

		public Binding Binding {
			get {
				return new Binding("IsFullScreen") {
					Source = MainWindow.Instance,
				};
			}
		}

		public bool CanExecute(object parameter) {
			return true;
		}

		public event System.EventHandler CanExecuteChanged {
			add { }
			remove { }
		}

		public void Execute(object parameter) {
		}
	}
}
