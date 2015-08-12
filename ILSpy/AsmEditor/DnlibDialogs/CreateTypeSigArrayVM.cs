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

using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class CreateTypeSigArrayVM : ViewModelBase {
		public ICommand AddCommand {
			get { return new RelayCommand(a => AddCurrent(), a => AddCurrentCanExecute()); }
		}

		public bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					isEnabled = value;
					typeSigCreator.IsEnabled = value;
					TypeSigCollection.IsEnabled = value;
					OnPropertyChanged("IsEnabled");
					OnPropertyChanged("CanAddMore");
					OnPropertyChanged("CanNotAddMore");
				}
			}
		}
		bool isEnabled = true;

		public TypeSig[] TypeSigArray {
			get { return TypeSigCollection.ToArray(); }
		}

		public MyObservableCollection<TypeSig> TypeSigCollection {
			get { return typeSigCollection; }
		}
		readonly MyObservableCollection<TypeSig> typeSigCollection = new MyObservableCollection<TypeSig>();

		public TypeSigCreatorVM TypeSigCreator {
			get { return typeSigCreator; }
		}
		TypeSigCreatorVM typeSigCreator;

		public string Title {
			get {
				if (!string.IsNullOrEmpty(title))
					return title;
				if (IsUnlimitedCount)
					return "Create TypeSigs";
				else if (RequiredCount.Value == 1)
					return "Create a TypeSig";
				else
					return string.Format("Create {0} TypeSigs", RequiredCount.Value);
			}
		}
		string title;

		public int? RequiredCount {
			get { return requiredCount; }
			set {
				if (requiredCount != value) {
					requiredCount = value;
					OnPropertyChanged("RequiredCount");
					OnPropertyChanged("IsUnlimitedCount");
					OnPropertyChanged("IsFiniteCount");
					OnPropertyChanged("Title");
					UpdateNumberLeftProperties();
				}
			}
		}
		int? requiredCount;

		public bool IsFiniteCount {
			get { return RequiredCount != null; }
		}

		public bool IsUnlimitedCount {
			get { return RequiredCount == null; }
		}

		public int NumberOfTypesLeft {
			get { return RequiredCount == null ? -1 : RequiredCount.Value - TypeSigCollection.Count; }
		}

		public bool CanNotAddMore {
			get { return !CanAddMore; }
		}

		public bool CanAddMore {
			get { return IsEnabled && (IsUnlimitedCount || NumberOfTypesLeft > 0); }
		}

		public string NumberOfTypesLeftString {
			get {
				int numLeft = NumberOfTypesLeft;
				if (numLeft < 0)
					return string.Empty;
				return numLeft == 0 ? string.Empty :
					numLeft == 1 ? "One TypeSig left" :
					string.Format("{0} TypeSigs left", numLeft);
			}
		}

		public CreateTypeSigArrayVM(TypeSigCreatorOptions options, int? requiredCount) {
			this.title = options.Title;
			this.typeSigCreator = new TypeSigCreatorVM(options);
			this.RequiredCount = requiredCount;
			this.TypeSigCollection.CollectionChanged += (s, e) => UpdateNumberLeftProperties();
		}

		void UpdateNumberLeftProperties() {
			OnPropertyChanged("NumberOfTypesLeft");
			OnPropertyChanged("NumberOfTypesLeftString");
			OnPropertyChanged("CanAddMore");
			OnPropertyChanged("CanNotAddMore");
			HasErrorUpdated();
			TypeSigCreator.IsEnabled = CanAddMore;
		}

		void AddCurrent() {
			if (!AddCurrentCanExecute())
				return;
			var typeSig = TypeSigCreator.TypeSig;
			TypeSigCollection.Add(typeSig);
			TypeSigCollection.SelectedIndex = TypeSigCollection.Count - 1;
			TypeSigCreator.TypeSig = null;
		}

		bool AddCurrentCanExecute() {
			return IsEnabled &&
				(IsUnlimitedCount || NumberOfTypesLeft > 0) &&
				TypeSigCreator.TypeSig != null;
		}

		public override bool HasError {
			get { return !IsUnlimitedCount && NumberOfTypesLeft > 0; }
		}
	}
}
