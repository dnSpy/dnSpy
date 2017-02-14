/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Diagnostics;
using System.Linq;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Old.Properties;
using ICSharpCode.TreeView;

namespace dnSpy.Debugger.Locals {
	interface IEditableValue : INotifyPropertyChanged {
		bool CanEdit { get; }
		bool IsEditingValue { get; set; }
		string GetValueAsText();
		string SetValueAsText(string newText);
	}

	abstract class ValueVM : SharpTreeNode, IEditableValue, IDisposable {
		public object NameObject => this;
		public object ValueObject => this;
		public object TypeObject => this;

		public bool IsEditingValue {
			get { return isEditingValue; }
			set {
				if (isEditingValue != value) {
					isEditingValue = value;
					RaisePropertyChanged(nameof(IsEditingValue));
				}
			}
		}
		bool isEditingValue;

		public virtual bool CanEdit => false;
		protected abstract ImageReference IconReference { get; }
		public IPrinterContext PrinterContext => context.LocalsOwner.PrinterContext;
		public sealed override bool ShowIcon => true;

		public sealed override object Icon => IconReference;

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

		protected void UpdateCachedOutputValue() {
			if (cachedOutputValue == null || !HasPropertyChangedHandlers)
				InvalidateValueObject();
			else {
				var newCachedOutputValue = CreateCachedOutputValue();
				if (newCachedOutputValue.Equals(cachedOutputValue.Value))
					return;

				cachedOutputValue = newCachedOutputValue;
				RaisePropertyChanged(nameof(ValueObject));
			}
		}

		protected void UpdateCachedOutputType() {
			if (cachedOutputType == null || !HasPropertyChangedHandlers)
				InvalidateTypeObject();
			else {
				var newCachedOutputType = CreateCachedOutputType();
				if (newCachedOutputType.Equals(cachedOutputType.Value))
					return;

				cachedOutputType = newCachedOutputType;
				RaisePropertyChanged(nameof(TypeObject));
			}
		}

		protected virtual CachedOutput CreateCachedOutputValue() => CachedOutput.Create();
		protected virtual CachedOutput CreateCachedOutputType() => CachedOutput.Create();
		public abstract void WriteName(ITextColorWriter output);

		protected ValueContext context;
		protected TypePrinterFlags TypePrinterFlags => context.LocalsOwner.PrinterContext.TypePrinterFlags;

		internal void RefreshTypeFields() {
			InvalidateValueObject();
			InvalidateTypeObject();
		}

		internal void RefreshHexFields() {
			InvalidateValueObject();
			InvalidateTypeObject();
		}

		internal void RefreshThemeFields() {
			RaisePropertyChanged(nameof(NameObject));
			InvalidateValueObject();
			InvalidateTypeObject();
		}

		internal void RefreshSyntaxHighlightFields() => RefreshThemeFields();
		internal void RefreshToStringFields() => InvalidateValueObject();

		protected void InvalidateValueObject() {
			cachedOutputValue = null;
			RaisePropertyChanged(nameof(ValueObject));
		}

		protected void InvalidateTypeObject() {
			cachedOutputType = null;
			RaisePropertyChanged(nameof(TypeObject));
		}

		public string GetValueAsText() {
			Debug.Assert(CanEdit);
			if (!CanEdit)
				return null;

			return CachedOutputValue.ToString();
		}

		public virtual string SetValueAsText(string newText) => dnSpy_Debugger_Resources.LocalsEditValue_Error_FieldCanNotBeEdited;

		protected void ClearAndDisposeChildren() => ClearAndDisposeChildren(this);

		internal static void ClearAndDisposeChildren(SharpTreeNode node, bool includeSelf = false) {
			var nodes = (includeSelf ? node.DescendantsAndSelf() : node.Descendants()).ToArray();
			node.Children.Clear();
			foreach (var n in nodes) {
				if (n is IDisposable id)
					id.Dispose();
			}
		}

		internal static void DisposeAndRemoveAt(SharpTreeNode node, int index) {
			var child = node.Children[index];
			node.Children.RemoveAt(index);
			ClearAndDisposeChildren(child, true);
		}

		public virtual void Dispose() { }
	}

	sealed class MessageValueVM : ValueVM {
		protected override ImageReference IconReference => DsImages.StatusError;

		public static MessageValueVM CreateError(ValueContext context, string msg) => new MessageValueVM(context, msg);

		readonly string msg;

		MessageValueVM(ValueContext context, string msg) {
			this.context = context;
			this.msg = msg;
		}

		public override void WriteName(ITextColorWriter output) =>
			output.Write(BoxedTextColor.Error, msg);
	}

	sealed class LiteralFieldValueVM : ValueVM {
		protected override ImageReference IconReference => FieldValueType.GetIconName(info.OwnerType, info.Attributes);
		public object Constant => info.Constant;

		/*readonly*/ CorFieldInfo info;
		readonly bool overridden;

		public LiteralFieldValueVM(ValueContext context, CorFieldInfo info, bool overridden) {
			this.info = info;
			this.overridden = overridden;
			Reinitialize(context);
		}

		public void Reinitialize(ValueContext context) => this.context = context;
		protected override CachedOutput CreateCachedOutputValue() => CachedOutput.CreateConstant(info.FieldType, info.Constant, TypePrinterFlags);
		protected override CachedOutput CreateCachedOutputType() => CachedOutput.Create(info.FieldType, TypePrinterFlags);
		public override void WriteName(ITextColorWriter output) =>
			FieldValueType.WriteName(output, info.Name, BoxedTextColor.LiteralField, info.OwnerType, overridden);
	}

	/// <summary>
	/// Base class of classes that can read and possibly write <see cref="CorValue"/>s, i.e.,
	/// used by locals, args, exception objects, array elements, fields etc... The derived classes
	/// are the only classes allowed to write to the <see cref="CorValue"/>.
	/// </summary>
	abstract class NormalValueVM : ValueVM {
		protected const int ERROR_PropertyEvalDisabled = -2;
		protected const int ERROR_EvalTimedOut = -3;
		protected const int ERROR_EvalDisabledTimedOut = -4;
		protected const int ERROR_CantEvaluate = -5;

		public override bool CanEdit => ReadOnlyCorValue != null && valueType.CanEdit;
		protected override ImageReference IconReference => CorValueError ? DsImages.StatusError : valueType.IconReference;

		bool CorValueError {
			get { return corValueError; }
			set {
				if (corValueError != value) {
					corValueError = value;
					RaisePropertyChanged(nameof(Icon));
					RaisePropertyChanged(nameof(ExpandedIcon));
				}
			}
		}
		bool corValueError;

		public static bool IsType<T>(SharpTreeNode node) where T : NormalValueType =>
			(node as NormalValueVM)?.NormalValueType is T;

