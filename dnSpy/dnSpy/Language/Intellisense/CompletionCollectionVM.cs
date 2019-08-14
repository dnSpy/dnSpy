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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Language.Intellisense {
	sealed class CompletionCollectionVM : INotifyCollectionChanged, IList {
		public event NotifyCollectionChangedEventHandler? CollectionChanged;

		public object? this[int index] {
			get => list[index];
			set => throw new NotImplementedException();
		}

		public int Count => list.Count;
		public bool IsFixedSize => false;
		public bool IsReadOnly => true;
		public bool IsSynchronized => false;
		public object SyncRoot => list;

		readonly List<CompletionVM> list;
		readonly IList<Completion> completionList;
		readonly INotifyCollectionChanged? completionListNotifyCollectionChanged;

		public CompletionCollectionVM(IList<Completion> completionList) {
			this.completionList = completionList ?? throw new ArgumentNullException(nameof(completionList));
			completionListNotifyCollectionChanged = completionList as INotifyCollectionChanged;
			if (!(completionListNotifyCollectionChanged is null))
				completionListNotifyCollectionChanged.CollectionChanged += CompletionList_CollectionChanged;
			list = new List<CompletionVM>(completionList.Count);
			ReinitializeList();
		}

		void CompletionList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
			int i;
			switch (e.Action) {
			case NotifyCollectionChangedAction.Add:
				Debug2.Assert(!(e.NewItems is null));
				i = e.NewStartingIndex;
				var newList = new List<CompletionVM>();
				foreach (Completion? c in e.NewItems) {
					Debug2.Assert(!(c is null));
					var vm = GetOrCreateVM(c);
					newList.Add(vm);
					list.Insert(i++, vm);
				}
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newList, e.NewStartingIndex));
				break;

			case NotifyCollectionChangedAction.Remove:
				Debug2.Assert(!(e.OldItems is null));
				var oldList = new List<CompletionVM>();
				foreach (Completion? c in e.OldItems) {
					Debug2.Assert(!(c is null));
					var vm = CompletionVM.TryGet(c);
					if (!(vm is null))
						oldList.Add(vm);
					Debug.Assert(list[e.OldStartingIndex].Completion == vm?.Completion);
					list.RemoveAt(e.OldStartingIndex);
				}
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldList, e.NewStartingIndex));
				break;

			case NotifyCollectionChangedAction.Replace:
				throw new NotSupportedException();

			case NotifyCollectionChangedAction.Move:
				throw new NotSupportedException();

			case NotifyCollectionChangedAction.Reset:
				ReinitializeList();
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
				break;

			default:
				Debug.Fail($"Unknown action: {e.Action}");
				break;
			}
		}

		CompletionVM GetOrCreateVM(Completion completion) => CompletionVM.TryGet(completion) ?? new CompletionVM(completion);

		void ReinitializeList() {
			list.Clear();
			foreach (var c in completionList)
				list.Add(GetOrCreateVM(c));
		}

		public bool Contains(object? value) => list.Contains((value as CompletionVM)!);
		public int IndexOf(object? value) => list.IndexOf((value as CompletionVM)!);
		public void CopyTo(Array array, int index) => Array.Copy(list.ToArray(), 0, array, index, list.Count);
		public IEnumerator GetEnumerator() => list.GetEnumerator();

		public int Add(object? value) => throw new NotSupportedException();
		public void Clear() => throw new NotSupportedException();
		public void Insert(int index, object? value) => throw new NotSupportedException();
		public void Remove(object? value) => throw new NotSupportedException();
		public void RemoveAt(int index) => throw new NotSupportedException();
		public void Dispose() {
			if (!(completionListNotifyCollectionChanged is null))
				completionListNotifyCollectionChanged.CollectionChanged -= CompletionList_CollectionChanged;
			list.Clear();
		}
	}
}
