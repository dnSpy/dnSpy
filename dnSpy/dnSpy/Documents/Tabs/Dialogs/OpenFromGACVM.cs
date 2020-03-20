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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.ETW;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Utilities;
using dnSpy.Properties;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Documents.Tabs.Dialogs {
	sealed class OpenFromGACVM : ViewModelBase, IGACFileReceiver, IDisposable {
		public ObservableCollection<GACFileVM> Collection => gacFileList;
		public ICollectionView CollectionView => collectionView;
		public bool SyntaxHighlight { get; }
		public IClassificationFormatMap ClassificationFormatMap { get; }
		public ITextElementProvider TextElementProvider { get; }

		public string OpenGAC_Search_ToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.OpenGAC_Search_ToolTip, dnSpy_Resources.ShortCutKeyCtrlF);

		readonly ObservableCollection<GACFileVM> gacFileList;
		readonly ListCollectionView collectionView;

		public object? SelectedItem {
			get => selectedItem;
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged(nameof(SelectedItem));
				}
			}
		}
		object? selectedItem;

		public bool SearchingGAC {
			get => searchingGAC;
			set {
				if (searchingGAC != value) {
					searchingGAC = value;
					OnPropertyChanged(nameof(SearchingGAC));
					OnPropertyChanged(nameof(NotSearchingGAC));
				}
			}
		}
		bool searchingGAC;

		public bool NotSearchingGAC => !SearchingGAC;

		public string? SearchText {
			get => searchText;
			set {
				if (searchText != value) {
					searchText = value;
					OnPropertyChanged(nameof(SearchText));
					Refilter();
				}
			}
		}
		string? searchText;

		public bool ShowDuplicates {
			get => showDuplicates;
			set {
				if (showDuplicates != value) {
					showDuplicates = value;
					OnPropertyChanged(nameof(ShowDuplicates));
					Refilter();
				}
			}
		}
		bool showDuplicates;

		readonly CancellationTokenSource cancellationTokenSource;
		readonly CancellationToken cancellationToken;
		readonly HashSet<GACFileVM> uniqueFiles;

		public OpenFromGACVM(bool syntaxHighlight, IClassificationFormatMap classificationFormatMap, ITextElementProvider textElementProvider) {
			SyntaxHighlight = syntaxHighlight;
			ClassificationFormatMap = classificationFormatMap;
			TextElementProvider = textElementProvider;
			gacFileList = new ObservableCollection<GACFileVM>();
			collectionView = (ListCollectionView)CollectionViewSource.GetDefaultView(gacFileList);
			collectionView.CustomSort = new GACFileVM_Comparer();
			cancellationTokenSource = new CancellationTokenSource();
			cancellationToken = cancellationTokenSource.Token;
			searchingGAC = true;
			uniqueFiles = new HashSet<GACFileVM>(new GACFileVM_EqualityComparer());

			var dispatcher = Dispatcher.CurrentDispatcher;
			DnSpyEventSource.Log.OpenFromGACStart();
			Task.Factory.StartNew(() => new GACFileFinder(this, dispatcher, cancellationToken).Find(), cancellationToken)
			.ContinueWith(t => {
				DnSpyEventSource.Log.OpenFromGACStop();
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

		void RefreshCounters() => OnPropertyChanged(nameof(FilesShownInfo));

		public void AddFiles(IEnumerable<GacFileInfo> files) {
			foreach (var file in files) {
				var vm = new GACFileVM(this, file);
				vm.IsDuplicate = uniqueFiles.Contains(vm);
				uniqueFiles.Add(vm);
				Collection.Add(vm);
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
			Debug2.Assert(!(filterText is null) && filterText.Trim().ToUpperInvariant() == filterText);
			if (!ShowDuplicates && vm.IsDuplicate)
				return false;
			if (string.IsNullOrEmpty(filterText))
				return true;
			var name = vm.Name.ToUpperInvariant();
			var version = vm.VersionString.ToUpperInvariant();
			foreach (var s in filterText.ToUpperInvariant().Split(sep, StringSplitOptions.RemoveEmptyEntries)) {
				if (!name.Contains(s) && !version.Contains(s))
					return false;
			}
			return true;
		}
		static readonly char[] sep = new char[] { ' ' };

		public void Dispose() {
			if (disposed)
				return;
			disposed = true;
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
		}
		bool disposed;
	}

	sealed class GACFileVM_EqualityComparer : IEqualityComparer<GACFileVM> {
		// Ignore culture
		const AssemblyNameComparerFlags flags = AssemblyNameComparerFlags.Name |
												AssemblyNameComparerFlags.Version |
												AssemblyNameComparerFlags.PublicKeyToken |
												AssemblyNameComparerFlags.ContentType;

		public bool Equals([AllowNull] GACFileVM x, [AllowNull] GACFileVM y) {
			if (x == y)
				return true;
			if (x is null || y is null)
				return false;
			return new AssemblyNameComparer(flags).Equals(x.Assembly, y.Assembly);
		}

		public int GetHashCode([DisallowNull] GACFileVM obj) {
			if (obj is null)
				return 0;
			return new AssemblyNameComparer(flags).GetHashCode(obj.Assembly);
		}
	}

	sealed class GACFileVM_Comparer : System.Collections.IComparer {
		public int Compare(object? x, object? y) {
			var a = x as GACFileVM;
			var b = y as GACFileVM;
			if (a == b)
				return 0;
			if (a is null)
				return -1;
			if (b is null)
				return 1;
			return new AssemblyNameComparer(AssemblyNameComparerFlags.All).CompareTo(a.Assembly, b.Assembly);
		}
	}
}