		/// <summary>
		/// Gets a read-only <see cref="CorValue"/> object. The caller must not write to the value
		/// since it could be the return value of a property method call.
		/// </summary>
		public CorValue ReadOnlyCorValue {
			get {
				hr_ReadOnlyCorValue = GetReadOnlyCorValue(out var value);
				CorValueError = hr_ReadOnlyCorValue != 0 || value == null;
				return value;
			}
		}
		int hr_ReadOnlyCorValue;

		bool GetReadOnlyCorValueNullable(CorValue value, out CorValue nullableValue, out bool nullableIsNull) {
			nullableValue = null;
			nullableIsNull = false;
			if (value == null)
				return false;
			var et = value.ExactType;
			if (et == null)
				return false;
			if (!et.GetSystemNullableFields(out var hasValueInfo, out var valueInfo, out var nullableElemType))
				return false;
			var hasValueValue = value.GetFieldValue(et.Class, hasValueInfo.Token);
			var valueValue = value.GetFieldValue(et.Class, valueInfo.Token);
			if (hasValueValue == null || valueValue == null || hasValueValue.ElementType != CorElementType.Boolean || hasValueValue.Size != 1)
				return false;
			var res = hasValueValue.Value;
			if (!res.IsValid || !(res.Value is bool))
				return false;

			nullableIsNull = !(bool)res.Value;
			nullableValue = valueValue;
			return true;
		}

		protected abstract int GetReadOnlyCorValue(out CorValue value);

		protected sealed override CachedOutput CreateCachedOutputValue() => CachedOutput.CreateValue(ReadOnlyCorValue, TypePrinterFlags, () => context.LocalsOwner.CreateEval(context));

		protected sealed override CachedOutput CreateCachedOutputType() {
			var value = ReadOnlyCorValue;
			if (type is TypeSig ts)
				return CachedOutput.CreateType(value, ts, context.GenericTypeArguments, context.GenericMethodArguments, TypePrinterFlags);
			if (type is CorType ct)
				return CachedOutput.CreateType(value, ct, TypePrinterFlags);
			if (type is CorClass cc)
				return CachedOutput.CreateType(value, cc, TypePrinterFlags);
			return CachedOutput.CreateType(value, TypePrinterFlags);
		}

		// Called only if we should keep the field/property type, i.e., should be called by
		// FieldValueVM and PropertyValueVM only.
		protected void ReinitializeInternal(ValueContext newContext) => ReinitializeInternal(newContext, type);

		protected void ReinitializeInternal(ValueContext newContext, object newType) {
			context = newContext;
			type = newType;

			UpdateCachedOutputValue();
			UpdateCachedOutputType();

			InitializeChildren();
			Debug.Assert((LazyLoading && loadChildrenDel != null) || (!LazyLoading && loadChildrenDel == null));
			Debug.Assert(!LazyLoading || Children.Count == 0);
		}

		protected void WriteLazyLoading(bool value) {
			if (LazyLoading != value)
				LazyLoading = value;
		}

		protected override void LoadChildren() {
			Debug.Assert(loadChildrenDel != null);
			var del = loadChildrenDel;
			loadChildrenDel = null;
			del?.Invoke();
		}

		void InitializeChildren() {
			var v = ReadOnlyCorValue;
			bool isNullable = GetReadOnlyCorValueNullable(v, out var nullableValue, out bool nullableIsNull);
			loadChildrenDel = null;

			// If eg. the array has been collapsed, forget about all the children. This speeds up
			// the code when stepping, and will save some memory if it's a big array or a class with
			// many children.
			if (Children.Count > 0 && !IsExpanded)
				ClearAndDisposeChildren();

			if (v == null) {
				ClearAndDisposeChildren();
				var msg = dnSpy_Debugger_Resources.Locals_Error_CouldNotRecreateTheValue;
				if (hr_ReadOnlyCorValue == CordbgErrors.CORDBG_E_STATIC_VAR_NOT_AVAILABLE)
					msg = dnSpy_Debugger_Resources.Locals_Error_StaticFieldIsNotYetAvailable;//TODO: Do something about it. VS has no problem showing the value
				else if (hr_ReadOnlyCorValue == ERROR_PropertyEvalDisabled)
					msg = dnSpy_Debugger_Resources.Locals_Error_PropertyFuncEvalHasBeenDisabledInSettings;
				else if (hr_ReadOnlyCorValue == ERROR_EvalTimedOut)
					msg = dnSpy_Debugger_Resources.Locals_Error_EvaluationTimedOut;
				else if (hr_ReadOnlyCorValue == ERROR_EvalDisabledTimedOut)
					msg = EVAL_DISABLED_TIMEDOUT_ERROR_MSG;
				else if (hr_ReadOnlyCorValue == ERROR_CantEvaluate)
					msg = EVAL_DISABLED_CANT_CALL_PROPS_METHS;
				else if (CordbgErrors.IsCantEvaluateError(hr_ReadOnlyCorValue))
					msg = dnSpy_Debugger_Resources.Locals_Error_CantEvaluateWhenThreadIsAtUnsafePoint;
				Children.Add(MessageValueVM.CreateError(context, msg));
				WriteLazyLoading(false);
				childrenState = null;
				return;
			}

			if (isNullable) {
				if (nullableIsNull) {
					InitializeNullReference(nullableValue);
					return;
				}
				// Don't show the nullable's value and hasValue fields
				v = nullableValue;
				Debug.Assert(v != null);
			}

			// Check if it's a simple type: integer, floating point number, ptr, decimal, string
			var res = v.Value;
			if (res.IsValid && res.Value != null) {
				ClearAndDisposeChildren();
				WriteLazyLoading(false);
				childrenState = null;
				return;
			}

			if (v.IsReference) {
				if (v.IsNull) {
					InitializeNullReference(v);
					return;
				}
				v = v.NeuterCheckDereferencedValue ?? v;
			}
			if (v.IsReference) {
				if (v.IsNull) {
					InitializeNullReference(v);
					return;
				}
				v = v.NeuterCheckDereferencedValue ?? v;
			}
			if (v.IsBox)
				v = v.BoxedValue ?? v;

			if (v.IsArray) {
				InitializeArray(v);
				return;
			}
			if (v.IsObject) {
				InitializeObject(v);
				return;
			}

			Debug.Fail(string.Format("Unknown type: {0} ({1})", v, (object)v.ExactType ?? v.Class));
			ClearAndDisposeChildren();
			WriteLazyLoading(false);
			childrenState = null;
		}
		object childrenState = null;
		Action loadChildrenDel = null;

		void InitializeNullReference(CorValue v) {
			// Input is either a reference whose IsNull prop is true or it's a nullable's value
			// field which is a value type and not null
			ClearAndDisposeChildren();
			WriteLazyLoading(false);
			childrenState = null;
		}

