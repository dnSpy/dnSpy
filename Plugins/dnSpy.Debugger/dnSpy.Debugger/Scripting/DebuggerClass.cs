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
using System.Diagnostics;
using System.Linq;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Scripting.Debugger;
using SR = System.Reflection;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerClass : IDebuggerClass {
		public string Name {
			get {
				if (name != null)
					return name;
				return debugger.Dispatcher.UI(() => {
					if (name != null)
						return name;
					InitializeNamespaceName();
					return name;
				});
			}
		}
		void InitializeNamespaceName() {
			debugger.Dispatcher.VerifyAccess();
			Debug.Assert(name == null);
			string tmpNs, tmpName;
			CorClass.GetName(out tmpNs, out tmpName);
			@namespace = tmpNs;
			name = tmpName ?? string.Empty;
		}
		string name, @namespace;

		public string Namespace {
			get {
				if (name != null)
					return @namespace;
				return debugger.Dispatcher.UI(() => {
					if (name != null)
						return @namespace;
					InitializeNamespaceName();
					return @namespace;
				});
			}
		}

		public string FullName => ToString();
		public TypeAttributes Attributes { get; }
		public TypeAttributes Visibility => Attributes & TypeAttributes.VisibilityMask;
		public bool IsNotPublic => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic;
		public bool IsPublic => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public;
		public bool IsNestedPublic => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic;
		public bool IsNestedPrivate => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate;
		public bool IsNestedFamily => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily;
		public bool IsNestedAssembly => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly;
		public bool IsNestedFamilyAndAssembly => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamANDAssem;
		public bool IsNestedFamilyOrAssembly => (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamORAssem;
		public TypeAttributes Layout => Attributes & TypeAttributes.LayoutMask;
		public bool IsAutoLayout => (Attributes & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout;
		public bool IsSequentialLayout => (Attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout;
		public bool IsExplicitLayout => (Attributes & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout;
		public bool IsInterface => (Attributes & TypeAttributes.Interface) != 0;
		public bool IsClass => (Attributes & TypeAttributes.Interface) == 0;
		public bool IsAbstract => (Attributes & TypeAttributes.Abstract) != 0;
		public bool IsSealed => (Attributes & TypeAttributes.Sealed) != 0;
		public bool IsSpecialName => (Attributes & TypeAttributes.SpecialName) != 0;
		public bool IsImport => (Attributes & TypeAttributes.Import) != 0;
		public bool IsSerializable => (Attributes & TypeAttributes.Serializable) != 0;
		public bool IsWindowsRuntime => (Attributes & TypeAttributes.WindowsRuntime) != 0;
		public TypeAttributes StringFormat => Attributes & TypeAttributes.StringFormatMask;
		public bool IsAnsiClass => (Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AnsiClass;
		public bool IsUnicodeClass => (Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass;
		public bool IsAutoClass => (Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AutoClass;
		public bool IsCustomFormatClass => (Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.CustomFormatClass;
		public bool IsBeforeFieldInit => (Attributes & TypeAttributes.BeforeFieldInit) != 0;
		public bool IsForwarder => (Attributes & TypeAttributes.Forwarder) != 0;
		public bool IsRuntimeSpecialName => (Attributes & TypeAttributes.RTSpecialName) != 0;
		public bool HasSecurity => (Attributes & TypeAttributes.HasSecurity) != 0;
		public bool IsSystemEnum => debugger.Dispatcher.UI(() => CorClass.IsSystemEnum);
		public bool IsSystemObject => debugger.Dispatcher.UI(() => CorClass.IsSystemObject);
		public bool IsSystemValueType => debugger.Dispatcher.UI(() => CorClass.IsSystemValueType);

		public IDebuggerModule Module {
			get {
				if (module != null)
					return module;
				debugger.Dispatcher.UI(() => {
					if (module == null)
						module = debugger.FindModuleUI(CorClass.Module);
				});
				return module;
			}
		}

		public IDebuggerType BaseType => debugger.Dispatcher.UI(() => ToType(Array.Empty<IDebuggerType>())?.BaseType);
		public uint Token { get; }
		public bool HasAttribute(string attributeName) => debugger.Dispatcher.UI(() => CorClass.HasAttribute(attributeName));
		public bool IsSystem(string name) => debugger.Dispatcher.UI(() => CorClass.IsSystem(name));
		public bool SetJustMyCode(bool jmc) => debugger.Dispatcher.UI(() => CorClass.SetJustMyCode(jmc));
		internal CorClass CorClass { get; }

		readonly Debugger debugger;
		readonly int hashCode;
		IDebuggerModule module;

		public DebuggerClass(Debugger debugger, CorClass cls) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.CorClass = cls;
			this.hashCode = cls.GetHashCode();
			this.Token = cls.Token;
			this.Attributes = cls.GetTypeAttributes();
		}

		public IDebuggerType ToType(IDebuggerType[] typeArgs) => debugger.Dispatcher.UI(() => {
			// We can use Class all the time, even for value types
			var type = CorClass.GetParameterizedType(dndbg.COM.CorDebug.CorElementType.Class, typeArgs.ToCorTypes());
			return type == null ? null : new DebuggerType(debugger, type, this.Token);
		});

		public IDebuggerValue ReadStaticField(IStackFrame frame, uint token) => debugger.Dispatcher.UI(() => {
			var value = CorClass.GetStaticFieldValue(token, ((StackFrame)frame).CorFrame);
			return value == null ? null : new DebuggerValue(debugger, value);
		});

		public IDebuggerValue ReadStaticField(IStackFrame frame, IDebuggerField field) => ReadStaticField(frame, field.Token);

		public IDebuggerValue ReadStaticField(IStackFrame frame, string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var field = GetField(name, checkBaseClasses);
			return field == null ? null : ReadStaticField(frame, field.Token);
		});

		public IDebuggerMethod[] Methods {
			get {
				if (methods != null)
					return methods;
				return debugger.Dispatcher.UI(() => {
					if (methods != null)
						return methods;
					var funcs = CorClass.FindFunctions(false).ToList();
					var res = new IDebuggerMethod[funcs.Count];
					for (int i = 0; i < res.Length; i++)
						res[i] = new DebuggerMethod(debugger, funcs[i]);
					methods = res;
					return methods;
				});
			}
		}
		IDebuggerMethod[] methods;

		public IDebuggerField[] Fields {
			get {
				if (fields != null)
					return fields;
				return debugger.Dispatcher.UI(() => {
					if (fields != null)
						return fields;
					var flds = CorClass.FindFields(false).ToList();
					var res = new IDebuggerField[flds.Count];
					for (int i = 0; i < res.Length; i++)
						res[i] = new DebuggerField(debugger, flds[i]);
					fields = res;
					return fields;
				});
			}
		}
		IDebuggerField[] fields;

		public IDebuggerProperty[] Properties {
			get {
				if (properties != null)
					return properties;
				return debugger.Dispatcher.UI(() => {
					if (properties != null)
						return properties;
					var props = CorClass.FindProperties(false).ToList();
					var res = new IDebuggerProperty[props.Count];
					for (int i = 0; i < res.Length; i++)
						res[i] = new DebuggerProperty(debugger, props[i]);
					properties = res;
					return properties;
				});
			}
		}
		IDebuggerProperty[] properties;

		public IDebuggerEvent[] Events {
			get {
				if (events != null)
					return events;
				return debugger.Dispatcher.UI(() => {
					if (events != null)
						return events;
					var evts = CorClass.FindEvents(false).ToList();
					var res = new IDebuggerEvent[evts.Count];
					for (int i = 0; i < res.Length; i++)
						res[i] = new DebuggerEvent(debugger, evts[i]);
					events = res;
					return events;
				});
			}
		}
		IDebuggerEvent[] events;

		public IDebuggerMethod[] GetMethods(bool checkBaseClasses) {
			if (!checkBaseClasses)
				return Methods;
			return debugger.Dispatcher.UI(() => {
				var funcs = CorClass.FindFunctions(checkBaseClasses).ToList();
				var res = new IDebuggerMethod[funcs.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerMethod(debugger, funcs[i]);
				return res;
			});
		}

		public IDebuggerMethod[] GetMethods(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var funcs = CorClass.FindFunctions(name, checkBaseClasses).ToList();
			var res = new IDebuggerMethod[funcs.Count];
			for (int i = 0; i < res.Length; i++)
				res[i] = new DebuggerMethod(debugger, funcs[i]);
			return res;
		});

		public IDebuggerMethod GetMethod(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var methods = GetMethods(name, checkBaseClasses);
			foreach (var m in methods) {
				if (m.MethodSig.Params.Count == 0)
					return m;
			}
			return methods.Length == 1 ? methods[0] : null;
		});

		public IDebuggerMethod GetMethod(string name, params object[] argTypes) => GetMethod(name, true, argTypes);

		public IDebuggerMethod GetMethod(string name, bool checkBaseClasses, params object[] argTypes) => debugger.Dispatcher.UI(() => {
			var comparer = new TypeComparer();
			foreach (var m in GetMethods(name, checkBaseClasses)) {
				if (comparer.ArgListsEquals(m.MethodSig.Params, argTypes))
					return m;
			}
			return null;
		});

		public IDebuggerMethod[] GetConstructors() => debugger.Dispatcher.UI(() => {
			var ctors = CorClass.FindConstructors();
			var res = new IDebuggerMethod[ctors.Length];
			for (int i = 0; i < res.Length; i++)
				res[i] = new DebuggerMethod(debugger, ctors[i]);
			return res;
		});

		public IDebuggerMethod GetConstructor() => GetConstructor(Array.Empty<object>());

		public IDebuggerMethod GetConstructor(params object[] argTypes) => debugger.Dispatcher.UI(() => {
			var comparer = new TypeComparer();
			foreach (var m in GetConstructors()) {
				if (comparer.ArgListsEquals(m.MethodSig.Params, argTypes))
					return m;
			}
			return null;
		});

		public IDebuggerField[] GetFields(bool checkBaseClasses) {
			if (!checkBaseClasses)
				return Fields;
			return debugger.Dispatcher.UI(() => {
				var fields = CorClass.FindFields(checkBaseClasses).ToList();
				var res = new IDebuggerField[fields.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerField(debugger, fields[i]);
				return res;
			});
		}

		public IDebuggerField[] GetFields(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var fields = CorClass.FindFields(name, checkBaseClasses).ToList();
			var res = new IDebuggerField[fields.Count];
			for (int i = 0; i < res.Length; i++)
				res[i] = new DebuggerField(debugger, fields[i]);
			return res;
		});

		public IDebuggerField GetField(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var field = CorClass.FindField(name, checkBaseClasses);
			return field == null ? null : new DebuggerField(debugger, field);
		});

		public IDebuggerProperty[] GetProperties(bool checkBaseClasses) {
			if (!checkBaseClasses)
				return Properties;
			return debugger.Dispatcher.UI(() => {
				var props = CorClass.FindProperties(checkBaseClasses).ToList();
				var res = new IDebuggerProperty[props.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerProperty(debugger, props[i]);
				return res;
			});
		}

		public IDebuggerProperty[] GetProperties(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var props = CorClass.FindProperties(name, checkBaseClasses).ToList();
			var res = new IDebuggerProperty[props.Count];
			for (int i = 0; i < res.Length; i++)
				res[i] = new DebuggerProperty(debugger, props[i]);
			return res;
		});

		public IDebuggerProperty GetProperty(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var prop = CorClass.FindProperty(name, checkBaseClasses);
			return prop == null ? null : new DebuggerProperty(debugger, prop);
		});

		public IDebuggerEvent[] GetEvents(bool checkBaseClasses) {
			if (!checkBaseClasses)
				return Events;
			return debugger.Dispatcher.UI(() => {
				var events = CorClass.FindEvents(checkBaseClasses).ToList();
				var res = new IDebuggerEvent[events.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerEvent(debugger, events[i]);
				return res;
			});
		}

		public IDebuggerEvent[] GetEvents(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var events = CorClass.FindEvents(name, checkBaseClasses).ToList();
			var res = new IDebuggerEvent[events.Count];
			for (int i = 0; i < res.Length; i++)
				res[i] = new DebuggerEvent(debugger, events[i]);
			return res;
		});

		public IDebuggerEvent GetEvent(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var evt = CorClass.FindEvent(name, checkBaseClasses);
			return evt == null ? null : new DebuggerEvent(debugger, evt);
		});

		public IDebuggerField GetField(SR.FieldInfo field) => debugger.Dispatcher.UI(() => {
			var comparer = new TypeComparer();
			foreach (var f in GetFields(field.Name, true)) {
				if (comparer.Equals(f.FieldSig.GetFieldType(), field.FieldType))
					return f;
			}
			return null;
		});

		public IDebuggerMethod GetMethod(SR.MethodBase method) => debugger.Dispatcher.UI(() => {
			var comparer = new TypeComparer();
			foreach (var m in GetMethods(method.Name, true)) {
				if (comparer.MethodSigEquals(m.MethodSig, method))
					return m;
			}
			return null;
		});

		public IDebuggerProperty GetProperty(SR.PropertyInfo prop) => debugger.Dispatcher.UI(() => {
			var comparer = new TypeComparer();
			foreach (var p in GetProperties(prop.Name, true)) {
				if (comparer.PropertySignatureEquals(p, prop))
					return p;
			}
			return null;
		});

		public IDebuggerEvent GetEvent(SR.EventInfo evt) => debugger.Dispatcher.UI(() => {
			var comparer = new TypeComparer();
			foreach (var e in GetEvents(evt.Name, true)) {
				var eventType = e.EventType;
				// EventType currently always returns null
				if (eventType == null)
					return e;
				if (comparer.Equals(eventType, evt.EventHandlerType))
					return e;
			}
			return null;
		});

		public override bool Equals(object obj) => (obj as DebuggerClass)?.CorClass == CorClass;
		public override int GetHashCode() => hashCode;
		const TypePrinterFlags DEFAULT_FLAGS = TypePrinterFlags.Default;
		public void WriteTo(IOutputWriter output) => Write(output, (TypeFormatFlags)DEFAULT_FLAGS);
		public void Write(IOutputWriter output, TypeFormatFlags flags) =>
			debugger.Dispatcher.UI(() => CorClass.Write(new OutputWriterConverter(output), (TypePrinterFlags)flags));
		public string ToString(TypeFormatFlags flags) => debugger.Dispatcher.UI(() => CorClass.ToString((TypePrinterFlags)flags));
		public override string ToString() => debugger.Dispatcher.UI(() => CorClass.ToString(DEFAULT_FLAGS));
	}
}
