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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Properties;
using dnSpy.Decompiler.Shared;
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
		protected abstract string IconName { get; }
		public IPrinterContext PrinterContext => context.LocalsOwner.PrinterContext;
		public sealed override object Icon => context.LocalsOwner.PrinterContext.ImageManager.GetImage(GetType().Assembly, IconName, BackgroundType.TreeNode);
		public sealed override bool ShowIcon => true;

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
		public abstract void WriteName(IOutputColorWriter output);

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
			RaisePropertyChanged(nameof(Icon));
			RaisePropertyChanged(nameof(ExpandedIcon));
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
				var id = n as IDisposable;
				if (id != null)
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
		protected override string IconName => "StatusError";

		public static MessageValueVM CreateError(ValueContext context, string msg) => new MessageValueVM(context, msg);

		readonly string msg;

		MessageValueVM(ValueContext context, string msg) {
			this.context = context;
			this.msg = msg;
		}

		public override void WriteName(IOutputColorWriter output) =>
			output.Write(BoxedOutputColor.Error, msg);
	}

	sealed class LiteralFieldValueVM : ValueVM {
		protected override string IconName => FieldValueType.GetIconName(info.OwnerType, info.Attributes);
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
		public override void WriteName(IOutputColorWriter output) =>
			FieldValueType.WriteName(output, info.Name, BoxedOutputColor.LiteralField, info.OwnerType, overridden);
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
		protected override string IconName => CorValueError ? "StatusError" : valueType.IconName;

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
				CorValue value;
				hr_ReadOnlyCorValue = GetReadOnlyCorValue(out value);
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
			TokenAndName hasValueInfo, valueInfo;
			CorType nullableElemType;
			if (!et.GetSystemNullableFields(out hasValueInfo, out valueInfo, out nullableElemType))
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
			var ts = type as TypeSig;
			if (ts != null)
				return CachedOutput.CreateType(value, ts, context.GenericTypeArguments, context.GenericMethodArguments, TypePrinterFlags);
			var ct = type as CorType;
			if (ct != null)
				return CachedOutput.CreateType(value, ct, TypePrinterFlags);
			var cc = type as CorClass;
			if (cc != null)
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
			CorValue nullableValue;
			bool nullableIsNull;
			bool isNullable = GetReadOnlyCorValueNullable(v, out nullableValue, out nullableIsNull);
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
					var field = child as FieldValueVM;
					if (field != null) {
						field.Reinitialize(context);
						continue;
					}

					var lfield = child as LiteralFieldValueVM;
					if (lfield != null) {
						lfield.Reinitialize(context);
						continue;
					}

					var prop = child as PropertyValueVM;
					if (prop != null) {
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
			CorValue nullableValue;
			bool nullableIsNull;
			if (GetReadOnlyCorValueNullable(v, out nullableValue, out nullableIsNull)) {
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
				this.Name = name;
				this.Overridden = false;
				this.Index = index;
				this.CorInfo = corInfo;
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
				if (info.CorInfo is CorFieldInfo) {
					var finfo = (CorFieldInfo)info.CorInfo;
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

		protected NormalValueVM(ValueContext context, object type, NormalValueType valueType) {
			InitializeFromConstructor(context, type, valueType);
		}

		protected void InitializeFromConstructor(ValueContext context, object type, NormalValueType valueType) {
			this.valueType = valueType;
			Debug.Assert(this.valueType.Owner == null);
			this.valueType.Owner = this;
			this.type = type;
		}

		internal void RaisePropertyChangedInternal(string propName) => RaisePropertyChanged(propName);
		public override void WriteName(IOutputColorWriter output) => valueType.WriteName(output);

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
			TokenAndName hasValueInfo, valueInfo;
			CorType nullableElemType;
			if (et.GetSystemNullableFields(out hasValueInfo, out valueInfo, out nullableElemType)) {
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
				byte[] bytes;
				var error = parser.GetPrimitiveValue(value.ExactType, out bytes);
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
				byte[] bytes;
				var error = parser.GetPrimitiveValue(value.ExactType, out bytes);
				if (!string.IsNullOrEmpty(error))
					return error;
				if (bytes != null) {
					if ((uint)bytes.Length != value.Size)
						return "Wrong buffer size";
					int sizeWritten;
					int hr = context.Process.CorProcess.WriteMemory(value.Address, bytes, 0, bytes.Length, out sizeWritten);
					if (sizeWritten == bytes.Length)
						return null;
					return string.Format(dnSpy_Debugger_Resources.LocalsEditValue_Error_CouldNotWriteTheValue, hr);
				}
			}

			if (value.IsReference && value.ElementType == CorElementType.String) {
				string s;
				var error = parser.GetString(out s);
				if (!string.IsNullOrEmpty(error))
					return error;
				if (s == null) {
					value.ReferenceAddress = 0;
					return null;
				}

				CorValue newStringValue;
				error = CreateString(s, out newStringValue);
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

			if (this.context.LocalsOwner.TheDebugger.EvalDisabled)
				return dnSpy_Debugger_Resources.Locals_Error_EvalTimedOutCantCreateNewStringsUntilContinue;
			if (!this.context.LocalsOwner.TheDebugger.CanEvaluate)
				return dnSpy_Debugger_Resources.Locals_Error_CantEvaluateCantCreateStrings;

			int hr;
			EvalResult? res;
			using (var eval = this.context.LocalsOwner.TheDebugger.CreateEval(context.Thread.CorThread))
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
			: base(context, type, valueType) {
			Reinitialize(context, value, type);
		}

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
			this.FieldAttributes = info.Attributes;
			this.OwnerType = info.OwnerType;
			this.Token = info.Token;
			this.Overridden = overridden;
			InitializeFromConstructor(CreateValueContext(context, OwnerType), info.FieldType, valueType);
		}

		public void Reinitialize(ValueContext newContext) {
			this.context = CreateValueContext(newContext, OwnerType);
			CleanUpCorValue();
			ReinitializeInternal(this.context);
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
				int hr;
				value = OwnerType.GetStaticFieldValue(Token, context.FrameCouldBeNeutered, out hr);
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

				int hr;
				value = parentValue.GetFieldValue(OwnerType.Class, Token, out hr);
				return hr;
			}
		}

		protected override void CleanUpCorValue() {
			Debug.Assert(this.context != null);
			if (this.context != null)
				this.context.LocalsOwner.TheDebugger.DisposeHandle(value);
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
			this.OwnerType = info.OwnerType;
			this.Name = info.Name;
			this.PropertyType = info.GetSig.RetType;
			this.GetMethodAttributes = info.GetMethodAttributes;
			this.Overridden = overridden;
			this.getToken = info.GetToken;
			this.setToken = info.SetToken;
			InitializeFromConstructor(CreateValueContext(context, OwnerType), info.GetSig.RetType, valueType);
		}

		public void Reinitialize(ValueContext newContext) {
			this.context = CreateValueContext(newContext, OwnerType);
			CleanUpCorValue();
			ReinitializeInternal(this.context);
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
			if (this.context.LocalsOwner.TheDebugger.EvalDisabled)
				return ERROR_EvalDisabledTimedOut;
			if (!this.context.LocalsOwner.TheDebugger.CanEvaluate)
				return ERROR_CantEvaluate;

			try {
				int hr;
				using (var eval = this.context.LocalsOwner.TheDebugger.CreateEval(context.Thread.CorThread)) {
					var func = OwnerType.Class.Module.GetFunctionFromToken(getToken);
					CorValue[] args;
					if ((GetMethodAttributes & MethodAttributes.Static) != 0)
						args = Array.Empty<CorValue>();
					else
						args = new CorValue[1] { GetThisArg() };
					var res = eval.Call(func, GetTypeArgs(), args, out hr);
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
			Debug.Assert(this.context != null);
			if (this.context != null)
				this.context.LocalsOwner.TheDebugger.DisposeHandle(value);
			value = null;
		}

		protected override string SetValueAsTextInternal(ValueStringParser parser) {
			if (this.context.LocalsOwner.TheDebugger.EvalDisabled)
				return EVAL_DISABLED_TIMEDOUT_ERROR_MSG;
			if (!this.context.LocalsOwner.TheDebugger.CanEvaluate)
				return EVAL_DISABLED_CANT_CALL_PROPS_METHS;

			var v = ReadOnlyCorValue;

			bool createNull = false;
			if (v.IsReference && parser.IsNull)
				createNull = true;
			else if (v.IsReference && v.ElementType == CorElementType.String) {
				string s;
				var error = parser.GetString(out s);
				if (!string.IsNullOrEmpty(error))
					return error;
				CorValue newStringValue;
				error = CreateString(s, out newStringValue);
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

				int hr;
				using (var eval = this.context.LocalsOwner.TheDebugger.CreateEval(context.Thread.CorThread)) {
					if (createNull)
						v = eval.CreateNull();

					var func = OwnerType.Class.Module.GetFunctionFromToken(setToken);
					CorValue[] args;
					if ((GetMethodAttributes & MethodAttributes.Static) != 0)
						args = new CorValue[1] { v };
					else
						args = new CorValue[2] { GetThisArg(), v };
					var res = eval.Call(func, GetTypeArgs(), args, out hr);
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
			var gis = ts as GenericInstSig;
			if (gis != null)
				return gis.GenericType is ValueTypeSig;
			return false;
		}
	}

	abstract class NormalValueType {
		public virtual bool CanEdit => true;
		public abstract string IconName { get; }
		public abstract void WriteName(IOutputColorWriter output);
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
		public override string IconName => "Field";
		public int Index { get; }

		public LocalValueType(int index) {
			this.Index = index;
		}

		public void InitializeName(string name) {
			if (this.name != name) {
				this.name = name;
				Owner.RaisePropertyChangedInternal(nameof(Owner.NameObject));
			}
		}
		string name;

		public override void WriteName(IOutputColorWriter output) {
			var n = name;
			if (string.IsNullOrEmpty(n))
				n = string.Format("V_{0}", Index);
			output.Write(BoxedOutputColor.Local, IdentifierEscaper.Escape(n));
		}
	}

	sealed class ArgumentValueType : NormalValueType {
		public override string IconName => "Field";
		public int Index { get; }

		public ArgumentValueType(int index) {
			this.Index = index;
		}

		public void InitializeName(string name, bool isThis) {
			if (this.name != name || this.isThis != isThis) {
				this.isThis = isThis;
				this.name = name;
				Owner.RaisePropertyChangedInternal(nameof(Owner.NameObject));
			}
		}
		bool isThis;
		string name;

		public override void WriteName(IOutputColorWriter output) {
			if (isThis)
				output.Write(BoxedOutputColor.Keyword, "this");
			else {
				var n = name;
				if (string.IsNullOrEmpty(n))
					n = string.Format("A_{0}", Index);
				output.Write(BoxedOutputColor.Parameter, IdentifierEscaper.Escape(n));
			}
		}
	}

	sealed class ExceptionValueType : NormalValueType {
		public override string IconName => "Exception";
		public override bool CanEdit => false;

		public override void WriteName(IOutputColorWriter output) =>
			output.Write(BoxedOutputColor.Local, "$exception");
	}

	sealed class ArrayElementValueType : NormalValueType {
		public override string IconName => "Field";

		readonly uint index;
		readonly ArrayState state;

		public ArrayElementValueType(uint index, ArrayState state) {
			this.index = index;
			this.state = state;
		}

		public override void WriteName(IOutputColorWriter output) {
			output.Write(BoxedOutputColor.Punctuation, "[");

			if (state.Dimensions.Length == 1 && state.Indices.Length == 1 && state.Indices[0] == 0) {
				long i2 = index + (int)state.Indices[0];
				// It's always in decimal
				output.Write(BoxedOutputColor.Number, i2.ToString());
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
						output.Write(BoxedOutputColor.Punctuation, ",");
						output.Write(BoxedOutputColor.Text, " ");
					}
					long i2 = ary[i] + (int)state.Indices[i];
					// It's always in decimal
					output.Write(BoxedOutputColor.Number, i2.ToString());
				}
			}

			output.Write(BoxedOutputColor.Punctuation, "]");
		}
	}

	sealed class FieldValueType : NormalValueType {
		public override string IconName => GetIconName(vm.OwnerType, vm.FieldAttributes);
		bool IsEnum => vm.OwnerType.IsEnum;

		readonly string name;
		readonly FieldValueVM vm;

		public FieldValueType(string name, FieldValueVM vm) {
			this.name = name;
			this.vm = vm;
		}

		internal static string GetIconName(CorType ownerType, FieldAttributes attrs) {
			var access = attrs & FieldAttributes.FieldAccessMask;

			if ((attrs & FieldAttributes.SpecialName) == 0 && ownerType.IsEnum) {
				switch (access) {
				default:
				case FieldAttributes.Public:
					return "EnumValue";
				case FieldAttributes.Private:
					return "EnumValuePrivate";
				case FieldAttributes.Family:
					return "EnumValueProtected";
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return "EnumValueInternal";
				case FieldAttributes.CompilerControlled:
					return "EnumValueCompilerControlled";
				case FieldAttributes.FamORAssem:
					return "EnumValueProtectedInternal";
				}
			}
			else if ((attrs & FieldAttributes.Literal) != 0) {
				switch (access) {
				default:
				case FieldAttributes.Public:
					return "Literal";
				case FieldAttributes.Private:
					return "LiteralPrivate";
				case FieldAttributes.Family:
					return "LiteralProtected";
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return "LiteralInternal";
				case FieldAttributes.CompilerControlled:
					return "LiteralCompilerControlled";
				case FieldAttributes.FamORAssem:
					return "LiteralProtectedInternal";
				}
			}
			else if ((attrs & FieldAttributes.InitOnly) != 0) {
				switch (access) {
				default:
				case FieldAttributes.Public:
					return "FieldReadOnly";
				case FieldAttributes.Private:
					return "FieldReadOnlyPrivate";
				case FieldAttributes.Family:
					return "FieldReadOnlyProtected";
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return "FieldReadOnlyInternal";
				case FieldAttributes.CompilerControlled:
					return "FieldReadOnlyCompilerControlled";
				case FieldAttributes.FamORAssem:
					return "FieldReadOnlyProtectedInternal";
				}
			}
			else {
				switch (access) {
				default:
				case FieldAttributes.Public:
					return "Field";
				case FieldAttributes.Private:
					return "FieldPrivate";
				case FieldAttributes.Family:
					return "FieldProtected";
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return "FieldInternal";
				case FieldAttributes.CompilerControlled:
					return "FieldCompilerControlled";
				case FieldAttributes.FamORAssem:
					return "FieldProtectedInternal";
				}
			}
		}

		public override void WriteName(IOutputColorWriter output) =>
			WriteName(output, name, GetTypeColor(), vm.OwnerType, vm.Overridden);

		internal static void WriteName(IOutputColorWriter output, string name, object typeColor, CorType ownerType, bool overridden) {
			output.Write(typeColor, IdentifierEscaper.Escape(name));
			if (overridden) {
				output.Write(BoxedOutputColor.Text, " ");
				output.Write(BoxedOutputColor.Punctuation, "(");
				ownerType.Write(new OutputConverter(output), TypePrinterFlags.Default);
				output.Write(BoxedOutputColor.Punctuation, ")");
			}
		}

		object GetTypeColor() {
			if (IsEnum)
				return BoxedOutputColor.EnumField;
			if ((vm.FieldAttributes & FieldAttributes.Literal) != 0)
				return BoxedOutputColor.LiteralField;
			if ((vm.FieldAttributes & FieldAttributes.Static) != 0)
				return BoxedOutputColor.StaticField;
			return BoxedOutputColor.InstanceField;
		}
	}

	sealed class PropertyValueType : NormalValueType {
		public override string IconName => GetIconName(vm.OwnerType, vm.GetMethodAttributes);

		readonly string name;
		readonly PropertyValueVM vm;

		public PropertyValueType(string name, PropertyValueVM vm) {
			this.name = name;
			this.vm = vm;
		}

		internal static string GetIconName(CorType ownerType, MethodAttributes attrs) {
			var access = attrs & MethodAttributes.MemberAccessMask;

			if ((attrs & MethodAttributes.Static) != 0) {
				switch (access) {
				default:
				case MethodAttributes.Public:
					return "StaticProperty";
				case MethodAttributes.Private:
					return "StaticPropertyPrivate";
				case MethodAttributes.Family:
					return "StaticPropertyProtected";
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return "StaticPropertyInternal";
				case MethodAttributes.CompilerControlled:
					return "StaticPropertyCompilerControlled";
				case MethodAttributes.FamORAssem:
					return "StaticPropertyProtectedInternal";
				}
			}

			if ((attrs & MethodAttributes.Virtual) != 0) {
				switch (access) {
				default:
				case MethodAttributes.Public:
					return "VirtualProperty";
				case MethodAttributes.Private:
					return "VirtualPropertyPrivate";
				case MethodAttributes.Family:
					return "VirtualPropertyProtected";
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return "VirtualPropertyInternal";
				case MethodAttributes.CompilerControlled:
					return "VirtualPropertyCompilerControlled";
				case MethodAttributes.FamORAssem:
					return "VirtualPropertyProtectedInternal";
				}
			}

			switch (access) {
			default:
			case MethodAttributes.Public:
				return "Property";
			case MethodAttributes.Private:
				return "PropertyPrivate";
			case MethodAttributes.Family:
				return "PropertyProtected";
			case MethodAttributes.Assembly:
			case MethodAttributes.FamANDAssem:
				return "PropertyInternal";
			case MethodAttributes.CompilerControlled:
				return "PropertyCompilerControlled";
			case MethodAttributes.FamORAssem:
				return "PropertyProtectedInternal";
			}
		}

		public override void WriteName(IOutputColorWriter output) =>
			FieldValueType.WriteName(output, name, GetTypeColor(), vm.OwnerType, vm.Overridden);

		object GetTypeColor() {
			if ((vm.GetMethodAttributes & MethodAttributes.Static) != 0)
				return BoxedOutputColor.StaticProperty;
			return BoxedOutputColor.InstanceProperty;
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
			this.ArrayElementType = v.ElementType;
			this.ElementType = v.ArrayElementType;
			this.Dimensions = v.Dimensions;
			this.Indices = v.BaseIndicies;
			this.Count = v.ArrayCount;
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

		public ObjectState(CorType type) {
			this.Type = type;
		}

		public bool Equals(ObjectState other) => other != null && Type == other.Type;
		public override bool Equals(object obj) => Equals(obj as ObjectState);
		public override int GetHashCode() => Type.GetHashCode();
	}

	sealed class GenericVariableValueVM : ValueVM {
		protected override string IconName => "GenericParameter";
		protected sealed override CachedOutput CreateCachedOutputValue() => CachedOutput.Create(type, TypePrinterFlags);
		protected sealed override CachedOutput CreateCachedOutputType() => CreateCachedOutputValue();

		public void Reinitialize(ValueContext newContext, CorType type) {
			this.context = newContext;
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

		public override void WriteName(IOutputColorWriter output) {
			if (!string.IsNullOrEmpty(name))
				output.Write(isTypeVar ? BoxedOutputColor.TypeGenericParameter : BoxedOutputColor.MethodGenericParameter, IdentifierEscaper.Escape(name));
			else if (isTypeVar) {
				output.Write(BoxedOutputColor.Operator, "!");
				output.Write(BoxedOutputColor.Number, string.Format("{0}", index));
			}
			else {
				output.Write(BoxedOutputColor.Operator, "!!");
				output.Write(BoxedOutputColor.Number, string.Format("{0}", index));
			}
		}
	}

	sealed class TypeVariablesValueVM : ValueVM {
		protected override string IconName => "GenericParameter";

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

		public TypeVariablesValueVM(ValueContext context) {
			Reinitialize(context);
		}

		public override void WriteName(IOutputColorWriter output) =>
			output.Write(BoxedOutputColor.TypeGenericParameter, dnSpy_Debugger_Resources.Locals_TypeVariables);
	}
}
