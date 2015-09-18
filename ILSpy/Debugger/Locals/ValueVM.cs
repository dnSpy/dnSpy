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
using System.ComponentModel;
using System.Diagnostics;
using dndbg.Engine;
using dndbg.Engine.COM.CorDebug;
using dnlib.DotNet;
using dnSpy.Images;
using dnSpy.MVVM;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;

namespace dnSpy.Debugger.Locals {
	interface IEditableValue : INotifyPropertyChanged {
		bool CanEdit { get; }
		bool IsEditingValue { get; set; }
		string GetValueAsText();
		string SetValueAsText(string newText);
	}

	abstract class ValueVM : SharpTreeNode, IEditableValue {
		public object NameObject { get { return this; } }
		public object ValueObject { get { return this; } }
		public object TypeObject { get { return this; } }

		public bool IsEditingValue {
			get { return isEditingValue; }
			set {
				if (isEditingValue != value) {
					isEditingValue = value;
					RaisePropertyChanged("IsEditingValue");
				}
			}
		}
		bool isEditingValue;

		public bool CanEdit {
			get {
				return true;//TODO:
			}
		}

		public CachedOutput CachedOutputValue {
			get {
				if (cachedOutputValue == null)
					cachedOutputValue = CreateCachedOutputValue();
				return cachedOutputValue.Value;
			}
		}
		CachedOutput? cachedOutputValue;

		public CachedOutput CachedOutputType {
			get {
				if (cachedOutputType == null)
					cachedOutputType = CreateCachedOutputType();
				return cachedOutputType.Value;
			}
		}
		CachedOutput? cachedOutputType;

		CachedOutput CreateCachedOutputValue() {
			return CachedOutput.CreateValue(value.CorValue, LocalsVM.TypePrinterFlags);
		}

		CachedOutput CreateCachedOutputType() {
			var ts = type as TypeSig;
			if (ts != null)
				return CachedOutput.CreateType(value.CorValue, ts, context.GenericTypeArguments, context.GenericMethodArguments, LocalsVM.TypePrinterFlags);
			return CachedOutput.CreateType(value.CorValue, LocalsVM.TypePrinterFlags);
		}

		public sealed override object Icon {
			get { return ImageCache.Instance.GetImage(IconName, BackgroundType.TreeNode); }
		}

		public override bool ShowIcon {
			get { return true; }
		}

		public abstract void WriteName(ITextOutput output);

		public void Reinitialize(ValueContext newContext, ICorValueHolder newValue, object newType) {
			context = newContext;
			value = newValue;
			type = newType;

			if (cachedOutputValue == null || !HasPropertyChangedHandlers)
				InvalidateValueObject();
			else {
				var newCachedOutputValue = CreateCachedOutputValue();
				if (newCachedOutputValue.Equals(cachedOutputValue.Value))
					return;

				cachedOutputValue = newCachedOutputValue;
				RaisePropertyChanged("ValueObject");
			}

			if (cachedOutputType == null || !HasPropertyChangedHandlers)
				InvalidateTypeObject();
			else {
				var newCachedOutputType = CreateCachedOutputType();
				if (newCachedOutputType.Equals(cachedOutputType.Value))
					return;

				cachedOutputType = newCachedOutputType;
				RaisePropertyChanged("TypeObject");
			}

			if (Children.Count > 0) {
				//TODO: foreach child => child.Reinitialize(newContext, newChildValue, new ChildType)
			}
		}

		protected abstract string IconName { get; }

		public CorValue Value {
			get { return value.CorValue; }
		}
		ICorValueHolder value;

		// This is the arg/local/field type, and doesn't have to be the same type as the value type.
		// Eg. this could be a System.Object and the value type could be a System.String, or any other
		// type that is a sub class of this type or implement this interface (if this is an iface type)
		object type;

		public ValueContext Context {
			get { return context; }
		}
		ValueContext context;

		protected ValueVM(ValueContext context, ICorValueHolder value, object type) {
			this.Reinitialize(context, value, type);
		}

		internal void RefreshTypeFields() {
			InvalidateValueObject();
			InvalidateTypeObject();
		}

		internal void RefreshHexFields() {
			InvalidateValueObject();
			InvalidateTypeObject();
		}

		internal void RefreshThemeFields() {
			RaisePropertyChanged("Icon");
			RaisePropertyChanged("ExpandedIcon");
			RaisePropertyChanged("NameObject");
			InvalidateValueObject();
			InvalidateTypeObject();
		}

		internal void RefreshSyntaxHighlightFields() {
			RefreshThemeFields();
		}

		void InvalidateValueObject() {
			cachedOutputValue = null;
			RaisePropertyChanged("ValueObject");
		}

		void InvalidateTypeObject() {
			cachedOutputType = null;
			RaisePropertyChanged("TypeObject");
		}

		public string GetValueAsText() {
			Debug.Assert(CanEdit);
			if (!CanEdit)
				return null;

			return CachedOutputValue.ToString();
		}

		public string SetValueAsText(string newText) {
			var res = SetValueAsTextInternal(newText);
			InvalidateValueObject();
			InvalidateTypeObject();
			return res;
		}