		void InitializeObject(CorValue v) {
			Debug.Assert(v.IsObject);
			var et = v.ExactType;
			Debug.Assert(et != null);	// Should only be null if CLR 1.x debugger, unless it's been neutered

			var newState = new ObjectState(et);
			if (!newState.Equals(childrenState))
				ClearAndDisposeChildren();
			childrenState = newState;

			if (Children.Count == 0) {
				if (GetFields(et).Any() || GetProperties(et).Any()) {
					loadChildrenDel = LoadObjectChildren;
					WriteLazyLoading(true);
				}
				else {
					loadChildrenDel = null;
					WriteLazyLoading(false);
				}
			}
			else {
				Debug.Assert(!LazyLoading && loadChildrenDel == null);
				foreach (var child in Children) {
					if (child is FieldValueVM field) {
						field.Reinitialize(context);
						continue;
					}

					if (child is LiteralFieldValueVM lfield) {
						lfield.Reinitialize(context);
						continue;
					}

					if (child is PropertyValueVM prop) {
						prop.Reinitialize(context);
						continue;
					}

					Debug.Fail("Unknown type");
				}
			}
		}

		internal CorValue GetFieldInstanceObject() => GetObjectCorValue();

		CorValue GetObjectCorValue() {
			var v = ReadOnlyCorValue;
			if (GetReadOnlyCorValueNullable(v, out var nullableValue, out bool nullableIsNull)) {
				Debug.Assert(!nullableIsNull);
				if (!nullableIsNull)
					v = nullableValue;
			}
			if (v != null && v.IsReference)
				v = v.NeuterCheckDereferencedValue;
			if (v != null && v.IsReference)
				v = v.NeuterCheckDereferencedValue;
			if (v != null && v.IsBox)
				v = v.BoxedValue;
			return v != null && v.IsObject ? v : null;
		}

		static readonly StringComparer ObjectNameComparer = StringComparer.Ordinal;
		sealed class ObjectValueInfo {
			public string Name;
			public bool Overridden; // true if some derived class has a prop/field with the same name
			public int Index;
			public object CorInfo;	// CorFieldInfo / CorPropertyInfo

			public ObjectValueInfo(int index, string name, object corInfo) {
				Name = name;
				Overridden = false;
				Index = index;
				CorInfo = corInfo;
			}

			public static int SortFunc(ObjectValueInfo a, ObjectValueInfo b) {
				int c = ObjectNameComparer.Compare(a.Name, b.Name);
				if (c != 0)
					return c;
				// The most derived one has a smaller index, sort it after base class' field/prop
				return b.Index.CompareTo(a.Index);
			}

			public static void SortAndMarkOverridden(List<ObjectValueInfo> list) {
				list.Sort(SortFunc);

				for (int i = 1; i < list.Count; i++) {
					var prev = list[i - 1];
					var curr = list[i];
					if (prev.Name == curr.Name)
						prev.Overridden = true;
				}
			}
		}

		IEnumerable<CorFieldInfo> GetFields(CorType et) => et.GetFields().Where(a => {
			// VS2015 adds this to property backing store fields
			if (context.LocalsOwner.DebuggerBrowsableAttributesCanHidePropsFields && a.DebuggerBrowsableState == DebuggerBrowsableState.Never)
				return false;
			// All VS compilers probably add this to all property backing store fields
			if (context.LocalsOwner.CompilerGeneratedAttributesCanHideFields && a.CompilerGeneratedAttribute)
				return false;
			return true;
		});

		IEnumerable<CorPropertyInfo> GetProperties(CorType et) {
			if (!context.LocalsOwner.DebuggerBrowsableAttributesCanHidePropsFields)
				return et.GetProperties();
			return et.GetProperties().Where(a => a.DebuggerBrowsableState == null || a.DebuggerBrowsableState.Value != DebuggerBrowsableState.Never);
		}

		List<ObjectValueInfo> GetObjectValueInfos(CorType et) {
			var list = new List<ObjectValueInfo>();

			var flist = new List<ObjectValueInfo>();
			int index = 0;
			foreach (var info in GetFields(et))
				flist.Add(new ObjectValueInfo(index++, info.Name, info));
			ObjectValueInfo.SortAndMarkOverridden(flist);

			var hash = new HashSet<string>(StringComparer.Ordinal);
			var plist = new List<ObjectValueInfo>();
			index = 0;
			foreach (var info in GetProperties(et)) {
				bool isVirtual = (info.GetMethodAttributes & MethodAttributes.Virtual) != 0;
				if (isVirtual && hash.Contains(info.Name))
					continue;
				plist.Add(new ObjectValueInfo(index++, info.Name, info));
				if (isVirtual)
					hash.Add(info.Name);
			}
			ObjectValueInfo.SortAndMarkOverridden(plist);

			list.AddRange(flist);
			list.AddRange(plist);
			list.Sort(ObjectValueInfo.SortFunc);

			return list;
		}

		void LoadObjectChildren() {
			var v = GetObjectCorValue();
			Debug.Assert(v != null);
			if (v == null)
				return;
			var et = v.ExactType;
			Debug.Assert(et != null);
			if (et == null)
				return;

			foreach (var info in GetObjectValueInfos(et)) {
				if (info.CorInfo is CorFieldInfo finfo) {
					if ((finfo.Attributes & FieldAttributes.Literal) != 0)
						Children.Add(new LiteralFieldValueVM(context, finfo, info.Overridden));
					else {
						var vm = new FieldValueVM(context, finfo, info.Overridden);
						Children.Add(vm);
						vm.Reinitialize(context);
					}
				}
				else if (info.CorInfo is CorPropertyInfo) {
					var vm = new PropertyValueVM(context, (CorPropertyInfo)info.CorInfo, info.Overridden);
					Children.Add(vm);
					vm.Reinitialize(context);
				}
				else {
					Debug.Fail("Unknown type");
				}
			}
		}

		void InitializeArray(CorValue v) {
			Debug.Assert(v.IsArray);

			var newState = new ArrayState(v);
			if (!newState.Equals(childrenState))
				ClearAndDisposeChildren();
			childrenState = newState;

			if (Children.Count == 0) {
				if (v.ArrayCount == 0) {
					loadChildrenDel = null;
					WriteLazyLoading(false);
				}
				else {
					loadChildrenDel = LoadArrayElements;
					WriteLazyLoading(true);
				}
			}
			else {
				Debug.Assert(!LazyLoading && loadChildrenDel == null);
				var et = v.ExactType;
				Debug.Assert(et != null);
				object elemType = et == null ? null : et.FirstTypeParameter;
				for (int i = 0; i < Children.Count; i++) {
					var vmElem = Children[i] as CorValueVM;
					if (vmElem == null) {
						Debug.Assert(i + 1 == Children.Count && i == MAX_ARRAY_ELEMS && Children[i] is MessageValueVM);
						continue;
					}
					uint pos = (uint)i;
					var holder = new CorValueHolder(null, () => {
						var v2 = GetArrayCorValue();
						return v2 == null ? null : v2.GetElementAtPosition(pos);
					});
					vmElem.Reinitialize(context, holder, elemType);
				}
			}
		}

