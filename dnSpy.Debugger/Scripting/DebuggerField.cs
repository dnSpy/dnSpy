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

using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Scripting.Debugger;
using dnSpy.Shared.Scripting;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerField : IDebuggerField {
		public string Name {
			get { return name; }
		}

		public FieldAttributes Attributes {
			get { return attributes; }
		}
		readonly FieldAttributes attributes;

		public FieldAttributes Access {
			get { return attributes & FieldAttributes.FieldAccessMask; }
		}

		public bool IsCompilerControlled {
			get { return IsPrivateScope; }
		}

		public bool IsPrivateScope {
			get { return (attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.PrivateScope; }
		}

		public bool IsPrivate {
			get { return (attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private; }
		}

		public bool IsFamilyAndAssembly {
			get { return (attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamANDAssem; }
		}

		public bool IsAssembly {
			get { return (attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly; }
		}

		public bool IsFamily {
			get { return (attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family; }
		}

		public bool IsFamilyOrAssembly {
			get { return (attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamORAssem; }
		}

		public bool IsPublic {
			get { return (attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public; }
		}

		public bool IsStatic {
			get { return (attributes & FieldAttributes.Static) != 0; }
		}

		public bool IsInitOnly {
			get { return (attributes & FieldAttributes.InitOnly) != 0; }
		}

		public bool IsLiteral {
			get { return (attributes & FieldAttributes.Literal) != 0; }
		}

		public bool IsNotSerialized {
			get { return (attributes & FieldAttributes.NotSerialized) != 0; }
		}

		public bool IsSpecialName {
			get { return (attributes & FieldAttributes.SpecialName) != 0; }
		}

		public bool IsPinvokeImpl {
			get { return (attributes & FieldAttributes.PinvokeImpl) != 0; }
		}

		public bool IsRuntimeSpecialName {
			get { return (attributes & FieldAttributes.RTSpecialName) != 0; }
		}

		public bool HasFieldMarshal {
			get { return (attributes & FieldAttributes.HasFieldMarshal) != 0; }
		}

		public bool HasDefault {
			get { return (attributes & FieldAttributes.HasDefault) != 0; }
		}

		public bool HasFieldRVA {
			get { return (attributes & FieldAttributes.HasFieldRVA) != 0; }
		}

		public FieldSig FieldSig {
			get {
				if (fieldSig != null)
					return fieldSig;
				return debugger.Dispatcher.UI(() => {
					if (fieldSig != null)
						return fieldSig;
					fieldSig = field.GetFieldSig();
					if (fieldSig == null)
						fieldSig = new FieldSig(new CorLibTypes(new ModuleDefUser()).Void);
					return fieldSig;
				});
			}
		}
		FieldSig fieldSig;

		public IDebuggerModule Module {
			get { return debugger.Dispatcher.UI(() => debugger.FindModuleUI(field.Module)); }
		}

		public IDebuggerClass Class {
			get {
				return debugger.Dispatcher.UI(() => {
					var cls = field.Class;
					return cls == null ? null : new DebuggerClass(debugger, cls);
				});
			}
		}

		public uint Token {
			get { return token; }
		}

		public object Constant {
			get {
				if (constantInitd)
					return constant;
				return debugger.Dispatcher.UI(() => {
					if (constantInitd)
						return constant;

					constant = field.GetConstant();
					constantInitd = true;
					return constant;
				});
			}
		}
		object constant;
		bool constantInitd;

		readonly Debugger debugger;
		readonly int hashCode;
		readonly uint token;
		readonly CorField field;
		readonly string name;

		public DebuggerField(Debugger debugger, CorField field) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.field = field;
			this.hashCode = field.GetHashCode();
			this.token = field.Token;
			this.name = field.GetName() ?? string.Empty;
			this.attributes = field.GetAttributes();
		}

		public IDebuggerValue ReadStatic(IStackFrame frame, IDebuggerType type) {
			if (type != null)
				return type.ReadStaticField(frame, token);
			return debugger.Dispatcher.UI(() => {
				var v = field.ReadStatic(((StackFrame)frame).CorFrame);
				return v == null ? null : new DebuggerValue(debugger, v);
			});
		}

		public override bool Equals(object obj) {
			var other = obj as DebuggerField;
			return other != null && other.field == field;
		}

		public override int GetHashCode() {
			return hashCode;
		}

		public void Write(ISyntaxHighlightOutput output, TypeFormatFlags flags) {
			debugger.Dispatcher.UI(() => field.Write(new OutputConverter(output), (TypePrinterFlags)flags));
		}

		public string ToString(TypeFormatFlags flags) {
			return debugger.Dispatcher.UI(() => field.ToString((TypePrinterFlags)flags));
		}

		public override string ToString() {
			const TypePrinterFlags flags = TypePrinterFlags.ShowNamespaces | TypePrinterFlags.ShowTypeKeywords;
			return debugger.Dispatcher.UI(() => field.ToString(flags));
		}
	}
}
