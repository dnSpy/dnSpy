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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Properties;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Documents.Tabs.Dialogs {
	sealed class OpenDocumentListVM : ViewModelBase, IDisposable {
		public ICommand RemoveCommand => new RelayCommand(a => Remove(), a => CanRemove);
		public ICommand CreateListCommand => new RelayCommand(a => CreateList(), a => CanCreateList);
		public ObservableCollection<DocumentListVM> Collection => documentListColl;
		public ICollectionView CollectionView => collectionView;
		public bool SyntaxHighlight { get; }

		readonly ObservableCollection<DocumentListVM> documentListColl;
		readonly ListCollectionView collectionView;

		public object SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged(nameof(SelectedItem));
				}
			}
		}
		object selectedItem;

		public DocumentListVM[] SelectedItems {
			get { return selectedItems; }
			set { selectedItems = value ?? Array.Empty<DocumentListVM>(); }
		}
		DocumentListVM[] selectedItems;

		public bool SearchingForDefaultLists {
			get { return searchingForDefaultLists; }
			set {
				if (searchingForDefaultLists != value) {
					searchingForDefaultLists = value;
					OnPropertyChanged(nameof(SearchingForDefaultLists));
					OnPropertyChanged(nameof(NotSearchingForDefaultLists));
				}
			}
		}
		bool searchingForDefaultLists;

		public bool NotSearchingForDefaultLists => !SearchingForDefaultLists;

		public string SearchText {
			get { return searchText; }
			set {
				if (searchText != value) {
					searchText = value;
					OnPropertyChanged(nameof(SearchText));
					Refilter();
				}
			}
		}
		string searchText;

		public bool ShowSavedLists {
			get { return showSavedLists; }
			set {
				if (showSavedLists != value) {
					showSavedLists = value;
					OnPropertyChanged(nameof(ShowSavedLists));
					Refilter();
				}
			}
		}
		bool showSavedLists;

		readonly DocumentListService documentListService;
		readonly HashSet<DocumentListVM> removedDocumentLists;
		readonly List<DocumentListVM> addedDocumentLists;
		readonly Func<string, string> askUser;
		readonly CancellationTokenSource cancellationTokenSource;
		readonly CancellationToken cancellationToken;

		public IClassificationFormatMap ClassificationFormatMap { get; }
		public ITextElementProvider TextElementProvider { get; }

		public OpenDocumentListVM(bool syntaxHighlight, IClassificationFormatMap classificationFormatMap, ITextElementProvider textElementProvider, DocumentListService documentListService, Func<string, string> askUser) {
			SyntaxHighlight = syntaxHighlight;
			ClassificationFormatMap = classificationFormatMap;
			TextElementProvider = textElementProvider;
			this.documentListService = documentListService;
			this.askUser = askUser;
			documentListColl = new ObservableCollection<DocumentListVM>();
			collectionView = (ListCollectionView)CollectionViewSource.GetDefaultView(documentListColl);
			collectionView.CustomSort = new DocumentListVM_Comparer();
			selectedItems = Array.Empty<DocumentListVM>();
			removedDocumentLists = new HashSet<DocumentListVM>();
			addedDocumentLists = new List<DocumentListVM>();
			cancellationTokenSource = new CancellationTokenSource();
			cancellationToken = cancellationTokenSource.Token;
			searchingForDefaultLists = true;

			var hash = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var documentList in documentListService.DocumentLists) {
				hash.Add(documentList.Name);
				documentListColl.Add(new DocumentListVM(this, documentList, true, true));
			}
			Refilter();

			Task.Factory.StartNew(() => new DefaultDocumentListFinder(cancellationToken).Find(), cancellationToken)
			.ContinueWith(t => {
				var ex = t.Exception;
				SearchingForDefaultLists = false;
				if (!t.IsCanceled && !t.IsFaulted) {
					foreach (var defaultList in t.Result) {
						if (hash.Contains(defaultList.Name))
							continue;
						var documentList = new DocumentList(defaultList);
						documentListColl.Add(new DocumentListVM(this, documentList, false, false));
					}
					Refilter();
				}
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		void Refilter() {
			var text = (searchText ?? string.Empty).Trim().ToUpperInvariant();
			if (text == string.Empty && !ShowSavedLists)
				CollectionView.Filter = null;
			else
				CollectionView.Filter = o => CalculateIsVisible((DocumentListVM)o, text);
		}

		bool CalculateIsVisible(DocumentListVM vm, string filterText) {
			Debug.Assert(filterText != null && filterText.Trim().ToUpperInvariant() == filterText);
			if (string.IsNullOrEmpty(filterText) && !ShowSavedLists)
				return true;
			if (ShowSavedLists && !vm.IsUserList)
				return false;
			var name = vm.Name.ToUpperInvariant();
			foreach (var s in filterText.ToUpperInvariant().Split(sep, StringSplitOptions.RemoveEmptyEntries)) {
				if (!name.Contains(s))
					return false;
			}
			return true;
		}
		static readonly char[] sep = new char[] { ' ' };

		public bool CanRemove => SelectedItems.Length > 0;

		public void Remove() {
			if (!CanRemove)
				return;
			foreach (var vm in SelectedItems.ToArray()) {
				if (vm.IsExistingList)
					removedDocumentLists.Add(vm);
				documentListColl.Remove(vm);
			}
		}

		bool CanCreateList => true;

		void CreateList() {
			if (!CanCreateList)
				return;
			var name = askUser(dnSpy_Resources.OpenList_AskForName);
			if (string.IsNullOrEmpty(name))
				return;

			var vm = new DocumentListVM(this, new DocumentList(name), false, true);
			addedDocumentLists.Add(vm);
			documentListColl.Add(vm);
		}

		public void Save() {
			foreach (var removed in removedDocumentLists) {
				Debug.Assert(removed.IsExistingList);
				if (!removed.IsExistingList)
					continue;
				bool b = documentListService.SelectedDocumentList == removed.DocumentList;
				if (b)
					continue;
				documentListService.Remove(removed.DocumentList);
			}
			foreach (var added in addedDocumentLists) {
				if (removedDocumentLists.Contains(added))
					continue;
				documentListService.Add(added.DocumentList);
			}
		}

		public void Dispose() {
			if (disposed)
				return;
			disposed = true;
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
		}
		bool disposed;
	}

	sealed class DocumentListVM_Comparer : System.Collections.IComparer {
		public int Compare(object x, object y) {
			var a = x as DocumentListVM;
			var b = y as DocumentListVM;
			if (a == b)
				return 0;
			if (a == null)
				return -1;
			if (b == null)
				return 1;
			return StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
		}
	}
}