		//TODO: This should be 1000000 but has been lowered because the Children array isn't virtualized at the moment.
		const uint MAX_ARRAY_ELEMS = 10000;
		CorValue GetArrayCorValue() {
			var v = ReadOnlyCorValue;
			if (v != null && v.IsReference)
				v = v.NeuterCheckDereferencedValue;
			if (v != null && v.IsReference)
				v = v.NeuterCheckDereferencedValue;
			return v != null && v.IsArray ? v : null;
		}

		void LoadArrayElements() {
			var state = childrenState as ArrayState;
			Debug.Assert(state != null);
			var v = GetArrayCorValue();
			if (state == null)
				Children.Add(MessageValueVM.CreateError(context, "State is not ArrayState"));
			else if (ReadOnlyCorValue == null)
				Children.Add(MessageValueVM.CreateError(context, "Array has been neutered but couldn't be recreated"));
			else if (v == null || !v.IsArray)
				Children.Add(MessageValueVM.CreateError(context, "Could not find the array"));
			else {
				Debug.Assert(new ArrayState(v).Equals(state));

				uint count = state.Count;
				if (count > MAX_ARRAY_ELEMS) {
					bool showElems = AskUserShowAllArrayElements();
					if (!showElems) {
						loadChildrenDel = LoadArrayElements;
						WriteLazyLoading(true);
						return;
					}
					count = MAX_ARRAY_ELEMS;
				}

				var et = v.ExactType;
				Debug.Assert(et != null);
				object elemType = et == null ? null : et.FirstTypeParameter;

				for (uint i = 0; i < count; i++) {
					uint pos = i;
					var holder = new CorValueHolder(null, () => {
						var v2 = GetArrayCorValue();
						return v2 == null ? null : v2.GetElementAtPosition(pos);
					});
					Children.Add(new CorValueVM(context, holder, elemType, new ArrayElementValueType(i, state)));
				}
				if (state.Count != count)
					Children.Add(MessageValueVM.CreateError(context, "..."));
			}
		}

		bool AskUserShowAllArrayElements() {
			var q = string.Format(dnSpy_Debugger_Resources.Locals_Ask_TooManyItems, MAX_ARRAY_ELEMS);
			return context.LocalsOwner.AskUser(q);
		}

		// This is the arg/local/field type, and doesn't have to be the same type as the value type.
		// Eg. this could be a System.Object and the value type could be a System.String, or any other
		// type that is a sub class of this type or implements this interface (if this is an iface type)
		object type;

		public NormalValueType NormalValueType => valueType;
		NormalValueType valueType;

		protected NormalValueVM() {
		}

		protected NormalValueVM(ValueContext context, object type, NormalValueType valueType) => InitializeFromConstructor(context, type, valueType);

		protected void InitializeFromConstructor(ValueContext context, object type, NormalValueType valueType) {
			this.valueType = valueType;
			Debug.Assert(this.valueType.Owner == null);
			this.valueType.Owner = this;
			this.type = type;
		}

		internal void RaisePropertyChangedInternal(string propName) => RaisePropertyChanged(propName);
		public override void WriteName(ITextColorWriter output) => valueType.WriteName(output);

		public override string SetValueAsText(string newText) {
			if (!CanEdit)
				return dnSpy_Debugger_Resources.LocalsEditValue_Error_ValueCanNotBeEdited;
			var res = SetValueAsTextInternal(new ValueStringParser(newText));
			if (string.IsNullOrEmpty(res))
				context.LocalsOwner.Refresh(this);
			return res;
		}

		protected abstract string SetValueAsTextInternal(ValueStringParser parser);
		public sealed override void Dispose() => CleanUpCorValue();
		protected abstract void CleanUpCorValue();

		protected string WriteNewValue(ValueStringParser parser, Func<CorValue> getValue) {
			var value = getValue();
			if (value == null || value.IsNeutered)
				return dnSpy_Debugger_Resources.Locals_Error_ErrorNeuteredCouldNotBeRecreated;

			if (value.IsReference && value.ElementType == CorElementType.ByRef) {
				var v = value.NeuterCheckDereferencedValue;
				if (v != null)
					value = v;
			}

			var et = value.ExactType;
			if (et.GetSystemNullableFields(out var hasValueInfo, out var valueInfo, out var nullableElemType)) {
				var hasValueValue = value.GetFieldValue(et.Class, hasValueInfo.Token);
				var valueValue = value.GetFieldValue(et.Class, valueInfo.Token);
				Debug.Assert(hasValueValue != null && valueValue != null);
				if (hasValueValue != null && valueValue != null && hasValueValue.ElementType == CorElementType.Boolean && hasValueValue.Size == 1) {
					if (valueValue.Size > 0x00100000)
						return dnSpy_Debugger_Resources.LocalsEditValue_Error_ValueTypeIsTooBig;
					byte[] newHasValueBuf, newValueBuf;
					if (parser.IsNull) {
						newHasValueBuf = new byte[1] { 0 };
						newValueBuf = new byte[valueValue.Size];
					}
					else {
						newHasValueBuf = new byte[1] { 1 };
						var error = parser.GetPrimitiveValue(nullableElemType, out newValueBuf);
						if (!string.IsNullOrEmpty(error))
							return error;
					}

					if (newValueBuf != null && (uint)newValueBuf.Length == valueValue.Size) {
						int hr = hasValueValue.WriteGenericValue(newHasValueBuf);
						if (hr >= 0)
							hr = valueValue.WriteGenericValue(newValueBuf);
						if (hr < 0)
							return string.Format(dnSpy_Debugger_Resources.LocalsEditValue_Error_CouldNotWriteNullToNullableType, hr);
						return null;
					}
				}
			}

			if (value.IsReference && !parser.IsNull) {
				var v = value.NeuterCheckDereferencedValue;
				if (v != null && v.IsBox) {
					v = v.BoxedValue;
					if (v != null)
						value = v;
				}
			}

			if (value.IsGeneric) {
				var error = parser.GetPrimitiveValue(value.ExactType, out var bytes);
				if (!string.IsNullOrEmpty(error))
					return error;
				if (bytes != null) {
					int hr = value.WriteGenericValue(bytes);
					if (hr >= 0)
						return null;
					return string.Format(dnSpy_Debugger_Resources.LocalsEditValue_Error_CouldNotWriteTheValue, hr);
				}
			}

			if (value.IsReference && (value.ElementType == CorElementType.Ptr || value.ElementType == CorElementType.FnPtr)) {
				var error = parser.GetPrimitiveValue(value.ExactType, out var bytes);
				if (!string.IsNullOrEmpty(error))
					return error;
				if (bytes != null) {
					if ((uint)bytes.Length != value.Size)
						return "Wrong buffer size";
					int hr = context.Process.CorProcess.WriteMemory(value.Address, bytes, 0, bytes.Length, out int sizeWritten);
					if (sizeWritten == bytes.Length)
						return null;
					return string.Format(dnSpy_Debugger_Resources.LocalsEditValue_Error_CouldNotWriteTheValue, hr);
				}
			}

			if (value.IsReference && value.ElementType == CorElementType.String) {
				var error = parser.GetString(out string s);
				if (!string.IsNullOrEmpty(error))
					return error;
				if (s == null) {
					value.ReferenceAddress = 0;
					return null;
				}

				error = CreateString(s, out var newStringValue);
				if (!string.IsNullOrEmpty(error))
					return error;
				value = getValue();
				if (value == null || value.IsNeutered)
					return dnSpy_Debugger_Resources.Locals_Error_ErrorNeuteredCouldNotBeRecreated;
				if (value.IsReference && value.ElementType == CorElementType.ByRef)
					value = value.NeuterCheckDereferencedValue;
				if (value == null || value.IsNeutered)
					return dnSpy_Debugger_Resources.Locals_Error_ErrorNeuteredCouldNotBeRecreated;
				value.ReferenceAddress = newStringValue.ReferenceAddress;
				return null;
			}

			if (value.IsReference &&
				(value.ElementType == CorElementType.Class || value.ElementType == CorElementType.Array ||
				value.ElementType == CorElementType.SZArray || value.ElementType == CorElementType.String ||
				value.ElementType == CorElementType.Object)) {
				if (!parser.IsNull)
					return dnSpy_Debugger_Resources.LocalsEditValue_Error_CanOnlyBeSetToNull;
				value.ReferenceAddress = 0;
				return null;
			}

			return dnSpy_Debugger_Resources.LocalsEditValue_Error_CanNotWriteNewValueToThisType;
		}

