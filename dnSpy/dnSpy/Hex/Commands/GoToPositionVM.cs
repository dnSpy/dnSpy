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

using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using dnSpy.Contracts.MVVM;
using dnSpy.Properties;

namespace dnSpy.Hex.Commands {
	sealed class GoToPositionVM : ViewModelBase {
		public Integer64VM OffsetVM { get; }

		public ICommand SelectPositionAbsolute => new RelayCommand(a => PositionKind = PositionKind.Absolute);
		public ICommand SelectPositionFile => new RelayCommand(a => PositionKind = PositionKind.File);
		public ICommand SelectPositionRVA => new RelayCommand(a => PositionKind = PositionKind.RVA);
		public ICommand SelectPositionCurrent => new RelayCommand(a => PositionKind = PositionKind.CurrentPosition);

		public ObservableCollection<PositionVM> PositionsCollection { get; }

		public PositionVM SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged(nameof(SelectedItem));
				}
			}
		}
		PositionVM selectedItem;

		public PositionKind PositionKind {
			get { return SelectedItem.Kind; }
			set { SelectedItem = PositionsCollection.First(a => a.Kind == value); }
		}

		public bool SelectToNewPosition {
			get { return selectToNewPosition; }
			set {
				if (value != selectToNewPosition) {
					selectToNewPosition = value;
					OnPropertyChanged(nameof(SelectToNewPosition));
				}
			}
		}
		bool selectToNewPosition;

		public GoToPositionVM(ulong offset) {
			PositionsCollection = new ObservableCollection<PositionVM>();
			PositionsCollection.Add(new PositionVM(PositionKind.Absolute, dnSpy_Resources.GoToAbsolutePosition, dnSpy_Resources.ShortCutKeyCtrl1));
			PositionsCollection.Add(new PositionVM(PositionKind.File, dnSpy_Resources.GoToFilePosition, dnSpy_Resources.ShortCutKeyCtrl2));
			PositionsCollection.Add(new PositionVM(PositionKind.RVA, "RVA", dnSpy_Resources.ShortCutKeyCtrl3));
			PositionsCollection.Add(new PositionVM(PositionKind.CurrentPosition, dnSpy_Resources.GoToCurrentPosition, dnSpy_Resources.ShortCutKeyCtrl4));
			OffsetVM = new Integer64VM(offset, a => HasErrorUpdated());
			selectedItem = PositionsCollection[0];
		}

		public override bool HasError => OffsetVM.HasError;
	}

	sealed class PositionVM {
		public PositionKind Kind { get; }
		public string Text { get; }
		public string InputGestureText { get; }
		public PositionVM(PositionKind kind, string text, string inputGestureText) {
			Kind = kind;
			Text = text;
			InputGestureText = inputGestureText;
		}
	}
}
