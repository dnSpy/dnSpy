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

using System.Collections.Generic;
using System.Linq;
using dndbg.Engine;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Scripting.Debugger;
using dnSpy.Shared.Scripting;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerType : IDebuggerType {
		public IDebuggerType BaseType {
			get {
				return debugger.Dispatcher.UI(() => {
					var b = type.Base;
					return b == null ? null : new DebuggerType(debugger, b);
				});
			}
		}

		public IDebuggerClass Class {
			get {
				return debugger.Dispatcher.UI(() => {
					var cls = type.Class;
					return cls == null ? null : new DebuggerClass(debugger, cls);
				});
			}
		}

		public bool DerivesFromSystemValueType {
			get { return debugger.Dispatcher.UI(() => type.DerivesFromSystemValueType); }
		}

		public CorElementType ElementType {
			get { return elementType; }
		}

		public CorElementType ElementTypeOrEnumUnderlyingType {
			get { return debugger.Dispatcher.UI(() => (CorElementType)type.TypeOrEnumUnderlyingType); }
		}

		public CorElementType EnumUnderlyingType {
			get { return debugger.Dispatcher.UI(() => (CorElementType)type.EnumUnderlyingType); }
		}

		public IDebuggerType FirstTypeParameter {
			get {
				return debugger.Dispatcher.UI(() => {
					var b = type.FirstTypeParameter;
					return b == null ? null : new DebuggerType(debugger, b);
				});
			}
		}

		public bool IsAnyArray {
			get { return elementType == CorElementType.SZArray || elementType == CorElementType.Array; }
		}

		public bool IsArray {
			get { return elementType == CorElementType.Array; }
		}

		public bool IsByRef {
			get { return elementType == CorElementType.ByRef; }
		}

		public bool IsEnum {
			get { return debugger.Dispatcher.UI(() => type.IsEnum); }
		}

		public bool IsFnPtr {
			get { return elementType == CorElementType.FnPtr; }
		}

		public bool IsGenericInst {
			get { return elementType == CorElementType.GenericInst; }
		}

		public bool IsPtr {
			get { return elementType == CorElementType.Ptr; }
		}

		public bool IsSystemEnum {
			get { return debugger.Dispatcher.UI(() => type.IsSystemEnum); }
		}

		public bool IsSystemNullable {
			get { return debugger.Dispatcher.UI(() => type.IsSystemNullable); }
		}

		public bool IsSystemObject {
			get { return debugger.Dispatcher.UI(() => type.IsSystemObject); }
		}

		public bool IsSystemValueType {
			get { return debugger.Dispatcher.UI(() => type.IsSystemValueType); }
		}

		public bool IsSZArray {
			get { return elementType == CorElementType.SZArray; }
		}

		public bool IsValueType {
			get { return debugger.Dispatcher.UI(() => type.IsValueType); }
		}

		public uint Rank {
			get { return debugger.Dispatcher.UI(() => type.Rank); }
		}

		public IDebuggerType[] TypeParameters {
			get {
				return debugger.Dispatcher.UI(() => {
					var list = new List<IDebuggerType>();
					foreach (var t in type.TypeParameters)
						list.Add(new DebuggerType(debugger, t));
					return list.ToArray();
				});
			}
		}

		internal CorType CorType {
			get { return type; }
		}

		readonly Debugger debugger;
		readonly CorType type;
		readonly int hashCode;
		readonly CorElementType elementType;

		public DebuggerType(Debugger debugger, CorType type) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.type = type;
			this.hashCode = type.GetHashCode();
			this.elementType = (CorElementType)type.ElementType;
		}

		public bool HasAttribute(string attributeName) {
			return debugger.Dispatcher.UI(() => type.HasAttribute(attributeName));
		}

		public bool IsSystem(string name) {
			return debugger.Dispatcher.UI(() => type.IsSystem(name));
		}

		public CorElementType TryGetPrimitiveType() {
			return debugger.Dispatcher.UI(() => (CorElementType)type.TryGetPrimitiveType());
		}

		public IDebuggerValue GetStaticFieldValue(uint token, IStackFrame frame) {
			return debugger.Dispatcher.UI(() => {
				var value = type.GetStaticFieldValue(token, ((StackFrame)frame).CorFrame);
				return value == null ? null : new DebuggerValue(debugger, value);
			});
		}

		CorAppDomain CorAppDomain {
			get {
				var t = type;
				int i = 0;
				while (t != null && !t.HasClass && i++ < 1000)
					t = t.FirstTypeParameter;
				var cls = t == null ? null : t.Class;
				var mod = cls == null ? null : cls.Module;
				var asm = mod == null ? null : mod.Assembly;
				return asm == null ? null : asm.AppDomain;
			}
		}

		public IDebuggerType ToPtr() {
			return debugger.Dispatcher.UI(() => {
				var ad = CorAppDomain;
				var res = ad == null ? null : ad.GetPtr(type);
				return res == null ? null : new DebuggerType(debugger, res);
			});
		}

		public IDebuggerType ToPointer() {
			return ToPtr();
		}

		public IDebuggerType ToByRef() {
			return debugger.Dispatcher.UI(() => {
				var ad = CorAppDomain;
				var res = ad == null ? null : ad.GetByRef(type);
				return res == null ? null : new DebuggerType(debugger, res);
			});
		}

		public IDebuggerType ToByReference() {
			return ToByRef();
		}

		public IDebuggerType ToSZArray() {
			return debugger.Dispatcher.UI(() => {
				var ad = CorAppDomain;
				var res = ad == null ? null : ad.GetSZArray(type);
				return res == null ? null : new DebuggerType(debugger, res);
			});
		}

		public IDebuggerType ToArray(int rank) {
			return debugger.Dispatcher.UI(() => {
				var ad = CorAppDomain;
				var res = ad == null ? null : ad.GetArray(type, (uint)rank);
				return res == null ? null : new DebuggerType(debugger, res);
			});
		}

		public IDebuggerFunction FindMethod(string name, bool checkBaseClasses) {
			return debugger.Dispatcher.UI(() => {
				var methods = FindMethods(name, checkBaseClasses);
				foreach (var m in methods) {
					if (m.MethodSig.Params.Count == 0)
						return m;
				}
				return methods.Length == 1 ? methods[0] : null;
			});
		}

		public IDebuggerFunction FindMethod(string name, params object[] argTypes) {
			return FindMethod(name, true, argTypes);
		}

		public IDebuggerFunction FindMethod(string name, bool checkBaseClasses, params object[] argTypes) {
			return debugger.Dispatcher.UI(() => {
				var comparer = new TypeComparer();
				foreach (var m in FindMethods(name, checkBaseClasses)) {
					if (comparer.ArgListsEquals(m.MethodSig.Params, argTypes))
						return m;
				}
				return null;
			});
		}

		public IDebuggerFunction[] FindMethods(string name, bool checkBaseClasses) {
			return debugger.Dispatcher.UI(() => {
				var funcs = type.FindFunctions(name, checkBaseClasses).ToList();
				var res = new IDebuggerFunction[funcs.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerFunction(debugger, funcs[i]);
				return res;
			});
		}

		public IDebuggerFunction[] FindConstructors() {
			return debugger.Dispatcher.UI(() => {
				if (!type.HasClass)
					return new IDebuggerFunction[0];
				var cls = Class;
				return cls == null ? new IDebuggerFunction[0] : cls.FindConstructors();
			});
		}

		public IDebuggerFunction FindConstructor() {
			return FindConstructor(emptyArgTypes);
		}
		static readonly object[] emptyArgTypes = new object[0];

		public IDebuggerFunction FindConstructor(params object[] argTypes) {
			return debugger.Dispatcher.UI(() => {
				var comparer = new TypeComparer();
				foreach (var m in FindConstructors()) {
					if (comparer.ArgListsEquals(m.MethodSig.Params, argTypes))
						return m;
				}
				return null;
			});
		}

		public override bool Equals(object obj) {
			var other = obj as DebuggerType;
			return other != null && other.type == type;
		}

		public override int GetHashCode() {
			return hashCode;
		}

		public void Write(ISyntaxHighlightOutput output, TypeFormatFlags flags) {
			debugger.Dispatcher.UI(() => type.Write(new OutputConverter(output), (TypePrinterFlags)flags));
		}

		public string ToString(TypeFormatFlags flags) {
			return debugger.Dispatcher.UI(() => type.ToString((TypePrinterFlags)flags));
		}

		public override string ToString() {
			return debugger.Dispatcher.UI(() => type.ToString());
		}
	}
}
