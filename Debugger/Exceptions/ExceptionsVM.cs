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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using dnSpy.MVVM;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Debugger.Exceptions {
	sealed class ExceptionsVM : ViewModelBase {
		public ObservableCollection<ExceptionVM> Collection {
			get { return exceptionsList; }
		}
		readonly ObservableCollection<ExceptionVM> exceptionsList;

		public ICollectionView CollectionView {
			get { return collectionView; }
		}
		readonly ICollectionView collectionView;

		public object ShowOnlyEnabledExceptionsImageObject { get { return this; } }
		public object AddExceptionImageObject { get { return this; } }
		public object RemoveExceptionImageObject { get { return this; } }
		public object RestoreDefaultsImageObject { get { return this; } }

		public ICommand AddExceptionCommand {
			get { return new RelayCommand(a => AddException(), a => CanAddException); }
		}

		public ICommand RemoveExceptionsCommand {
			get { return new RelayCommand(a => RemoveExceptions(), a => CanRemoveExceptions); }
		}

		public ICommand RestoreDefaultsCommand {
			get { return new RelayCommand(a => RestoreDefaults(), a => CanRestoreDefaults); }
		}

		public bool ShowOnlyEnabledExceptions {
			get { return showOnlyEnabledExceptions; }
			set {
				if (showOnlyEnabledExceptions != value) {
					showOnlyEnabledExceptions = value;
					OnPropertyChanged("ShowOnlyEnabledExceptions");
					Refilter();
				}
			}
		}
		bool showOnlyEnabledExceptions;

		public string FilterText {
			get { return filterText; }
			set {
				if (filterText != value) {
					filterText = value;
					OnPropertyChanged("FilterText");
					Refilter();
				}
			}
		}
		string filterText = string.Empty;

		void Refilter() {
			var text = (filterText ?? string.Empty).Trim().ToUpperInvariant();
			if (text == string.Empty && !ShowOnlyEnabledExceptions)
				CollectionView.Filter = null;
			else
				CollectionView.Filter = o => CalculateIsVisible((ExceptionVM)o, text);
		}

		bool CalculateIsVisible(ExceptionVM vm, string filterText) {
			Debug.Assert(filterText != null && filterText.Trim().ToUpperInvariant() == filterText);
			if (ShowOnlyEnabledExceptions && !vm.BreakOnFirstChance)
				return false;
			return string.IsNullOrEmpty(filterText) || vm.Name.ToUpperInvariant().Contains(filterText);
		}

		readonly ISelectedItemsProvider<ExceptionVM> selectedItemsProvider;
		readonly IGetNewExceptionName getNewExceptionName;

		public ExceptionsVM(ISelectedItemsProvider<ExceptionVM> selectedItemsProvider, IGetNewExceptionName getNewExceptionName) {
			this.selectedItemsProvider = selectedItemsProvider;
			this.getNewExceptionName = getNewExceptionName;
			this.exceptionsList = new ObservableCollection<ExceptionVM>();
			this.collectionView = CollectionViewSource.GetDefaultView(exceptionsList);
			DebuggerSettings.Instance.PropertyChanged += DebuggerSettings_PropertyChanged;
			ExceptionManager.Instance.Changed += ExceptionManager_Changed;
			InitializeDefaultExceptions();
		}

		void ExceptionManager_Changed(object sender, ExceptionManagerEventArgs e) {
			switch (e.EventType) {
			case ExceptionManagerEventType.Restored:
				InitializeDefaultExceptions();
				break;

			case ExceptionManagerEventType.Removed:
				Remove((List<ExceptionInfo>)e.Argument);
				break;

			case ExceptionManagerEventType.Added:
				Add((ExceptionInfo)e.Argument);
				break;

			case ExceptionManagerEventType.ExceptionInfoPropertyChanged:
				break;

			default:
				Debug.Fail(string.Format("Unknown type: {0}", e.EventType));
				break;
			}
		}

		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "SyntaxHighlightExceptions")
				RefreshThemeFields();
		}

		void InitializeDefaultExceptions() {
			Collection.Clear();
			foreach (var info in Sort(ExceptionManager.Instance.ExceptionInfos))
				Collection.Add(new ExceptionVM(info));
		}

		static ExceptionInfo[] Sort(IEnumerable<ExceptionInfo> infos) {
			var ary = infos.ToArray();
			Array.Sort(ary, CompareExceptionInfos);
			return ary;
		}

		static int CompareExceptionInfos(ExceptionInfo a, ExceptionInfo b) {
			int res = a.ExceptionType.CompareTo(b.ExceptionType);
			if (res != 0)
				return res;
			if (a.IsOtherExceptions != b.IsOtherExceptions)
				return a.IsOtherExceptions ? -1 : 1;
			return StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name);
		}

		internal void RefreshThemeFields() {
			foreach (var vm in Collection)
				vm.RefreshThemeFields();
			OnPropertyChanged("ShowOnlyEnabledExceptionsImageObject");
			OnPropertyChanged("AddExceptionImageObject");
			OnPropertyChanged("RemoveExceptionImageObject");
			OnPropertyChanged("RestoreDefaultsImageObject");
		}

		public bool CanAddException {
			get { return true; }
		}

		public void AddException() {
			var name = getNewExceptionName.GetName();
			if (string.IsNullOrEmpty(name))
				return;
			name = name.Trim();
			if (string.IsNullOrEmpty(name))
				return;
			AddException(ExceptionType.DotNet, name);
		}

		public void AddException(ExceptionType type, string name) {
			if (name == null)
				return;
			ExceptionManager.Instance.Add(new ExceptionInfoKey(type, name));
		}

		void Add(ExceptionInfo info) {
			var vm = new ExceptionVM(info);
			int i;
			for (i = 0; i + 1 < Collection.Count; i++) {
				int res = CompareExceptionInfos(vm.ExceptionInfo, Collection[i].ExceptionInfo);
				if (res <= 0)
					break;
			}
			Collection.Insert(i, vm);
		}

		static ExceptionVM[] GetRemovableExceptions(ExceptionVM[] items) {
			return items.Where(a => ExceptionManager.Instance.CanRemove(a.ExceptionInfo)).ToArray();
		}

		public bool CanRemoveExceptions {
			get { return GetRemovableExceptions(selectedItemsProvider.SelectedItems).Length != 0; }
		}

		public void RemoveExceptions() {
			var items = GetRemovableExceptions(selectedItemsProvider.SelectedItems);
			ExceptionManager.Instance.RemoveExceptions(items.Select(a => a.ExceptionInfo));
		}

		void Remove(List<ExceptionInfo> infos) {
			var dict = new Dictionary<ExceptionInfo, int>(Collection.Count);
			for (int i = 0; i < Collection.Count; i++)
				dict[Collection[i].ExceptionInfo] = i;
			var ary = new int[infos.Count];
			for (int i = 0; i < infos.Count; i++)
				ary[i] = dict[infos[i]];
			Array.Sort(ary);
			for (int i = ary.Length - 1; i >= 0; i--)
				Collection.RemoveAt(ary[i]);
		}

		public bool CanRestoreDefaults {
			get { return true; }
		}

		public void RestoreDefaults() {
			ExceptionManager.Instance.RestoreDefaults();
			FilterText = string.Empty;
			ShowOnlyEnabledExceptions = false;
		}

		public bool CanEnableAllFilteredExceptions {
			get { return CollectionView.OfType<ExceptionVM>().Any(a => !a.BreakOnFirstChance); }
		}

		public void EnableAllFilteredExceptions() {
			using (ExceptionListSettings.Instance.TemporarilyDisableSave()) {
				foreach (ExceptionVM vm in CollectionView)
					vm.BreakOnFirstChance = true;
			}
		}

		public bool CanDisableAllFilteredExceptions {
			get { return CollectionView.OfType<ExceptionVM>().Any(a => a.BreakOnFirstChance); }
		}

		public void DisableAllFilteredExceptions() {
			using (ExceptionListSettings.Instance.TemporarilyDisableSave()) {
				foreach (ExceptionVM vm in CollectionView)
					vm.BreakOnFirstChance = false;
			}
			// Don't Refilter() now since items could be hidden
		}

		public bool Exists(ExceptionType type, string name) {
			return ExceptionManager.Instance.Exists(new ExceptionInfoKey(type, name));
		}
	}
}
