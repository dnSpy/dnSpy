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

using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.MVVM;
using dnSpy.Properties;

namespace dnSpy.Hex.Commands {
	sealed class SelectVM : ViewModelBase {
		public HexPositionVM StartVM { get; }
		public HexPositionVM EndVM { get; }

		public ICommand SelectPositionAbsoluteCommand => new RelayCommand(a => PositionKind = PositionKind.Absolute);
		public ICommand SelectPositionFileCommand => new RelayCommand(a => PositionKind = PositionKind.File);
		public ICommand SelectPositionRVACommand => new RelayCommand(a => PositionKind = PositionKind.RVA);
		public ICommand SelectPositionCurrentCommand => new RelayCommand(a => PositionKind = PositionKind.CurrentPosition);
		public ICommand SelectPositionCommand => new RelayCommand(a => PositionLengthKind = SelectPositionLengthKind.Position);
		public ICommand SelectLengthCommand => new RelayCommand(a => PositionLengthKind = SelectPositionLengthKind.Length);

		public ObservableCollection<PositionVM> PositionsCollection { get; }
		public ObservableCollection<PositionLengthVM> PositionLengthCollection { get; }

		public PositionVM PositionsCollectionSelectedItem {
			get => positionsCollectionSelectedItem;
			set {
				if (positionsCollectionSelectedItem != value) {
					positionsCollectionSelectedItem = value;
					OnPropertyChanged(nameof(PositionsCollectionSelectedItem));
				}
			}
		}
		PositionVM positionsCollectionSelectedItem;

		public PositionLengthVM PositionLengthCollectionSelectedItem {
			get => positionLengthCollectionSelectedItem;
			set {
				if (positionLengthCollectionSelectedItem != value) {
					positionLengthCollectionSelectedItem = value;
					OnPropertyChanged(nameof(PositionLengthCollectionSelectedItem));
				}
			}
		}
		PositionLengthVM positionLengthCollectionSelectedItem;

		public PositionKind PositionKind {
			get => PositionsCollectionSelectedItem.Kind;
			set => PositionsCollectionSelectedItem = PositionsCollection.First(a => a.Kind == value);
		}

		public SelectPositionLengthKind PositionLengthKind {
			get => PositionLengthCollectionSelectedItem.Kind;
			set => PositionLengthCollectionSelectedItem = PositionLengthCollection.First(a => a.Kind == value);
		}

		public SelectVM(HexPosition start, HexPosition end) {
			StartVM = new HexPositionVM(start, a => HasErrorUpdated(), false);
			EndVM = new HexPositionVM(end, a => HasErrorUpdated(), false);
			PositionsCollection = new ObservableCollection<PositionVM>();
			PositionsCollection.Add(new PositionVM(PositionKind.Absolute, dnSpy_Resources.GoToAbsolutePosition, dnSpy_Resources.ShortCutKeyCtrl1));
			PositionsCollection.Add(new PositionVM(PositionKind.File, dnSpy_Resources.GoToFilePosition, dnSpy_Resources.ShortCutKeyCtrl2));
			PositionsCollection.Add(new PositionVM(PositionKind.RVA, "RVA", dnSpy_Resources.ShortCutKeyCtrl3));
			PositionsCollection.Add(new PositionVM(PositionKind.CurrentPosition, dnSpy_Resources.GoToCurrentPosition, dnSpy_Resources.ShortCutKeyCtrl4));
			positionsCollectionSelectedItem = PositionsCollection[0];
			PositionLengthCollection = new ObservableCollection<PositionLengthVM>();
			PositionLengthCollection.Add(new PositionLengthVM(SelectPositionLengthKind.Position, dnSpy_Resources.GoToAbsolutePosition, dnSpy_Resources.ShortCutKeyCtrlO));
			PositionLengthCollection.Add(new PositionLengthVM(SelectPositionLengthKind.Length, dnSpy_Resources.SelectLength, dnSpy_Resources.ShortCutKeyCtrlL));
			positionLengthCollectionSelectedItem = PositionLengthCollection[0];
		}

		public override bool HasError => StartVM.HasError || EndVM.HasError;
	}

	enum SelectPositionLengthKind {
		Position,
		Length,
	}

	sealed class PositionLengthVM : ViewModelBase {
		public SelectPositionLengthKind Kind { get; }
		public string Text { get; }
		public string InputGestureText { get; }
		public PositionLengthVM(SelectPositionLengthKind kind, string text, string inputGestureText) {
			Kind = kind;
			Text = text;
			InputGestureText = inputGestureText;
		}
	}
}
