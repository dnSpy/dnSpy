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
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using ICSharpCode.ILSpy.AsmEditor.ViewHelpers;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	// Needed by ListVMControl
	abstract class ListVM : ViewModelBase
	{
		public abstract void EditCurrent();
	}

	abstract class ListVM<TVM, TModel> : ListVM
	{
		public IEdit<TVM> EditObject {
			set { editObject = value; }
		}
		IEdit<TVM> editObject;

		public bool InlineEditing {
			get { return inlineEditing; }
		}

		public bool NotInlineEditing {
			get { return !InlineEditing; }
		}

		public ICommand EditCommand {
			get { return new RelayCommand(a => EditCurrent(), a => EditCurrentCanExecute()); }
		}

		public ICommand AddCommand {
			get { return new RelayCommand(a => AddCurrent(), a => AddCurrentCanExecute()); }
		}

		public MyObservableCollection<TVM> Collection {
			get { return collection; }
		}
		readonly MyObservableCollection<TVM> collection = new MyObservableCollection<TVM>();

		readonly string editString;
		readonly string createString;
		protected readonly ModuleDef module;
		protected readonly Language language;
		protected readonly TypeDef ownerType;
		protected readonly MethodDef ownerMethod;
		readonly bool inlineEditing;

		protected ListVM(string editString, string createString, ModuleDef module, Language language, TypeDef ownerType, MethodDef ownerMethod, bool inlineEditing = false)
		{
			this.editString = editString;
			this.createString = createString;
			this.module = module;
			this.language = language;
			this.ownerType = ownerType;
			this.ownerMethod = ownerMethod;
			this.inlineEditing = inlineEditing;
		}

		protected abstract TVM Create(TModel model);
		protected abstract TVM Clone(TVM obj);
		protected abstract TVM Create();

		public void InitializeFrom(IEnumerable<TModel> modelObjs)
		{
			Collection.Clear();
			Collection.AddRange(modelObjs.Select(a => Create(a)));
		}

		TVM EditClone(TVM obj)
		{
			if (InlineEditing)
				return obj;
			if (editObject == null)
				throw new InvalidOperationException();
			return editObject.Edit(editString, obj);
		}

		TVM AddNew(TVM obj)
		{
			if (InlineEditing)
				return obj;
			if (editObject == null)
				throw new InvalidOperationException();
			return editObject.Edit(createString, obj);
		}

		public override void EditCurrent()
		{
			if (!EditCurrentCanExecute())
				return;
			int index = Collection.SelectedIndex;
			var vm = EditClone(Clone(Collection[index]));
			if (vm != null) {
				Collection[index] = vm;
				Collection.SelectedIndex = index;
			}
		}

		bool EditCurrentCanExecute()
		{
			return NotInlineEditing && Collection.SelectedIndex >= 0 && Collection.SelectedIndex < Collection.Count;
		}

		protected virtual void AddCurrent()
		{
			if (!AddCurrentCanExecute())
				return;

			var vm = AddNew(Create());
			if (vm != null) {
				var index = GetAddIndex(vm);
				Collection.Insert(index, vm);
				Collection.SelectedIndex = index;
			}
		}

		protected virtual int GetAddIndex(TVM obj)
		{
			return Collection.Count;
		}

		protected virtual bool AddCurrentCanExecute()
		{
			return true;
		}

		protected override string Verify(string columnName)
		{
			return string.Empty;
		}

		public override bool HasError {
			get { return false; }
		}
	}
}