		string SetValueAsTextInternal(string newText) {
			if (!CanEdit)
				return "This value can't be edited";

			var value = Value;
			if (value == null)
				return "The value has been neutered and couldn't be recreated";

			if (value.IsGeneric) {
				byte[] bytes;
				var error = GetNewSimpleValue(value, newText, out bytes);
				if (!string.IsNullOrEmpty(error))
					return error;
				if (bytes != null) {
					int hr = value.WriteGenericValue(bytes);
					if (hr >= 0)
						return null;
					return string.Format("Could not write the value. Error: 0x{0:X8}", hr);
				}
			}

			if (value.IsReference &&
				(value.Type == CorElementType.Class || value.Type == CorElementType.Array ||
				value.Type == CorElementType.SZArray || value.Type == CorElementType.String)) {
				if (newText.Trim() != "null")
					return "You can only set it to null";
				value.ReferenceAddress = 0;
				return null;
			}

			return "NYI! Can't write a new value to this type.";//TODO:
		}

		static string GetNewSimpleValue(CorValue value, string newText, out byte[] bytes) {
			//TODO: Use the C# parser to parse the value
			bytes = null;
			string error;
			switch (value.Type) {
			case CorElementType.Boolean:
				{
					var v = NumberVMUtils.ParseBoolean(newText, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(v);
					break;
				}

			case CorElementType.Char:
				{
					var v = NumberVMUtils.ParseChar(newText, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(v);
					break;
				}

			case CorElementType.I1:
				{
					var v = NumberVMUtils.ParseSByte(newText, sbyte.MinValue, sbyte.MaxValue, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(v);
					break;
				}

			case CorElementType.U1:
				{
					var v = NumberVMUtils.ParseByte(newText, byte.MinValue, byte.MaxValue, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(v);
					break;
				}

			case CorElementType.I2:
				{
					var v = NumberVMUtils.ParseInt16(newText, short.MinValue, short.MaxValue, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(v);
					break;
				}

			case CorElementType.U2:
				{
					var v = NumberVMUtils.ParseUInt16(newText, ushort.MinValue, ushort.MaxValue, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(v);
					break;
				}

			case CorElementType.I4:
				{
					var v = NumberVMUtils.ParseInt32(newText, int.MinValue, int.MaxValue, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(v);
					break;
				}

			case CorElementType.U4:
				{
					var v = NumberVMUtils.ParseUInt32(newText, uint.MinValue, uint.MaxValue, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(v);
					break;
				}

			case CorElementType.I8:
				{
					var v = NumberVMUtils.ParseInt64(newText, long.MinValue, long.MaxValue, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(v);
					break;
				}

			case CorElementType.U8:
				{
					var v = NumberVMUtils.ParseUInt64(newText, ulong.MinValue, ulong.MaxValue, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(v);
					break;
				}

			case CorElementType.R4:
				{
					var v = NumberVMUtils.ParseSingle(newText, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(v);
					break;
				}

			case CorElementType.R8:
				{
					var v = NumberVMUtils.ParseDouble(newText, out error);
					if (!string.IsNullOrEmpty(error))
						return error;
					bytes = BitConverter.GetBytes(v);
					break;
				}

			case CorElementType.I:
			case CorElementType.U:
			case CorElementType.Ptr:
				{
					if (IntPtr.Size == 4) {
						uint v = (uint)NumberVMUtils.ParseInt32(newText, int.MinValue, int.MaxValue, out error);
						if (!string.IsNullOrEmpty(error))
							v = NumberVMUtils.ParseUInt32(newText, uint.MinValue, uint.MaxValue, out error);
						if (!string.IsNullOrEmpty(error))
							return error;
						bytes = BitConverter.GetBytes(v);
					}
					else {
						ulong v = (ulong)NumberVMUtils.ParseInt64(newText, long.MinValue, long.MaxValue, out error);
						if (!string.IsNullOrEmpty(error))
							v = NumberVMUtils.ParseUInt64(newText, ulong.MinValue, ulong.MaxValue, out error);
						if (!string.IsNullOrEmpty(error))
							return error;
						bytes = BitConverter.GetBytes(v);
					}
				}
				break;
			}

			return null;
		}
	}

	sealed class LocalValueVM : ValueVM {
		protected override string IconName {
			get { return "Field"; }
		}

		public int Index {
			get { return index; }
		}
		readonly int index;

		public LocalValueVM(ValueContext context, ICorValueHolder value, object type, int index)
			: base(context, value, type) {
			this.index = index;
		}

		public void InitializeName(string name) {
			if (this.name != name) {
				this.name = name;
				RaisePropertyChanged("NameObject");
			}
		}
		string name;

		public override void WriteName(ITextOutput output) {
			var n = name;
			if (string.IsNullOrEmpty(n))
				n = string.Format("V_{0}", index);
			output.Write(IdentifierEscaper.Escape(n), TextTokenType.Local);
		}
	}

	sealed class ArgumentValueVM : ValueVM {
		protected override string IconName {
			get { return "Field"; }
		}

		public int Index {
			get { return index; }
		}
		readonly int index;

		public ArgumentValueVM(ValueContext context, ICorValueHolder value, object type, int index)
			: base(context, value, type) {
			this.index = index;
		}

		public void InitializeName(string name, bool isThis) {
			if (this.name != name || this.isThis != isThis) {
				this.isThis = isThis;
				this.name = name;
				RaisePropertyChanged("NameObject");
			}
		}
		bool isThis;
		string name;

		public override void WriteName(ITextOutput output) {
			if (isThis)
				output.Write("this", TextTokenType.Keyword);
			else {
				var n = name;
				if (string.IsNullOrEmpty(n))
					n = string.Format("A_{0}", index);
				output.Write(IdentifierEscaper.Escape(n), TextTokenType.Parameter);
			}
		}
	}
}
