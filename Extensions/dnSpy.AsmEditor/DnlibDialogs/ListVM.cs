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
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.DnlibDialogs {
	// Needed by ListVMControl
	abstract class ListVM : ViewModelBase {
		public abstract void EditItem();
	}

	abstract class ListVM<TVM, TModel> : ListVM where TVM : class {
		public IEdit<TVM> EditObject {
			set => editObject = value;
		}
		IEdit<TVM>? editObject;

		public bool IsEnabled {
			get => isEnabled;
			set {
				if (isEnabled != value) {
					isEnabled = value;
					Collection.IsEnabled = value;
					OnPropertyChanged(nameof(IsEnabled));
				}
			}
		}
		bool isEnabled = true;

		public bool InlineEditing => inlineEditing;
		public bool NotInlineEditing => !InlineEditing;
		public ICommand EditCommand => new RelayCommand(a => EditItem(), a => EditItemCanExecute());
		public ICommand AddCommand => new RelayCommand(a => AddItem(), a => AddItemCanExecute());
		public MyObservableCollection<TVM> Collection { get; } = new MyObservableCollection<TVM>();
		public ModuleDef OwnerModule { get; }

		readonly string? editString;
		readonly string? createString;
		protected readonly IDecompilerService decompilerService;
		protected readonly TypeDef? ownerType;
		protected readonly MethodDef? ownerMethod;
		readonly bool inlineEditing;

		protected ListVM(string? editString, string? createString, ModuleDef ownerModule, IDecompilerService decompilerService, TypeDef? ownerType, MethodDef? ownerMethod, bool inlineEditing = false) {
			this.editString = editString;
			this.createString = createString;
			OwnerModule = ownerModule;
			this.decompilerService = decompilerService;
			this.ownerType = ownerType;
			this.ownerMethod = ownerMethod;
			this.inlineEditing = inlineEditing;
			((INotifyPropertyChanged)Collection).PropertyChanged += Collection_PropertyChanged;
		}

		void Collection_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(Collection.IsEnabled))
				IsEnabled = Collection.IsEnabled;
		}

		protected abstract TVM Create(TModel model);
		protected abstract TVM Clone(TVM obj);
		protected abstract TVM Create();

		public void InitializeFrom(IEnumerable<TModel> modelObjs) {
			Collection.Clear();
			Collection.AddRange(modelObjs.Select(a => Create(a)));
		}

		TVM? EditClone(TVM obj) {
			if (InlineEditing)
				return obj;
			if (editObject is null)
				throw new InvalidOperationException();
			return editObject.Edit(editString, obj);
		}

		TVM? AddNew(TVM obj) {
			if (InlineEditing)
				return obj;
			if (editObject is null)
				throw new InvalidOperationException();
			return editObject.Edit(createString, obj);
		}

		public override void EditItem() {
			if (!EditItemCanExecute())
				return;
			int index = Collection.SelectedIndex;
			var vm = EditClone(Clone(Collection[index]));
			if (!(vm is null)) {
				Collection[index] = vm;
				Collection.SelectedIndex = index;
			}
		}

		bool EditItemCanExecute() => NotInlineEditing && Collection.SelectedIndex >= 0 && Collection.SelectedIndex < Collection.Count;

		protected virtual void AddItem() {
			if (!AddItemCanExecute())
				return;

			var vm = AddNew(Create());
			if (!(vm is null)) {
				var index = GetAddIndex(vm);
				Collection.Insert(index, vm);
				Collection.SelectedIndex = index;
			}
		}

		protected virtual int GetAddIndex(TVM obj) => Collection.Count;
		protected virtual bool AddItemCanExecute() => true;
	}
}
