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
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Scripting.Debugger;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerField : IDebuggerField {
		public string Name { get; }
		public FieldAttributes Attributes { get; }
		public FieldAttributes Access => Attributes & FieldAttributes.FieldAccessMask;
		public bool IsCompilerControlled => IsPrivateScope;
		public bool IsPrivateScope => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.PrivateScope;
		public bool IsPrivate => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private;
		public bool IsFamilyAndAssembly => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamANDAssem;
		public bool IsAssembly => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly;
		public bool IsFamily => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family;
		public bool IsFamilyOrAssembly => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamORAssem;
		public bool IsPublic => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public;
		public bool IsStatic => (Attributes & FieldAttributes.Static) != 0;
		public bool IsInitOnly => (Attributes & FieldAttributes.InitOnly) != 0;
		public bool IsLiteral => (Attributes & FieldAttributes.Literal) != 0;
		public bool IsNotSerialized => (Attributes & FieldAttributes.NotSerialized) != 0;
		public bool IsSpecialName => (Attributes & FieldAttributes.SpecialName) != 0;
		public bool IsPinvokeImpl => (Attributes & FieldAttributes.PinvokeImpl) != 0;
		public bool IsRuntimeSpecialName => (Attributes & FieldAttributes.RTSpecialName) != 0;
		public bool HasFieldMarshal => (Attributes & FieldAttributes.HasFieldMarshal) != 0;
		public bool HasDefault => (Attributes & FieldAttributes.HasDefault) != 0;
		public bool HasFieldRVA => (Attributes & FieldAttributes.HasFieldRVA) != 0;

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

		public IDebuggerModule Module => debugger.Dispatcher.UI(() => debugger.FindModuleUI(field.Module));

		public IDebuggerClass Class => debugger.Dispatcher.UI(() => {
			var cls = field.Class;
			return cls == null ? null : new DebuggerClass(debugger, cls);
		});

		public uint Token => token;

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

		public DebuggerField(Debugger debugger, CorField field) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.field = field;
			this.hashCode = field.GetHashCode();
			this.token = field.Token;
			this.Name = field.GetName() ?? string.Empty;
			this.Attributes = field.GetAttributes();
		}

		public IDebuggerValue ReadStatic(IStackFrame frame, IDebuggerType type) {
			if (type != null)
				return type.ReadStaticField(frame, token);
			return debugger.Dispatcher.UI(() => {
				var v = field.ReadStatic(((StackFrame)frame).CorFrame);
				return v == null ? null : new DebuggerValue(debugger, v);
			});
		}

		public override bool Equals(object obj) => (obj as DebuggerField)?.field == field;
		public override int GetHashCode() => hashCode;
		const TypePrinterFlags DEFAULT_FLAGS = TypePrinterFlags.ShowNamespaces | TypePrinterFlags.ShowTypeKeywords;
		public void WriteTo(IOutputWriter output) => Write(output, (TypeFormatFlags)DEFAULT_FLAGS);
		public void Write(IOutputWriter output, TypeFormatFlags flags) =>
			debugger.Dispatcher.UI(() => field.Write(new OutputWriterConverter(output), (TypePrinterFlags)flags));
		public string ToString(TypeFormatFlags flags) => debugger.Dispatcher.UI(() => field.ToString((TypePrinterFlags)flags));
		public override string ToString() => debugger.Dispatcher.UI(() => field.ToString(DEFAULT_FLAGS));
	}
}
