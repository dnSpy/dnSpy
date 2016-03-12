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

		public IEnumerable<IDebuggerType> TypeParameters {
			get { return debugger.Dispatcher.UIIter(GetTypeParametersUI); }
		}

		IEnumerable<IDebuggerType> GetTypeParametersUI() {
			foreach (var t in type.TypeParameters)
				yield return new DebuggerType(debugger, t);
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