		protected static readonly string EVAL_DISABLED_TIMEDOUT_ERROR_MSG = dnSpy_Debugger_Resources.Locals_Error_EvalTimedOutIsDisabled;
		protected static readonly string EVAL_DISABLED_CANT_CALL_PROPS_METHS = dnSpy_Debugger_Resources.Locals_Error_EvalDisabledCantCallPropsAndMethods;
		protected string CreateString(string s, out CorValue newString) {
			newString = null;

			if (context.LocalsOwner.TheDebugger.EvalDisabled)
				return dnSpy_Debugger_Resources.Locals_Error_EvalTimedOutCantCreateNewStringsUntilContinue;
			if (!context.LocalsOwner.TheDebugger.CanEvaluate)
				return dnSpy_Debugger_Resources.Locals_Error_CantEvaluateCantCreateStrings;

			int hr;
			EvalResult? res;
			using (var eval = context.LocalsOwner.TheDebugger.CreateEval(context.Thread.CorThread))
				res = eval.CreateString(s, out hr);
			if (res == null)
				return string.Format(dnSpy_Debugger_Resources.Locals_Error_CouldNotCreateString, hr);
			if (res.Value.WasException)
				return dnSpy_Debugger_Resources.Locals_Error_CouldNotCreateStringDueToException;

			newString = res.Value.ResultOrException;
			if (newString == null)
				return dnSpy_Debugger_Resources.Locals_Error_CouldNotCreateString2;

			return null;
		}

		protected static ValueContext CreateValueContext(ValueContext context, CorType type) => new ValueContext(context.LocalsOwner, context.FrameCouldBeNeutered, context.Thread, type.TypeParameters.ToList());
	}

	/// <summary>
	/// Base class that allows writing to <see cref="ReadOnlyCorValue"/>. Used by locals, args,
	/// exception objects, fields.
	/// </summary>
	abstract class WritableCorValueVM : NormalValueVM {
		protected WritableCorValueVM() {
		}

		protected WritableCorValueVM(ValueContext context, object type, NormalValueType valueType)
			: base(context, type, valueType) {
		}

		protected override string SetValueAsTextInternal(ValueStringParser parser) => WriteNewValue(parser, () => ReadOnlyCorValue);
	}

	sealed class CorValueVM : WritableCorValueVM {
		public ICorValueHolder Holder => valueHolder;
		ICorValueHolder valueHolder;

		public CorValueVM(ValueContext context, ICorValueHolder value, object type, NormalValueType valueType)
			: base(context, type, valueType) => Reinitialize(context, value, type);

		public void Reinitialize(ValueContext newContext, ICorValueHolder newValue, object newType) {
			if (valueHolder != null && valueHolder != newValue)
				valueHolder.Dispose();
			valueHolder = newValue;
			base.ReinitializeInternal(newContext, newType);
		}

		protected override int GetReadOnlyCorValue(out CorValue value) {
			value = valueHolder.CorValue;
			return 0;
		}

		protected override void CleanUpCorValue() => valueHolder.InvalidateCorValue();
	}

	sealed class FieldValueVM : WritableCorValueVM {
		public FieldAttributes FieldAttributes { get; }
		public CorType OwnerType { get; }
		public uint Token { get; }
		public bool Overridden { get; }

		public FieldValueVM(ValueContext context, CorFieldInfo info, bool overridden) {
			var valueType = new FieldValueType(info.Name, this);
			FieldAttributes = info.Attributes;
			OwnerType = info.OwnerType;
			Token = info.Token;
			Overridden = overridden;
			InitializeFromConstructor(CreateValueContext(context, OwnerType), info.FieldType, valueType);
		}

		public void Reinitialize(ValueContext newContext) {
			context = CreateValueContext(newContext, OwnerType);
			CleanUpCorValue();
			ReinitializeInternal(context);
		}

		protected override int GetReadOnlyCorValue(out CorValue value) {
			if (this.value == null || this.value.IsNeutered)
				hr_value = InitializeValue();
			value = this.value;
			return hr_value;
		}
		int hr_value;
		CorValue value;

		int InitializeValue() {
			CleanUpCorValue();
			if ((FieldAttributes & FieldAttributes.Static) != 0) {
				value = OwnerType.GetStaticFieldValue(Token, context.FrameCouldBeNeutered, out int hr);
				return hr;
			}
			else {
				var parent = Parent as NormalValueVM;
				Debug.Assert(parent != null);
				if (parent == null)
					return -1;

				var parentValue = parent.GetFieldInstanceObject();
				if (parentValue == null)
					return -1;

				value = parentValue.GetFieldValue(OwnerType.Class, Token, out int hr);
				return hr;
			}
		}

