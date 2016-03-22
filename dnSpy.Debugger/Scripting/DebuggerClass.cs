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

using System.Diagnostics;
using System.Linq;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Scripting.Debugger;
using dnSpy.Shared.Scripting;
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
			cls.GetName(out tmpNs, out tmpName);
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

		public string FullName {
			get { return ToString(); }
		}

		public TypeAttributes Attributes {
			get { return attributes; }
		}
		readonly TypeAttributes attributes;

		public TypeAttributes Visibility {
			get { return attributes & TypeAttributes.VisibilityMask; }
		}

		public bool IsNotPublic {
			get { return (attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic; }
		}

		public bool IsPublic {
			get { return (attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public; }
		}

		public bool IsNestedPublic {
			get { return (attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic; }
		}

		public bool IsNestedPrivate {
			get { return (attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate; }
		}

		public bool IsNestedFamily {
			get { return (attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily; }
		}

		public bool IsNestedAssembly {
			get { return (attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly; }
		}

		public bool IsNestedFamilyAndAssembly {
			get { return (attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamANDAssem; }
		}

		public bool IsNestedFamilyOrAssembly {
			get { return (attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamORAssem; }
		}

		public TypeAttributes Layout {
			get { return attributes & TypeAttributes.LayoutMask; }
		}

		public bool IsAutoLayout {
			get { return (attributes & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout; }
		}

		public bool IsSequentialLayout {
			get { return (attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout; }
		}

		public bool IsExplicitLayout {
			get { return (attributes & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout; }
		}

		public bool IsInterface {
			get { return (attributes & TypeAttributes.Interface) != 0; }
		}

		public bool IsClass {
			get { return (attributes & TypeAttributes.Interface) == 0; }
		}

		public bool IsAbstract {
			get { return (attributes & TypeAttributes.Abstract) != 0; }
		}

		public bool IsSealed {
			get { return (attributes & TypeAttributes.Sealed) != 0; }
		}

		public bool IsSpecialName {
			get { return (attributes & TypeAttributes.SpecialName) != 0; }
		}

		public bool IsImport {
			get { return (attributes & TypeAttributes.Import) != 0; }
		}

		public bool IsSerializable {
			get { return (attributes & TypeAttributes.Serializable) != 0; }
		}

		public bool IsWindowsRuntime {
			get { return (attributes & TypeAttributes.WindowsRuntime) != 0; }
		}

		public TypeAttributes StringFormat {
			get { return attributes & TypeAttributes.StringFormatMask; }
		}

		public bool IsAnsiClass {
			get { return (attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AnsiClass; }
		}

		public bool IsUnicodeClass {
			get { return (attributes & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass; }
		}

		public bool IsAutoClass {
			get { return (attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AutoClass; }
		}

		public bool IsCustomFormatClass {
			get { return (attributes & TypeAttributes.StringFormatMask) == TypeAttributes.CustomFormatClass; }
		}

		public bool IsBeforeFieldInit {
			get { return (attributes & TypeAttributes.BeforeFieldInit) != 0; }
		}

		public bool IsForwarder {
			get { return (attributes & TypeAttributes.Forwarder) != 0; }
		}

		public bool IsRuntimeSpecialName {
			get { return (attributes & TypeAttributes.RTSpecialName) != 0; }
		}

		public bool HasSecurity {
			get { return (attributes & TypeAttributes.HasSecurity) != 0; }
		}

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

		public IDebuggerType BaseType {
			get {
				return debugger.Dispatcher.UI(() => {
					var t = ToType(emptyTypes);
					return t == null ? null : t.BaseType;
				});
			}
		}
		static readonly IDebuggerType[] emptyTypes = new IDebuggerType[0];

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
			this.attributes = cls.GetTypeAttributes();
		}

		public IDebuggerType ToType(IDebuggerType[] typeArgs) {
			return debugger.Dispatcher.UI(() => {
				// We can use Class all the time, even for value types
				var type = cls.GetParameterizedType(dndbg.COM.CorDebug.CorElementType.Class, typeArgs.ToCorTypes());
				return type == null ? null : new DebuggerType(debugger, type, token);
			});
		}

		public IDebuggerValue ReadStaticField(IStackFrame frame, uint token) {
			return debugger.Dispatcher.UI(() => {
				var value = cls.GetStaticFieldValue(token, ((StackFrame)frame).CorFrame);
				return value == null ? null : new DebuggerValue(debugger, value);
			});
		}

		public IDebuggerValue ReadStaticField(IStackFrame frame, IDebuggerField field) {
			return ReadStaticField(frame, field.Token);
		}

		public IDebuggerValue ReadStaticField(IStackFrame frame, string name, bool checkBaseClasses) {
			return debugger.Dispatcher.UI(() => {
				var field = GetField(name, checkBaseClasses);
				return field == null ? null : ReadStaticField(frame, field.Token);
			});
		}

		public IDebuggerMethod[] Methods {
			get {
				if (methods != null)
					return methods;
				return debugger.Dispatcher.UI(() => {
					if (methods != null)
						return methods;
					var funcs = cls.FindFunctions(false).ToList();
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
					var flds = cls.FindFields(false).ToList();
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
					var props = cls.FindProperties(false).ToList();
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
					var evts = cls.FindEvents(false).ToList();
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
				var funcs = cls.FindFunctions(checkBaseClasses).ToList();
				var res = new IDebuggerMethod[funcs.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerMethod(debugger, funcs[i]);
				return res;
			});
		}

		public IDebuggerMethod[] GetMethods(string name, bool checkBaseClasses) {
			return debugger.Dispatcher.UI(() => {
				var funcs = cls.FindFunctions(name, checkBaseClasses).ToList();
				var res = new IDebuggerMethod[funcs.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerMethod(debugger, funcs[i]);
				return res;
			});
		}

		public IDebuggerMethod GetMethod(string name, bool checkBaseClasses) {
			return debugger.Dispatcher.UI(() => {
				var methods = GetMethods(name, checkBaseClasses);
				foreach (var m in methods) {
					if (m.MethodSig.Params.Count == 0)
						return m;
				}
				return methods.Length == 1 ? methods[0] : null;
			});
		}

		public IDebuggerMethod GetMethod(string name, params object[] argTypes) {
			return GetMethod(name, true, argTypes);
		}

		public IDebuggerMethod GetMethod(string name, bool checkBaseClasses, params object[] argTypes) {
			return debugger.Dispatcher.UI(() => {
				var comparer = new TypeComparer();
				foreach (var m in GetMethods(name, checkBaseClasses)) {
					if (comparer.ArgListsEquals(m.MethodSig.Params, argTypes))
						return m;
				}
				return null;
			});
		}

		public IDebuggerMethod[] GetConstructors() {
			return debugger.Dispatcher.UI(() => {
				var ctors = cls.FindConstructors();
				var res = new IDebuggerMethod[ctors.Length];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerMethod(debugger, ctors[i]);
				return res;
			});
		}

		public IDebuggerMethod GetConstructor() {
			return GetConstructor(emptyArgTypes);
		}
		static readonly object[] emptyArgTypes = new object[0];

		public IDebuggerMethod GetConstructor(params object[] argTypes) {
			return debugger.Dispatcher.UI(() => {
				var comparer = new TypeComparer();
				foreach (var m in GetConstructors()) {
					if (comparer.ArgListsEquals(m.MethodSig.Params, argTypes))
						return m;
				}
				return null;
			});
		}

		public IDebuggerField[] GetFields(bool checkBaseClasses) {
			if (!checkBaseClasses)
				return Fields;
			return debugger.Dispatcher.UI(() => {
				var fields = cls.FindFields(checkBaseClasses).ToList();
				var res = new IDebuggerField[fields.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerField(debugger, fields[i]);
				return res;
			});
		}

		public IDebuggerField[] GetFields(string name, bool checkBaseClasses) {
			return debugger.Dispatcher.UI(() => {
				var fields = cls.FindFields(name, checkBaseClasses).ToList();
				var res = new IDebuggerField[fields.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerField(debugger, fields[i]);
				return res;
			});
		}

		public IDebuggerField GetField(string name, bool checkBaseClasses) {
			return debugger.Dispatcher.UI(() => {
				var field = cls.FindField(name, checkBaseClasses);
				return field == null ? null : new DebuggerField(debugger, field);
			});
		}

		public IDebuggerProperty[] GetProperties(bool checkBaseClasses) {
			if (!checkBaseClasses)
				return Properties;
			return debugger.Dispatcher.UI(() => {
				var props = cls.FindProperties(checkBaseClasses).ToList();
				var res = new IDebuggerProperty[props.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerProperty(debugger, props[i]);
				return res;
			});
		}

		public IDebuggerProperty[] GetProperties(string name, bool checkBaseClasses) {
			return debugger.Dispatcher.UI(() => {
				var props = cls.FindProperties(name, checkBaseClasses).ToList();
				var res = new IDebuggerProperty[props.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerProperty(debugger, props[i]);
				return res;
			});
		}

		public IDebuggerProperty GetProperty(string name, bool checkBaseClasses) {
			return debugger.Dispatcher.UI(() => {
				var prop = cls.FindProperty(name, checkBaseClasses);
				return prop == null ? null : new DebuggerProperty(debugger, prop);
			});
		}

		public IDebuggerEvent[] GetEvents(bool checkBaseClasses) {
			if (!checkBaseClasses)
				return Events;
			return debugger.Dispatcher.UI(() => {
				var events = cls.FindEvents(checkBaseClasses).ToList();
				var res = new IDebuggerEvent[events.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerEvent(debugger, events[i]);
				return res;
			});
		}

		public IDebuggerEvent[] GetEvents(string name, bool checkBaseClasses) {
			return debugger.Dispatcher.UI(() => {
				var events = cls.FindEvents(name, checkBaseClasses).ToList();
				var res = new IDebuggerEvent[events.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerEvent(debugger, events[i]);
				return res;
			});
		}

		public IDebuggerEvent GetEvent(string name, bool checkBaseClasses) {
			return debugger.Dispatcher.UI(() => {
				var evt = cls.FindEvent(name, checkBaseClasses);
				return evt == null ? null : new DebuggerEvent(debugger, evt);
			});
		}

		public IDebuggerField GetField(SR.FieldInfo field) {
			return debugger.Dispatcher.UI(() => {
				var comparer = new TypeComparer();
				foreach (var f in GetFields(field.Name, true)) {
					if (comparer.Equals(f.FieldSig.GetFieldType(), field.FieldType))
						return f;
				}
				return null;
			});
		}

		public IDebuggerMethod GetMethod(SR.MethodBase method) {
			return debugger.Dispatcher.UI(() => {
				var comparer = new TypeComparer();
				foreach (var m in GetMethods(method.Name, true)) {
					if (comparer.MethodSigEquals(m.MethodSig, method))
						return m;
				}
				return null;
			});
		}

		public IDebuggerProperty GetProperty(SR.PropertyInfo prop) {
			return debugger.Dispatcher.UI(() => {
				var comparer = new TypeComparer();
				foreach (var p in GetProperties(prop.Name, true)) {
					if (comparer.PropertySignatureEquals(p, prop))
						return p;
				}
				return null;
			});
		}

		public IDebuggerEvent GetEvent(SR.EventInfo evt) {
			return debugger.Dispatcher.UI(() => {
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
