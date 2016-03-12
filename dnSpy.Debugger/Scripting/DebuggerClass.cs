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
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Scripting.Debugger;
using dnSpy.Shared.Scripting;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerClass : IDebuggerClass {
		public bool IsSystemEnum {
			get { return debugger.Dispatcher.UI(() => cls.IsSystemEnum); }
		}

		public bool IsSystemObject {
			get { return debugger.Dispatcher.UI(() => cls.IsSystemObject); }
		}

		public bool IsSystemValueType {
			get { return debugger.Dispatcher.UI(() => cls.IsSystemValueType); }
		}

		public IDebuggerModule Module {
			get {
				if (module != null)
					return module;
				debugger.Dispatcher.UI(() => {
					if (module == null)
						module = debugger.FindModuleUI(cls.Module);
				});
				return module;
			}
		}

		public uint Token {
			get { return token; }
		}

		public bool HasAttribute(string attributeName) {
			return debugger.Dispatcher.UI(() => cls.HasAttribute(attributeName));
		}

		public bool IsSystem(string name) {
			return debugger.Dispatcher.UI(() => cls.IsSystem(name));
		}

		public bool SetJustMyCode(bool jmc) {
			return debugger.Dispatcher.UI(() => cls.SetJustMyCode(jmc));
		}

		internal CorClass CorClass {
			get { return cls; }
		}

		readonly Debugger debugger;
		readonly CorClass cls;
		readonly int hashCode;
		readonly uint token;
		IDebuggerModule module;

		public DebuggerClass(Debugger debugger, CorClass cls) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.cls = cls;
			this.hashCode = cls.GetHashCode();
			this.token = cls.Token;
		}

		public IDebuggerType GetParameterizedType(CorElementType etype, IDebuggerType[] typeArgs) {
			return debugger.Dispatcher.UI(() => {
				var type = cls.GetParameterizedType((dndbg.COM.CorDebug.CorElementType)etype, typeArgs.ToCorType());
				return type == null ? null : new DebuggerType(debugger, type);
			});
		}

		public IDebuggerValue GetStaticFieldValue(uint token, IStackFrame frame) {
			return debugger.Dispatcher.UI(() => {
				var value = cls.GetStaticFieldValue(token, ((StackFrame)frame).CorFrame);
				return value == null ? null : new DebuggerValue(debugger, value);
			});
		}

		public override bool Equals(object obj) {
			var other = obj as DebuggerClass;
			return other != null && other.cls == cls;
		}

		public override int GetHashCode() {
			return hashCode;
		}

		public void Write(ISyntaxHighlightOutput output, TypeFormatFlags flags) {
			debugger.Dispatcher.UI(() => cls.Write(new OutputConverter(output), (TypePrinterFlags)flags));
		}

		public string ToString(TypeFormatFlags flags) {
			return debugger.Dispatcher.UI(() => cls.ToString((TypePrinterFlags)flags));
		}

		public override string ToString() {
			return debugger.Dispatcher.UI(() => cls.ToString());
		}
	}
}