		protected override void CleanUpCorValue() {
			Debug.Assert(context != null);
			if (context != null)
				context.LocalsOwner.TheDebugger.DisposeHandle(value);
			value = null;
		}
	}

	sealed class PropertyValueVM : NormalValueVM {
		public override bool CanEdit => setToken != 0 && base.CanEdit;
		public CorType OwnerType { get; }
		public string Name { get; }
		public TypeSig PropertyType { get; }
		public MethodAttributes GetMethodAttributes { get; }
		public bool Overridden { get; }

		readonly uint getToken;
		readonly uint setToken;

		public PropertyValueVM(ValueContext context, CorPropertyInfo info, bool overridden) {
			var valueType = new PropertyValueType(info.Name, this);
			OwnerType = info.OwnerType;
			Name = info.Name;
			PropertyType = info.GetSig.RetType;
			GetMethodAttributes = info.GetMethodAttributes;
			Overridden = overridden;
			getToken = info.GetToken;
			setToken = info.SetToken;
			InitializeFromConstructor(CreateValueContext(context, OwnerType), info.GetSig.RetType, valueType);
		}

		public void Reinitialize(ValueContext newContext) {
			context = CreateValueContext(newContext, OwnerType);
			CleanUpCorValue();
			ReinitializeInternal(context);
		}

		protected override int GetReadOnlyCorValue(out CorValue value) {
			if (this.value == null || this.value.IsNeutered)
				hr_value = InitializeValue();
			value = this.value;
			return hr_value;
		}
		int hr_value;
		CorValue value;

		CorValue GetOwnerCorValue() {
			var p = Parent as NormalValueVM;
			if (p == null)
				return null;
			return p.ReadOnlyCorValue;
		}

		CorValue GetThisArg() {
			Debug.Assert((GetMethodAttributes & MethodAttributes.Static) == 0);
			return GetOwnerCorValue();
		}

		CorType[] GetTypeArgs() => OwnerType.TypeParameters.ToArray();

		int InitializeValue() {
			CleanUpCorValue();

			if (!context.LocalsOwner.PropertyEvalAndFunctionCalls)
				return ERROR_PropertyEvalDisabled;
			if (context.LocalsOwner.TheDebugger.EvalDisabled)
				return ERROR_EvalDisabledTimedOut;
			if (!context.LocalsOwner.TheDebugger.CanEvaluate)
				return ERROR_CantEvaluate;

			try {
				using (var eval = context.LocalsOwner.TheDebugger.CreateEval(context.Thread.CorThread)) {
					var func = OwnerType.Class.Module.GetFunctionFromToken(getToken);
					CorValue[] args;
					if ((GetMethodAttributes & MethodAttributes.Static) != 0)
						args = Array.Empty<CorValue>();
					else
						args = new CorValue[1] { GetThisArg() };
					var res = eval.Call(func, GetTypeArgs(), args, out int hr);
					if (res == null)
						return hr;
					if (res.Value.WasException) {
						value = res.Value.ResultOrException;
						return -1;
					}
					value = res.Value.ResultOrException;
					return 0;
				}
			}
			catch (TimeoutException) {
				return ERROR_EvalTimedOut;
			}
			catch (Exception ex) {
				Debug.WriteLine("EX: {0}", ex);
				return -1;
			}
		}

		protected override void CleanUpCorValue() {
			Debug.Assert(context != null);
			if (context != null)
				context.LocalsOwner.TheDebugger.DisposeHandle(value);
			value = null;
		}

		protected override string SetValueAsTextInternal(ValueStringParser parser) {
			if (context.LocalsOwner.TheDebugger.EvalDisabled)
				return EVAL_DISABLED_TIMEDOUT_ERROR_MSG;
			if (!context.LocalsOwner.TheDebugger.CanEvaluate)
				return EVAL_DISABLED_CANT_CALL_PROPS_METHS;

			var v = ReadOnlyCorValue;

			bool createNull = false;
			if (v.IsReference && parser.IsNull)
				createNull = true;
			else if (v.IsReference && v.ElementType == CorElementType.String) {
				var error = parser.GetString(out string s);
				if (!string.IsNullOrEmpty(error))
					return error;
				error = CreateString(s, out var newStringValue);
				if (!string.IsNullOrEmpty(error))
					return error;
				v = newStringValue;
			}
			else {
				var error = WriteNewValue(parser, () => v);
				if (!string.IsNullOrEmpty(error))
					return error;
			}

			try {
				if (IsValueType(PropertyType)) {
					if (v.IsReference)
						v = v.NeuterCheckDereferencedValue;
					if (v != null && v.IsBox)
						v = v.BoxedValue;
					if (v == null || !v.IsGeneric)
						return "Internal error: Can't get a value type";
				}

				using (var eval = context.LocalsOwner.TheDebugger.CreateEval(context.Thread.CorThread)) {
					if (createNull)
						v = eval.CreateNull();

					var func = OwnerType.Class.Module.GetFunctionFromToken(setToken);
					CorValue[] args;
					if ((GetMethodAttributes & MethodAttributes.Static) != 0)
						args = new CorValue[1] { v };
					else
						args = new CorValue[2] { GetThisArg(), v };
					var res = eval.Call(func, GetTypeArgs(), args, out int hr);
					if (res == null)
						return string.Format(dnSpy_Debugger_Resources.LocalsEditValue_Error_CouldNotCallPropSetter, hr);
					if (res.Value.WasException) {
						var et = res.Value.ResultOrException?.ExactType;
						return string.Format(dnSpy_Debugger_Resources.LocalsEditValue_Error_ExceptionOccurredInDebuggedProcess, et);
					}
					return null;
				}
			}
			catch (Exception ex) {
				return string.Format(dnSpy_Debugger_Resources.LocalsEditValue_Error_CouldNotWriteValueDueToException, ex.Message);
			}
		}

		static bool IsValueType(TypeSig ts) {
			ts = ts.RemovePinnedAndModifiers();
			if (ts.GetElementType().IsValueType())
				return true;
			if (ts is GenericInstSig gis)
				return gis.GenericType is ValueTypeSig;
			return false;
		}
	}

	abstract class NormalValueType {
		public virtual bool CanEdit => true;
		public abstract ImageReference IconReference { get; }
		public abstract void WriteName(ITextColorWriter output);
		public NormalValueVM Owner {
			get { return owner; }
			set {
				Debug.Assert(owner == null);
				owner = value;
			}
		}
		NormalValueVM owner;
	}

