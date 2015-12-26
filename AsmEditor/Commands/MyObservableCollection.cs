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

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.AsmEditor.Commands {
	sealed class MyObservableCollection<T> : ObservableCollection<T> {
		void OnPropertyChanged(string propName) {
			OnPropertyChanged(new PropertyChangedEventArgs(propName));
		}

		public bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					isEnabled = value;
					OnPropertyChanged("IsEnabled");
				}
			}
		}
		bool isEnabled = true;

		public int SelectedIndex {
			get { return selectedIndex; }
			set {
				if (selectedIndex != value) {
					selectedIndex = value;
					OnPropertyChanged("SelectedIndex");
				}
			}
		}
		int selectedIndex;

		public ICommand RemoveSelectedCommand {
			get { return new RelayCommand(a => RemoveSelected(), a => RemoveSelectedCanExecute()); }
		}

		public ICommand MoveSelectedUpCommand {
			get { return new RelayCommand(a => MoveSelectedUp(), a => MoveSelectedUpCanExecute()); }
		}

		public ICommand MoveSelectedDownCommand {
			get { return new RelayCommand(a => MoveSelectedDown(), a => MoveSelectedDownCanExecute()); }
		}

		void RemoveSelected() {
			if (!RemoveSelectedCanExecute())
				return;
			int index = SelectedIndex;
			this.RemoveAt(index);
			if (index < this.Count)
				SelectedIndex = index;
			else if (this.Count > 0)
				SelectedIndex = this.Count - 1;
			else
				SelectedIndex = -1;
		}

		bool RemoveSelectedCanExecute() {
			return IsEnabled && SelectedIndex >= 0 && SelectedIndex < this.Count;
		}

		void MoveSelectedUp() {
			if (!MoveSelectedUpCanExecute())
				return;
			int index = SelectedIndex;
			var item = this[index];
			this.RemoveAt(index);
			this.Insert(index - 1, item);
			SelectedIndex = index - 1;
		}

		bool MoveSelectedUpCanExecute() {
			return IsEnabled && SelectedIndex > 0 && SelectedIndex < this.Count;
		}

		void MoveSelectedDown() {
			if (!MoveSelectedDownCanExecute())
				return;
			int index = SelectedIndex;
			var item = this[index];
			this.RemoveAt(index);
			this.Insert(index + 1, item);
			SelectedIndex = index + 1;
		}

		bool MoveSelectedDownCanExecute() {
			return IsEnabled && SelectedIndex >= 0 && SelectedIndex < this.Count - 1;
		}
	}
}
