/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Scripting.Debugger;
using SR = System.Reflection;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerType : IDebuggerType {
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
			var cls = Class;
			if (cls != null) {
				@namespace = cls.Namespace;
				name = cls.Name;
			}
			if (name == null)
				name = string.Empty;
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

		public uint Token {
			get {
				if (tokenInitd)
					return token;
				return debugger.Dispatcher.UI(() => {
					if (tokenInitd)
						return token;
					var cls = Class;
					token = cls == null ? 0 : cls.Token;
					tokenInitd = true;
					return token;
				});
			}
		}
		bool tokenInitd;
		uint token;

		public IDebuggerType BaseType => debugger.Dispatcher.UI(() => {
			var b = type.Base;
			return b == null ? null : new DebuggerType(debugger, b);
		});

		public IDebuggerClass Class => debugger.Dispatcher.UI(() => {
			var cls = type.Class;
			return cls == null ? null : new DebuggerClass(debugger, cls);
		});

		public bool DerivesFromSystemValueType => debugger.Dispatcher.UI(() => type.DerivesFromSystemValueType);
		public CorElementType ElementType => elementType;
		public CorElementType ElementTypeOrEnumUnderlyingType => debugger.Dispatcher.UI(() => (CorElementType)type.TypeOrEnumUnderlyingType);
		public CorElementType EnumUnderlyingType => debugger.Dispatcher.UI(() => (CorElementType)type.EnumUnderlyingType);

		public IDebuggerType FirstTypeParameter => debugger.Dispatcher.UI(() => {
			var b = type.FirstTypeParameter;
			return b == null ? null : new DebuggerType(debugger, b);
		});

		public bool IsAnyArray => elementType == CorElementType.SZArray || elementType == CorElementType.Array;
		public bool IsArray => elementType == CorElementType.Array;
		public bool IsByRef => elementType == CorElementType.ByRef;
		public bool IsEnum => debugger.Dispatcher.UI(() => type.IsEnum);
		public bool IsFnPtr => elementType == CorElementType.FnPtr;
		public bool IsGenericInst => elementType == CorElementType.GenericInst;
		public bool IsPtr => elementType == CorElementType.Ptr;
		public bool IsSystemEnum => debugger.Dispatcher.UI(() => type.IsSystemEnum);
		public bool IsSystemNullable => debugger.Dispatcher.UI(() => type.IsSystemNullable);
		public bool IsSystemObject => debugger.Dispatcher.UI(() => type.IsSystemObject);
		public bool IsSystemValueType => debugger.Dispatcher.UI(() => type.IsSystemValueType);
		public bool IsSZArray => elementType == CorElementType.SZArray;
		public bool IsValueType => debugger.Dispatcher.UI(() => type.IsValueType);
		public uint Rank => debugger.Dispatcher.UI(() => type.Rank);

		public IDebuggerType[] TypeParameters => debugger.Dispatcher.UI(() => {
			var list = new List<IDebuggerType>();
			foreach (var t in type.TypeParameters)
				list.Add(new DebuggerType(debugger, t));
			return list.ToArray();
		});

		internal CorType CorType => type;

		readonly Debugger debugger;
		readonly CorType type;
		readonly int hashCode;
		readonly CorElementType elementType;

		public DebuggerType(Debugger debugger, CorType type, uint token = 0) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.type = type;
			hashCode = type.GetHashCode();
			elementType = (CorElementType)type.ElementType;
			Attributes = type.GetTypeAttributes();
			this.token = token;
			tokenInitd = token != 0;
		}

		public bool HasAttribute(string attributeName) => debugger.Dispatcher.UI(() => type.HasAttribute(attributeName));
		public bool IsSystem(string name) => debugger.Dispatcher.UI(() => type.IsSystem(name));
		public CorElementType TryGetPrimitiveType() => debugger.Dispatcher.UI(() => (CorElementType)type.TryGetPrimitiveType());

		public IDebuggerValue ReadStaticField(IStackFrame frame, uint token) => debugger.Dispatcher.UI(() => {
			var value = type.GetStaticFieldValue(token, ((StackFrame)frame).CorFrame);
			return value == null ? null : new DebuggerValue(debugger, value);
		});

		public IDebuggerValue ReadStaticField(IStackFrame frame, IDebuggerField field) => ReadStaticField(frame, field.Token);

		public IDebuggerValue ReadStaticField(IStackFrame frame, string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var field = GetField(name, checkBaseClasses);
			return field == null ? null : ReadStaticField(frame, field.Token);
		});

		CorAppDomain CorAppDomain {
			get {
				var t = type;
				int i = 0;
				while (t != null && !t.HasClass && i++ < 1000)
					t = t.FirstTypeParameter;
				return t?.Class?.Module?.Assembly?.AppDomain;
			}
		}

		public IDebuggerType ToPtr() => debugger.Dispatcher.UI(() => {
			var res = CorAppDomain?.GetPtr(type);
			return res == null ? null : new DebuggerType(debugger, res);
		});

		public IDebuggerType ToPointer() => ToPtr();

		public IDebuggerType ToByRef() => debugger.Dispatcher.UI(() => {
			var res = CorAppDomain?.GetByRef(type);
			return res == null ? null : new DebuggerType(debugger, res);
		});

		public IDebuggerType ToByReference() => ToByRef();

		public IDebuggerType ToSZArray() => debugger.Dispatcher.UI(() => {
			var res = CorAppDomain?.GetSZArray(type);
			return res == null ? null : new DebuggerType(debugger, res);
		});

		public IDebuggerType ToArray(int rank) => debugger.Dispatcher.UI(() => {
			var res = CorAppDomain?.GetArray(type, (uint)rank);
			return res == null ? null : new DebuggerType(debugger, res);
		});

		public IDebuggerMethod[] Methods {
			get {
				if (methods != null)
					return methods;
				return debugger.Dispatcher.UI(() => {
					if (methods != null)
						return methods;
					var funcs = type.FindFunctions(false).ToList();
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
					var flds = type.FindFields(false).ToList();
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
					var props = type.FindProperties(false).ToList();
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
					var evts = type.FindEvents(false).ToList();
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
				var funcs = type.FindFunctions(checkBaseClasses).ToList();
				var res = new IDebuggerMethod[funcs.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerMethod(debugger, funcs[i]);
				return res;
			});
		}

		public IDebuggerMethod[] GetMethods(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var funcs = type.FindFunctions(name, checkBaseClasses).ToList();
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
			if (!type.HasClass)
				return Array.Empty<IDebuggerMethod>();
			var cls = Class;
			return cls == null ? Array.Empty<IDebuggerMethod>() : cls.GetConstructors();
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
				var fields = type.FindFields(checkBaseClasses).ToList();
				var res = new IDebuggerField[fields.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerField(debugger, fields[i]);
				return res;
			});
		}

		public IDebuggerField[] GetFields(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var fields = type.FindFields(name, checkBaseClasses).ToList();
			var res = new IDebuggerField[fields.Count];
			for (int i = 0; i < res.Length; i++)
				res[i] = new DebuggerField(debugger, fields[i]);
			return res;
		});

		public IDebuggerField GetField(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var field = type.FindField(name, checkBaseClasses);
			return field == null ? null : new DebuggerField(debugger, field);
		});

		public IDebuggerProperty[] GetProperties(bool checkBaseClasses) {
			if (!checkBaseClasses)
				return Properties;
			return debugger.Dispatcher.UI(() => {
				var props = type.FindProperties(checkBaseClasses).ToList();
				var res = new IDebuggerProperty[props.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerProperty(debugger, props[i]);
				return res;
			});
		}

		public IDebuggerProperty[] GetProperties(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var props = type.FindProperties(name, checkBaseClasses).ToList();
			var res = new IDebuggerProperty[props.Count];
			for (int i = 0; i < res.Length; i++)
				res[i] = new DebuggerProperty(debugger, props[i]);
			return res;
		});

		public IDebuggerProperty GetProperty(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var prop = type.FindProperty(name, checkBaseClasses);
			return prop == null ? null : new DebuggerProperty(debugger, prop);
		});

		public IDebuggerEvent[] GetEvents(bool checkBaseClasses) {
			if (!checkBaseClasses)
				return Events;
			return debugger.Dispatcher.UI(() => {
				var events = type.FindEvents(checkBaseClasses).ToList();
				var res = new IDebuggerEvent[events.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DebuggerEvent(debugger, events[i]);
				return res;
			});
		}

		public IDebuggerEvent[] GetEvents(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var events = type.FindEvents(name, checkBaseClasses).ToList();
			var res = new IDebuggerEvent[events.Count];
			for (int i = 0; i < res.Length; i++)
				res[i] = new DebuggerEvent(debugger, events[i]);
			return res;
		});

		public IDebuggerEvent GetEvent(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var evt = type.FindEvent(name, checkBaseClasses);
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

		public override bool Equals(object obj) => (obj as DebuggerType)?.type == type;
		public override int GetHashCode() => hashCode;
		const TypePrinterFlags DEFAULT_FLAGS = TypePrinterFlags.Default;
		public void WriteTo(IOutputWriter output) => Write(output, (TypeFormatFlags)DEFAULT_FLAGS);
		public void Write(IOutputWriter output, TypeFormatFlags flags) =>
			debugger.Dispatcher.UI(() => type.Write(new OutputWriterConverter(output), (TypePrinterFlags)flags));
		public string ToString(TypeFormatFlags flags) => debugger.Dispatcher.UI(() => type.ToString((TypePrinterFlags)flags));
		public override string ToString() => debugger.Dispatcher.UI(() => type.ToString(DEFAULT_FLAGS));
	}
}
