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
	sealed class DebuggerProperty : IDebuggerProperty {
		public string Name {
			get { return name; }
		}

		public PropertyAttributes Attributes {
			get { return attributes; }
		}
		readonly PropertyAttributes attributes;

		public bool IsSpecialName {
			get { return (attributes & PropertyAttributes.SpecialName) != 0; }
		}

		public bool IsRuntimeSpecialName {
			get { return (attributes & PropertyAttributes.RTSpecialName) != 0; }
		}

		public bool HasDefault {
			get { return (attributes & PropertyAttributes.HasDefault) != 0; }
		}

		public PropertySig PropertySig {
			get {
				if (propSig != null)
					return propSig;
				return debugger.Dispatcher.UI(() => {
					if (propSig != null)
						return propSig;
					propSig = prop.GetPropertySig();
					if (propSig == null)
						propSig = PropertySig.CreateStatic(new CorLibTypes(new ModuleDefUser()).Void);
					return propSig;
				});
			}
		}
		PropertySig propSig;

		public IDebuggerModule Module {
			get { return debugger.Dispatcher.UI(() => debugger.FindModuleUI(prop.Module)); }
		}

		public IDebuggerClass Class {
			get {
				return debugger.Dispatcher.UI(() => {
					var cls = prop.Class;
					return cls == null ? null : new DebuggerClass(debugger, cls);
				});
			}
		}

		public uint Token {
			get { return token; }
		}

		public IDebuggerMethod Getter {
			get {
				if (getterInitd)
					return getter;
				return debugger.Dispatcher.UI(() => {
					if (getterInitd)
						return getter;
					var func = prop.GetMethod;
					getter = func == null ? null : new DebuggerMethod(debugger, func);
					getterInitd = true;
					return getter;
				});
			}
		}
		bool getterInitd;
		IDebuggerMethod getter;

		public IDebuggerMethod Setter {
			get {
				if (setterInitd)
					return setter;
				return debugger.Dispatcher.UI(() => {
					if (setterInitd)
						return setter;
					var func = prop.SetMethod;
					setter = func == null ? null : new DebuggerMethod(debugger, func);
					setterInitd = true;
					return setter;
				});
			}
		}
		bool setterInitd;
		IDebuggerMethod setter;

		public IDebuggerMethod[] OtherMethods {
			get {
				if (otherMethods != null)
					return otherMethods;
				return debugger.Dispatcher.UI(() => {
					if (otherMethods != null)
						return otherMethods;
					var tokens = prop.GetOtherMethods();
					var res = new IDebuggerMethod[tokens.Length];
					for (int i = 0; i < res.Length; i++)
						res[i] = new DebuggerMethod(debugger, tokens[i]);
					otherMethods = res;
					return otherMethods;
				});
			}
		}
		IDebuggerMethod[] otherMethods;

		readonly Debugger debugger;
		readonly int hashCode;
		readonly uint token;
		readonly CorProperty prop;
		readonly string name;

		public DebuggerProperty(Debugger debugger, CorProperty prop) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.prop = prop;
			this.hashCode = prop.GetHashCode();
			this.token = prop.Token;
			this.name = prop.GetName() ?? string.Empty;
			this.attributes = prop.GetAttributes();
		}

		public override bool Equals(object obj) {
			var other = obj as DebuggerProperty;
			return other != null && other.prop == prop;
		}

		public override int GetHashCode() {
			return hashCode;
		}

		public void Write(ISyntaxHighlightOutput output, TypeFormatFlags flags) {
			debugger.Dispatcher.UI(() => prop.Write(new OutputConverter(output), (TypePrinterFlags)flags));
		}

		public string ToString(TypeFormatFlags flags) {
			return debugger.Dispatcher.UI(() => prop.ToString((TypePrinterFlags)flags));
		}

		public override string ToString() {
			const TypePrinterFlags flags = TypePrinterFlags.ShowParameterTypes |
				TypePrinterFlags.ShowReturnTypes | TypePrinterFlags.ShowNamespaces |
				TypePrinterFlags.ShowTypeKeywords;
			return debugger.Dispatcher.UI(() => prop.ToString(flags));
		}
	}
}
