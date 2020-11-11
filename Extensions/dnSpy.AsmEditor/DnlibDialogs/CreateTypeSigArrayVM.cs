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

using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class CreateTypeSigArrayVM : ViewModelBase {
		public ICommand AddCommand => new RelayCommand(a => AddCurrent(), a => AddCurrentCanExecute());

		public bool IsEnabled {
			get => isEnabled;
			set {
				if (isEnabled != value) {
					isEnabled = value;
					TypeSigCreator.IsEnabled = value;
					TypeSigCollection.IsEnabled = value;
					OnPropertyChanged(nameof(IsEnabled));
					OnPropertyChanged(nameof(CanAddMore));
					OnPropertyChanged(nameof(CanNotAddMore));
				}
			}
		}
		bool isEnabled = true;

		public TypeSig[] TypeSigArray => TypeSigCollection.ToArray();
		public MyObservableCollection<TypeSig> TypeSigCollection { get; } = new MyObservableCollection<TypeSig>();
		public TypeSigCreatorVM TypeSigCreator { get; }

		public string? Title {
			get {
				if (!string.IsNullOrEmpty(title))
					return title;
				if (IsUnlimitedCount)
					return dnSpy_AsmEditor_Resources.Create_TypeSigs;
				else if (RequiredCount!.Value == 1)
					return dnSpy_AsmEditor_Resources.Create_TypeSig;
				else
					return string.Format(dnSpy_AsmEditor_Resources.Create_N_TypeSigs, RequiredCount.Value);
			}
		}
		string? title;

		public int? RequiredCount {
			get => requiredCount;
			set {
				if (requiredCount != value) {
					requiredCount = value;
					OnPropertyChanged(nameof(RequiredCount));
					OnPropertyChanged(nameof(IsUnlimitedCount));
					OnPropertyChanged(nameof(IsFiniteCount));
					OnPropertyChanged(nameof(Title));
					UpdateNumberLeftProperties();
				}
			}
		}
		int? requiredCount;

		public bool IsFiniteCount => RequiredCount is not null;
		public bool IsUnlimitedCount => RequiredCount is null;
		public int NumberOfTypesLeft => RequiredCount is null ? -1 : RequiredCount.Value - TypeSigCollection.Count;
		public bool CanNotAddMore => !CanAddMore;
		public bool CanAddMore => IsEnabled && (IsUnlimitedCount || NumberOfTypesLeft > 0);

		public string NumberOfTypesLeftString {
			get {
				int numLeft = NumberOfTypesLeft;
				if (numLeft < 0)
					return string.Empty;
				return numLeft == 0 ? string.Empty :
					numLeft == 1 ? dnSpy_AsmEditor_Resources.Create_TypeSigs_OneLeft :
					string.Format(dnSpy_AsmEditor_Resources.Create_TypeSigs_N_Left, numLeft);
			}
		}

		public CreateTypeSigArrayVM(TypeSigCreatorOptions options, int? requiredCount) {
			title = options.Title;
			TypeSigCreator = new TypeSigCreatorVM(options);
			RequiredCount = requiredCount;
			TypeSigCollection.CollectionChanged += (s, e) => UpdateNumberLeftProperties();
		}

		void UpdateNumberLeftProperties() {
			OnPropertyChanged(nameof(NumberOfTypesLeft));
			OnPropertyChanged(nameof(NumberOfTypesLeftString));
			OnPropertyChanged(nameof(CanAddMore));
			OnPropertyChanged(nameof(CanNotAddMore));
			HasErrorUpdated();
			TypeSigCreator.IsEnabled = CanAddMore;
		}

		void AddCurrent() {
			if (!AddCurrentCanExecute())
				return;
			var typeSig = TypeSigCreator.TypeSig;
			Debug2.Assert(typeSig is not null);
			TypeSigCollection.Add(typeSig);
			TypeSigCollection.SelectedIndex = TypeSigCollection.Count - 1;
			TypeSigCreator.TypeSig = null;
		}

		bool AddCurrentCanExecute() =>
			IsEnabled &&
			(IsUnlimitedCount || NumberOfTypesLeft > 0) &&
			TypeSigCreator.TypeSig is not null;

		public override bool HasError => !IsUnlimitedCount && NumberOfTypesLeft > 0;
	}
}