	sealed class LocalValueType : NormalValueType {
		public override ImageReference IconReference => DsImages.FieldPublic;
		public int Index { get; }

		public LocalValueType(int index) => Index = index;

		public void InitializeName(string name) {
			if (this.name != name) {
				this.name = name;
				Owner.RaisePropertyChangedInternal(nameof(Owner.NameObject));
			}
		}
		string name;

		public override void WriteName(ITextColorWriter output) {
			var n = name;
			if (string.IsNullOrEmpty(n))
				n = string.Format("V_{0}", Index);
			output.Write(BoxedTextColor.Local, IdentifierEscaper.Escape(n));
		}
	}

	sealed class ArgumentValueType : NormalValueType {
		public override ImageReference IconReference => DsImages.FieldPublic;
		public int Index { get; }

		public ArgumentValueType(int index) => Index = index;

		public void InitializeName(string name, bool isThis) {
			if (this.name != name || this.isThis != isThis) {
				this.isThis = isThis;
				this.name = name;
				Owner.RaisePropertyChangedInternal(nameof(Owner.NameObject));
			}
		}
		bool isThis;
		string name;

		public override void WriteName(ITextColorWriter output) {
			if (isThis)
				output.Write(BoxedTextColor.Keyword, "this");
			else {
				var n = name;
				if (string.IsNullOrEmpty(n))
					n = string.Format("A_{0}", Index);
				output.Write(BoxedTextColor.Parameter, IdentifierEscaper.Escape(n));
			}
		}
	}

	sealed class ExceptionValueType : NormalValueType {
		public override ImageReference IconReference => DsImages.ExceptionPublic;
		public override bool CanEdit => false;

		public override void WriteName(ITextColorWriter output) =>
			output.Write(BoxedTextColor.Local, "$exception");
	}

	sealed class ArrayElementValueType : NormalValueType {
		public override ImageReference IconReference => DsImages.FieldPublic;

		readonly uint index;
		readonly ArrayState state;

		public ArrayElementValueType(uint index, ArrayState state) {
			this.index = index;
			this.state = state;
		}

		public override void WriteName(ITextColorWriter output) {
			output.Write(BoxedTextColor.Punctuation, "[");

			if (state.Dimensions.Length == 1 && state.Indices.Length == 1 && state.Indices[0] == 0) {
				long i2 = index + (int)state.Indices[0];
				// It's always in decimal
				output.Write(BoxedTextColor.Number, i2.ToString());
			}
			else {
				var ary = new uint[state.Dimensions.Length];
				uint index2 = index;
				for (int i = ary.Length - 1; i >= 0; i--) {
					uint d = state.Dimensions[i];
					if (d != 0) {
						ary[i] = index2 % d;
						index2 /= d;
					}
				}
				for (int i = 0; i < ary.Length; i++) {
					if (i > 0) {
						output.Write(BoxedTextColor.Punctuation, ",");
						output.Write(BoxedTextColor.Text, " ");
					}
					long i2 = ary[i] + (int)state.Indices[i];
					// It's always in decimal
					output.Write(BoxedTextColor.Number, i2.ToString());
				}
			}

			output.Write(BoxedTextColor.Punctuation, "]");
		}
	}

	sealed class FieldValueType : NormalValueType {
		public override ImageReference IconReference => GetIconName(vm.OwnerType, vm.FieldAttributes);
		bool IsEnum => vm.OwnerType.IsEnum;

		readonly string name;
		readonly FieldValueVM vm;

		public FieldValueType(string name, FieldValueVM vm) {
			this.name = name;
			this.vm = vm;
		}

		internal static ImageReference GetIconName(CorType ownerType, FieldAttributes attrs) {
			var access = attrs & FieldAttributes.FieldAccessMask;

			if ((attrs & FieldAttributes.SpecialName) == 0 && ownerType.IsEnum) {
				switch (access) {
				default:
				case FieldAttributes.Public:
					return DsImages.EnumerationItemPublic;
				case FieldAttributes.Private:
					return DsImages.EnumerationItemPrivate;
				case FieldAttributes.Family:
					return DsImages.EnumerationItemProtected;
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return DsImages.EnumerationItemInternal;
				case FieldAttributes.CompilerControlled:
					return DsImages.EnumerationItemSealed;
				case FieldAttributes.FamORAssem:
					return DsImages.EnumerationItemShortcut;
				}
			}
			else if ((attrs & FieldAttributes.Literal) != 0) {
				switch (access) {
				default:
				case FieldAttributes.Public:
					return DsImages.ConstantPublic;
				case FieldAttributes.Private:
					return DsImages.ConstantPrivate;
				case FieldAttributes.Family:
					return DsImages.ConstantProtected;
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return DsImages.ConstantInternal;
				case FieldAttributes.CompilerControlled:
					return DsImages.ConstantSealed;
				case FieldAttributes.FamORAssem:
					return DsImages.ConstantShortcut;
				}
			}
			else {
				switch (access) {
				default:
				case FieldAttributes.Public:
					return DsImages.FieldPublic;
				case FieldAttributes.Private:
					return DsImages.FieldPrivate;
				case FieldAttributes.Family:
					return DsImages.FieldProtected;
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return DsImages.FieldInternal;
				case FieldAttributes.CompilerControlled:
					return DsImages.FieldSealed;
				case FieldAttributes.FamORAssem:
					return DsImages.FieldShortcut;
				}
			}
		}

		public override void WriteName(ITextColorWriter output) =>
			WriteName(output, name, GetTypeColor(), vm.OwnerType, vm.Overridden);

		internal static void WriteName(ITextColorWriter output, string name, object typeColor, CorType ownerType, bool overridden) {
			output.Write(typeColor, IdentifierEscaper.Escape(name));
			if (overridden) {
				output.Write(BoxedTextColor.Text, " ");
				output.Write(BoxedTextColor.Punctuation, "(");
				ownerType.Write(new OutputConverter(output), TypePrinterFlags.Default);
				output.Write(BoxedTextColor.Punctuation, ")");
			}
		}

		object GetTypeColor() {
			if (IsEnum)
				return BoxedTextColor.EnumField;
			if ((vm.FieldAttributes & FieldAttributes.Literal) != 0)
				return BoxedTextColor.LiteralField;
			if ((vm.FieldAttributes & FieldAttributes.Static) != 0)
				return BoxedTextColor.StaticField;
			return BoxedTextColor.InstanceField;
		}
	}

	sealed class PropertyValueType : NormalValueType {
		public override ImageReference IconReference => GetIconName(vm.OwnerType, vm.GetMethodAttributes);

		readonly string name;
		readonly PropertyValueVM vm;

		public PropertyValueType(string name, PropertyValueVM vm) {
			this.name = name;
			this.vm = vm;
		}

