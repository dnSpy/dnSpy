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
	sealed class DebuggerEvent : IDebuggerEvent {
		public string Name {
			get { return name; }
		}

		public EventAttributes Attributes {
			get { return attributes; }
		}
		readonly EventAttributes attributes;

		public bool IsSpecialName {
			get { return (attributes & EventAttributes.SpecialName) != 0; }
		}

		public bool IsRuntimeSpecialName {
			get { return (attributes & EventAttributes.RTSpecialName) != 0; }
		}

		public IDebuggerType EventType {
			get {
				if (evtTypeInitd)
					return evtType;
				return debugger.Dispatcher.UI(() => {
					if (evtTypeInitd)
						return evtType;
					var et = evt.GetEventType();
					if (et != null)
						evtType = new DebuggerType(debugger, et);
					evtTypeInitd = true;
					return evtType;
				});
			}
		}
		IDebuggerType evtType;
		bool evtTypeInitd;

		public IDebuggerModule Module {
			get { return debugger.Dispatcher.UI(() => debugger.FindModuleUI(evt.Module)); }
		}

		public IDebuggerClass Class {
			get {
				return debugger.Dispatcher.UI(() => {
					var cls = evt.Class;
					return cls == null ? null : new DebuggerClass(debugger, cls);
				});
			}
		}

		public uint Token {
			get { return token; }
		}

		public IDebuggerMethod Adder {
			get {
				if (adderInitd)
					return adder;
				return debugger.Dispatcher.UI(() => {
					if (adderInitd)
						return adder;
					var func = evt.AddMethod;
					adder = func == null ? null : new DebuggerMethod(debugger, func);
					adderInitd = true;
					return adder;
				});
			}
		}
		bool adderInitd;
		IDebuggerMethod adder;

		public IDebuggerMethod Invoker {
			get {
				if (invokerInitd)
					return invoker;
				return debugger.Dispatcher.UI(() => {
					if (invokerInitd)
						return invoker;
					var func = evt.FireMethod;
					invoker = func == null ? null : new DebuggerMethod(debugger, func);
					invokerInitd = true;
					return invoker;
				});
			}
		}
		bool invokerInitd;
		IDebuggerMethod invoker;

		public IDebuggerMethod Remover {
			get {
				if (removerInitd)
					return remover;
				return debugger.Dispatcher.UI(() => {
					if (removerInitd)
						return remover;
					var func = evt.RemoveMethod;
					remover = func == null ? null : new DebuggerMethod(debugger, func);
					removerInitd = true;
					return remover;
				});
			}
		}
		bool removerInitd;
		IDebuggerMethod remover;

		public IDebuggerMethod[] OtherMethods {
			get {
				if (otherMethods != null)
					return otherMethods;
				return debugger.Dispatcher.UI(() => {
					if (otherMethods != null)
						return otherMethods;
					var tokens = evt.GetOtherMethods();
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
		readonly CorEvent evt;
		readonly string name;

		public DebuggerEvent(Debugger debugger, CorEvent evt) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.evt = evt;
			this.hashCode = evt.GetHashCode();
			this.token = evt.Token;
			this.name = evt.GetName() ?? string.Empty;
			this.attributes = evt.GetAttributes();
		}

		public override bool Equals(object obj) {
			var other = obj as DebuggerEvent;
			return other != null && other.evt == evt;
		}

		public override int GetHashCode() {
			return hashCode;
		}

		public void Write(ISyntaxHighlightOutput output, TypeFormatFlags flags) {
			debugger.Dispatcher.UI(() => evt.Write(new OutputConverter(output), (TypePrinterFlags)flags));
		}

		public string ToString(TypeFormatFlags flags) {
			return debugger.Dispatcher.UI(() => evt.ToString((TypePrinterFlags)flags));
		}

		public override string ToString() {
			const TypePrinterFlags flags = TypePrinterFlags.ShowParameterTypes |
				TypePrinterFlags.ShowReturnTypes | TypePrinterFlags.ShowNamespaces |
				TypePrinterFlags.ShowTypeKeywords;
			return debugger.Dispatcher.UI(() => evt.ToString(flags));
		}
	}
}
