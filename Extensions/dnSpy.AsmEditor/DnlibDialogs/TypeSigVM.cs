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
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.DnlibDialogs {
	abstract class TypeVMBase<T> : DataFieldVM<T> where T : IType {
		readonly TypeSigCreatorOptions? options;
		T type;

		public ITypeSigCreator TypeSigCreator {
			set => typeSigCreator = value;
		}
		ITypeSigCreator? typeSigCreator;

		public ICommand PickTypeCommand => new RelayCommand(a => PickType());

		protected TypeVMBase(T value, Action<DataFieldVM> onUpdated, TypeSigCreatorOptions? options)
			: base(onUpdated) {
			type = default!;
			if (!(options is null)) {
				this.options = options.Clone(dnSpy_AsmEditor_Resources.CreateType);
				this.options.NullTypeSigAllowed = true;
			}
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(T value) {
			type = value;
			return ToString(type);
		}

		protected override string? ConvertToValue(out T value) {
			value = type;
			return null;
		}

		void PickType() {
			if (typeSigCreator is null)
				throw new InvalidOperationException();
			if (options is null)
				throw new InvalidOperationException();
			var newTypeSig = typeSigCreator.Create(options, ToTypeSig(type), out bool canceled);
			if (!canceled)
				SetValue(ToType(newTypeSig!));
		}

		protected abstract TypeSig ToTypeSig(T type);
		protected abstract T ToType(TypeSig type);
		internal static string ToString(IType type) => type is null ? "null" : type.FullName;
	}

	sealed class TypeSigVM : TypeVMBase<TypeSig> {
		public TypeSigVM(Action<DataFieldVM> onUpdated, TypeSigCreatorOptions? options)
			: this(null, onUpdated, options) {
		}

		public TypeSigVM(TypeSig? value, Action<DataFieldVM> onUpdated, TypeSigCreatorOptions? options)
			: base(value!, onUpdated, options) {
		}

		protected override TypeSig ToTypeSig(TypeSig type) => type;
		protected override TypeSig ToType(TypeSig type) => type;
	}

	sealed class TypeDefOrRefVM : TypeVMBase<ITypeDefOrRef> {
		public TypeDefOrRefVM(Action<DataFieldVM> onUpdated, TypeSigCreatorOptions options)
			: this(null, onUpdated, options) {
		}

		public TypeDefOrRefVM(ITypeDefOrRef? value, Action<DataFieldVM> onUpdated, TypeSigCreatorOptions options)
			: base(value!, onUpdated, options) {
		}

		protected override TypeSig ToTypeSig(ITypeDefOrRef type) => type.ToTypeSig();
		protected override ITypeDefOrRef ToType(TypeSig type) => type.ToTypeDefOrRef();
	}

	abstract class TypeListDataFieldVMBase<T> : DataFieldVM<IList<T>> where T : IType {
		readonly TypeSigCreatorOptions? options;
		readonly List<T> types = new List<T>();

		public ITypeSigCreator TypeSigCreator {
			set => typeSigCreator = value;
		}
		ITypeSigCreator? typeSigCreator;

		public ICommand AddTypeCommand => new RelayCommand(a => AddType());
		public ICommand RemoveTypeCommand => new RelayCommand(a => RemoveType(), a => RemoveTypeCanExecute());
		public ICommand ClearTypesCommand => new RelayCommand(a => ClearTypes(), a => ClearTypesCanExecute());

		protected TypeListDataFieldVMBase(IList<T> value, Action<DataFieldVM> onUpdated, TypeSigCreatorOptions? options)
			: base(onUpdated) {
			if (!(options is null)) {
				this.options = options.Clone(dnSpy_AsmEditor_Resources.CreateType);
				this.options.NullTypeSigAllowed = true;
			}
			SetValueFromConstructor(value);
		}

		protected override string OnNewValue(IList<T> value) {
			types.Clear();
			if (!(value is null))
				types.AddRange(value);
			return CalculateStringValue();
		}

		string CalculateStringValue() => string.Join(", ", types.Select(a => TypeSigVM.ToString(a)));
		void InitializeStringValue() => StringValue = CalculateStringValue();

		protected override string? ConvertToValue(out IList<T> value) {
			value = types.ToArray();
			return null;
		}

		void AddType() {
			if (typeSigCreator is null)
				throw new InvalidOperationException();
			if (options is null)
				throw new InvalidOperationException();
			var newTypeSig = typeSigCreator.Create(options, null, out bool canceled);
			if (canceled)
				return;

			types.Add(ToType(newTypeSig!));
			InitializeStringValue();
		}

		void RemoveType() {
			if (!RemoveTypeCanExecute())
				return;

			types.RemoveAt(types.Count - 1);
			InitializeStringValue();
		}

		bool RemoveTypeCanExecute() => types.Count > 0;

		void ClearTypes() {
			if (!ClearTypesCanExecute())
				return;

			types.Clear();
			InitializeStringValue();
		}

		bool ClearTypesCanExecute() => types.Count > 0;
		protected abstract T ToType(TypeSig type);
	}

	sealed class TypeSigListDataFieldVM : TypeListDataFieldVMBase<TypeSig> {
		public TypeSigListDataFieldVM(Action<DataFieldVM> onUpdated, TypeSigCreatorOptions? options)
			: this(Array.Empty<TypeSig>(), onUpdated, options) {
		}

		public TypeSigListDataFieldVM(IList<TypeSig> value, Action<DataFieldVM> onUpdated, TypeSigCreatorOptions? options)
			: base(value, onUpdated, options) {
		}

		protected override TypeSig ToType(TypeSig type) => type;
	}

	sealed class TypeDefOrRefListDataFieldVM : TypeListDataFieldVMBase<ITypeDefOrRef> {
		public TypeDefOrRefListDataFieldVM(Action<DataFieldVM> onUpdated, TypeSigCreatorOptions options)
			: this(Array.Empty<ITypeDefOrRef>(), onUpdated, options) {
		}

		public TypeDefOrRefListDataFieldVM(IList<ITypeDefOrRef> value, Action<DataFieldVM> onUpdated, TypeSigCreatorOptions options)
			: base(value, onUpdated, options) {
		}

		protected override ITypeDefOrRef ToType(TypeSig type) => type.ToTypeDefOrRef();
	}
}
