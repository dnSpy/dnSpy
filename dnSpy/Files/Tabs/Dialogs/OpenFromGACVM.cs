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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using dnlib.DotNet;
using dnSpy.Properties;
using dnSpy.Shared.Files;
using dnSpy.Shared.MVVM;

namespace dnSpy.Files.Tabs.Dialogs {
	sealed class OpenFromGACVM : ViewModelBase, IGACFileReceiver, IDisposable {
		public ObservableCollection<GACFileVM> Collection {
			get { return gacFileList; }
		}
		readonly ObservableCollection<GACFileVM> gacFileList;

		public ICollectionView CollectionView {
			get { return collectionView; }
		}
		readonly ListCollectionView collectionView;

		public object SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged("SelectedItem");
				}
			}
		}
		object selectedItem;

		public bool SyntaxHighlight {
			get { return syntaxHighlight; }
		}
		readonly bool syntaxHighlight;

		public bool SearchingGAC {
			get { return searchingGAC; }
			set {
				if (searchingGAC != value) {
					searchingGAC = value;
					OnPropertyChanged("SearchingGAC");
					OnPropertyChanged("NotSearchingGAC");
				}
			}
		}
		bool searchingGAC;

		public bool NotSearchingGAC {
			get { return !SearchingGAC; }
		}

		public string SearchText {
			get { return searchText; }
			set {
				if (searchText != value) {
					searchText = value;
					OnPropertyChanged("SearchText");
					Refilter();
				}
			}
		}
		string searchText;

		public bool ShowDuplicates {
			get { return showDuplicates; }
			set {
				if (showDuplicates != value) {
					showDuplicates = value;
					OnPropertyChanged("ShowDuplicates");
					Refilter();
				}
			}
		}
		bool showDuplicates;

		readonly CancellationTokenSource cancellationTokenSource;
		readonly HashSet<GACFileVM> uniqueFiles;

		public OpenFromGACVM(bool syntaxHighlight) {
			this.syntaxHighlight = syntaxHighlight;
			this.gacFileList = new ObservableCollection<GACFileVM>();
			this.collectionView = (ListCollectionView)CollectionViewSource.GetDefaultView(gacFileList);
			this.collectionView.CustomSort = new GACFileVM_Comparer();
			this.cancellationTokenSource = new CancellationTokenSource();
			this.searchingGAC = true;
			this.uniqueFiles = new HashSet<GACFileVM>(new GACFileVM_EqualityComparer());

			var dispatcher = Dispatcher.CurrentDispatcher;
			Task.Factory.StartNew(() => new GACFileFinder(this, dispatcher, cancellationTokenSource.Token).Find(), cancellationTokenSource.Token)
			.ContinueWith(t => {
				var ex = t.Exception;
				SearchingGAC = false;
				Refilter();
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public string FilesShownInfo {
			get {
				if (collectionView.Count == gacFileList.Count)
					return string.Empty;
				return string.Format(dnSpy_Resources.OpenGAC_ShowFiles, collectionView.Count, gacFileList.Count);
			}
		}

		void RefreshCounters() {
			OnPropertyChanged("FilesShownInfo");
		}

		public void AddFiles(IEnumerable<GacFileInfo> files) {
			foreach (var file in files) {
				var vm = new GACFileVM(this, file);
				vm.IsDuplicate = uniqueFiles.Contains(vm);
				uniqueFiles.Add(vm);
				this.Collection.Add(vm);
			}
			RefreshCounters();
		}

		void Refilter() {
			var text = (searchText ?? string.Empty).Trim().ToUpperInvariant();
			if (text == string.Empty && ShowDuplicates)
				CollectionView.Filter = null;
			else
				CollectionView.Filter = o => CalculateIsVisible((GACFileVM)o, text);
			RefreshCounters();
		}

		bool CalculateIsVisible(GACFileVM vm, string filterText) {
			Debug.Assert(filterText != null && filterText.Trim().ToUpperInvariant() == filterText);
			if (!ShowDuplicates && vm.IsDuplicate)
				return false;
			if (string.IsNullOrEmpty(filterText))
				return true;
			var name = vm.Name.ToUpperInvariant();
			foreach (var s in filterText.ToUpperInvariant().Split(sep)) {
				if (!name.Contains(s))
					return false;
			}
			return true;
		}
		static readonly char[] sep = new char[] { ' ' };

		public void Dispose() {
			cancellationTokenSource.Cancel();
		}
	}

	sealed class GACFileVM_EqualityComparer : IEqualityComparer<GACFileVM> {
		// Ignore culture
		const AssemblyNameComparerFlags flags = AssemblyNameComparerFlags.Name |
												AssemblyNameComparerFlags.Version |
												AssemblyNameComparerFlags.PublicKeyToken |
												AssemblyNameComparerFlags.ContentType;

		public bool Equals(GACFileVM x, GACFileVM y) {
			if (x == y)
				return true;
			if (x == null || y == null)
				return false;
			return new AssemblyNameComparer(flags).Equals(x.Assembly, y.Assembly);
		}

		public int GetHashCode(GACFileVM obj) {
			if (obj == null)
				return 0;
			return new AssemblyNameComparer(flags).GetHashCode(obj.Assembly);
		}
	}

	sealed class GACFileVM_Comparer : System.Collections.IComparer {
		public int Compare(object x, object y) {
			var a = x as GACFileVM;
			var b = y as GACFileVM;
			if (a == b)
				return 0;
			if (a == null)
				return -1;
			if (b == null)
				return 1;
			return new AssemblyNameComparer(AssemblyNameComparerFlags.All).CompareTo(a.Assembly, b.Assembly);
		}
	}
}
