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

using System.Linq;
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

		public IDebuggerType ToType(IDebuggerType[] typeArgs) {
			return debugger.Dispatcher.UI(() => {
				// We can use Class all the time, even for value types
				var type = cls.GetParameterizedType(dndbg.COM.CorDebug.CorElementType.Class, typeArgs.ToCorTypes());
				return type == null ? null : new DebuggerType(debugger, type);
			});
		}

		public IDebuggerValue GetStaticFieldValue(uint token, IStackFrame frame) {
			return debugger.Dispatcher.UI(() => {
				var value = cls.GetStaticFieldValue(token, ((StackFrame)frame).CorFrame);
				return value == null ? null : new DebuggerValue(debugger, value);
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
				var funcs = cls.FindFunctions(name, checkBaseClasses).ToList();
				var res = new IDebuggerFunction[funcs.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerFunction(debugger, funcs[i]);
				return res;
			});
		}

		public IDebuggerFunction[] FindConstructors() {
			return debugger.Dispatcher.UI(() => {
				var ctors = cls.FindConstructors();
				var res = new IDebuggerFunction[ctors.Length];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerFunction(debugger, ctors[i]);
				return res;
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