		internal static ImageReference GetIconName(CorType ownerType, MethodAttributes attrs) {
			var access = attrs & MethodAttributes.MemberAccessMask;

			switch (access) {
			default:
			case MethodAttributes.Public:
				return DsImages.Property;
			case MethodAttributes.Private:
				return DsImages.PropertyPrivate;
			case MethodAttributes.Family:
				return DsImages.PropertyProtected;
			case MethodAttributes.Assembly:
			case MethodAttributes.FamANDAssem:
				return DsImages.PropertyInternal;
			case MethodAttributes.CompilerControlled:
				return DsImages.PropertySealed;
			case MethodAttributes.FamORAssem:
				return DsImages.PropertyShortcut;
			}
		}

		public override void WriteName(ITextColorWriter output) =>
			FieldValueType.WriteName(output, name, GetTypeColor(), vm.OwnerType, vm.Overridden);

		object GetTypeColor() {
			if ((vm.GetMethodAttributes & MethodAttributes.Static) != 0)
				return BoxedTextColor.StaticProperty;
			return BoxedTextColor.InstanceProperty;
		}
	}

	sealed class ArrayState : IEquatable<ArrayState> {
		public CorElementType ArrayElementType { get; }
		public CorElementType ElementType { get; }
		public uint[] Dimensions { get; }
		public uint[] Indices { get; }
		public uint Count { get; }

		public ArrayState(CorValue v) {
			Debug.Assert(v.IsArray);
			ArrayElementType = v.ElementType;
			ElementType = v.ArrayElementType;
			Dimensions = v.Dimensions;
			Indices = v.BaseIndicies;
			Count = v.ArrayCount;
		}

		public bool Equals(ArrayState other) {
			if (other == null)
				return false;

			if (Count != other.Count)
				return false;
			if (ArrayElementType != other.ArrayElementType)
				return false;
			if (ElementType != other.ElementType)
				return false;
			if (!Equals(Dimensions, other.Dimensions))
				return false;
			if (!Equals(Indices, other.Indices))
				return false;

			return true;
		}

		static bool Equals(uint[] a, uint[] b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		public override bool Equals(object obj) => Equals(obj as ArrayState);

		public override int GetHashCode() => (int)ArrayElementType ^ (int)ElementType ^ Dimensions.Length ^ Indices.Length ^ (int)Count;
	}

	sealed class ObjectState : IEquatable<ObjectState> {
		readonly CorType Type;

		public ObjectState(CorType type) => Type = type;

		public bool Equals(ObjectState other) => other != null && Type == other.Type;
		public override bool Equals(object obj) => Equals(obj as ObjectState);
		public override int GetHashCode() => Type.GetHashCode();
	}

	sealed class GenericVariableValueVM : ValueVM {
		protected override ImageReference IconReference => DsImages.FieldPublic;
		protected sealed override CachedOutput CreateCachedOutputValue() => CachedOutput.Create(type, TypePrinterFlags);
		protected sealed override CachedOutput CreateCachedOutputType() => CreateCachedOutputValue();

		public void Reinitialize(ValueContext newContext, CorType type) {
			context = newContext;
			this.type = type;

			UpdateCachedOutputValue();
			UpdateCachedOutputType();
		}

		public bool IsTypeVar => isTypeVar;
		public bool IsMethodVar => !isTypeVar;

		readonly string name;
		CorType type;
		readonly bool isTypeVar;
		readonly int index;

		public GenericVariableValueVM(ValueContext context, string name, CorType type, bool isTypeVar, int index) {
			this.name = name;
			this.isTypeVar = isTypeVar;
			this.index = index;
			Reinitialize(context, type);
		}

		public override void WriteName(ITextColorWriter output) {
			if (!string.IsNullOrEmpty(name))
				output.Write(isTypeVar ? BoxedTextColor.TypeGenericParameter : BoxedTextColor.MethodGenericParameter, IdentifierEscaper.Escape(name));
			else if (isTypeVar) {
				output.Write(BoxedTextColor.Operator, "!");
				output.Write(BoxedTextColor.Number, string.Format("{0}", index));
			}
			else {
				output.Write(BoxedTextColor.Operator, "!!");
				output.Write(BoxedTextColor.Number, string.Format("{0}", index));
			}
		}
	}

	sealed class TypeVariablesValueVM : ValueVM {
		protected override ImageReference IconReference => DsImages.FieldPublic;

		static string Read(List<TokenAndName> list, int index) {
			if ((uint)index >= (uint)list.Count)
				return null;
			return list[index].Name;
		}

		public void Reinitialize(ValueContext newContext) {
			var oldFunc = context?.Function;
			context = newContext;

			if (oldFunc != context.Function || !CanReuseChildren())
				ClearAndDisposeChildren();

			if (Children.Count == 0) {
				List<TokenAndName> typeParams, methodParams;
				if (context.Function != null)
					context.Function.GetGenericParameters(out typeParams, out methodParams);
				else
					typeParams = methodParams = new List<TokenAndName>();
				for (int i = 0; i < context.GenericTypeArguments.Count; i++)
					Children.Add(new GenericVariableValueVM(newContext, Read(typeParams, i), context.GenericTypeArguments[i], true, i));
				for (int i = 0; i < context.GenericMethodArguments.Count; i++)
					Children.Add(new GenericVariableValueVM(newContext, Read(methodParams, i), context.GenericMethodArguments[i], false, i));
			}
			else {
				int index = 0;
				for (int i = 0; i < context.GenericTypeArguments.Count; i++, index++)
					((GenericVariableValueVM)Children[index]).Reinitialize(newContext, context.GenericTypeArguments[i]);
				for (int i = 0; i < context.GenericMethodArguments.Count; i++, index++)
					((GenericVariableValueVM)Children[index]).Reinitialize(newContext, context.GenericMethodArguments[i]);
				Debug.Assert(index == Children.Count);
			}
		}

		bool CanReuseChildren() {
			int index = 0;

			if (index + context.GenericTypeArguments.Count + context.GenericMethodArguments.Count > Children.Count)
				return false;

			for (int i = 0; i < context.GenericTypeArguments.Count; i++, index++) {
				if (!((GenericVariableValueVM)Children[index]).IsTypeVar)
					return false;
			}
			for (int i = 0; i < context.GenericMethodArguments.Count; i++, index++) {
				if (!((GenericVariableValueVM)Children[index]).IsMethodVar)
					return false;
			}

			return index == Children.Count;
		}

		public TypeVariablesValueVM(ValueContext context) => Reinitialize(context);

		public override void WriteName(ITextColorWriter output) =>
			output.Write(BoxedTextColor.TypeGenericParameter, dnSpy_Debugger_Resources.Locals_TypeVariables);
	}
}
