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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dnlib.DotNet;

namespace dndbg.Engine {
	public sealed class CorClass : COMObject<ICorDebugClass>, IEquatable<CorClass> {
		/// <summary>
		/// Gets the token
		/// </summary>
		public uint Token {
			get { return token; }
		}
		readonly uint token;

		/// <summary>
		/// Gets the module or null
		/// </summary>
		public CorModule Module {
			get {
				ICorDebugModule module;
				int hr = obj.GetModule(out module);
				return hr < 0 || module == null ? null : new CorModule(module);
			}
		}

		/// <summary>
		/// true if this is <c>System.Enum</c>
		/// </summary>
		public bool IsSystemEnum {
			get { return IsSystem("Enum"); }
		}

		/// <summary>
		/// true if this is <c>System.ValueType</c>
		/// </summary>
		public bool IsSystemValueType {
			get { return IsSystem("ValueType"); }
		}

		/// <summary>
		/// true if this is <c>System.Object</c>
		/// </summary>
		public bool IsSystemObject {
			get { return IsSystem("Object"); }
		}

		/// <summary>
		/// true if this is <c>System.Decimal</c>
		/// </summary>
		public bool IsSystemDecimal {
			get { return IsSystem("Decimal"); }
		}

		public CorClass(ICorDebugClass cls)
			: base(cls) {
			int hr = cls.GetToken(out this.token);
			if (hr < 0)
				this.token = 0;
		}

		public TypeAttributes GetTypeAttributes() {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			return MDAPI.GetTypeDefAttributes(mdi, token) ?? 0;
		}

		/// <summary>
		/// Creates a <see cref="CorType"/>
		/// </summary>
		/// <param name="etype">Element type, must be <see cref="CorElementType.Class"/> or <see cref="CorElementType.ValueType"/></param>
		/// <param name="typeArgs">Generic type arguments or null</param>
		/// <returns></returns>
		public CorType GetParameterizedType(CorElementType etype, CorType[] typeArgs = null) {
			Debug.Assert(etype == CorElementType.Class || etype == CorElementType.ValueType);
			var c2 = obj as ICorDebugClass2;
			if (c2 == null)
				return null;
			ICorDebugType value;
			int hr = c2.GetParameterizedType(etype, typeArgs == null ? 0 : typeArgs.Length, typeArgs.ToCorDebugArray(), out value);
			return hr < 0 || value == null ? null : new CorType(value);
		}

		/// <summary>
		/// Returns true if it's a System.XXX type in the corlib (eg. mscorlib)
		/// </summary>
		/// <param name="name">Name (not including namespace)</param>
		/// <returns></returns>
		public bool IsSystem(string name) {
			var mod = Module;
			if (mod == null)
				return false;
			var names = MetaDataUtils.GetTypeDefFullNames(mod.GetMetaDataInterface<IMetaDataImport>(), Token);
			if (names.Count != 1)
				return false;
			if (names[0].Name != "System." + name)
				return false;

			//TODO: Check if it's mscorlib

			return true;
		}

		/// <summary>
		/// Reads a static field
		/// </summary>
		/// <param name="token">Token of field</param>
		/// <param name="frame">Frame</param>
		/// <returns></returns>
		public CorValue GetStaticFieldValue(uint token, CorFrame frame) {
			int hr;
			return GetStaticFieldValue(token, frame, out hr);
		}

		/// <summary>
		/// Reads a static field
		/// </summary>
		/// <param name="token">Token of field</param>
		/// <param name="frame">Frame</param>
		/// <param name="hr">Updated with HRESULT</param>
		/// <returns></returns>
		public CorValue GetStaticFieldValue(uint token, CorFrame frame, out int hr) {
			ICorDebugValue value;
			hr = obj.GetStaticFieldValue(token, frame == null ? null : frame.RawObject, out value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		/// <summary>
		/// Mark all methods in the type as user code
		/// </summary>
		/// <param name="jmc">true to set user code</param>
		/// <returns></returns>
		public bool SetJustMyCode(bool jmc) {
			var c2 = obj as ICorDebugClass2;
			if (c2 == null)
				return false;
			int hr = c2.SetJMCStatus(jmc ? 1 : 0);
			return hr >= 0;
		}

		/// <summary>
		/// Gets type generic parameters
		/// </summary>
		/// <returns></returns>
		public List<TokenAndName> GetGenericParameters() {
			var module = Module;
			return MetaDataUtils.GetGenericParameterNames(module == null ? null : module.GetMetaDataInterface<IMetaDataImport>(), Token);
		}

		/// <summary>
		/// Returns true if an attribute is present
		/// </summary>
		/// <param name="attributeName">Full name of attribute type</param>
		/// <returns></returns>
		public bool HasAttribute(string attributeName) {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			return MDAPI.HasAttribute(mdi, Token, attributeName);
		}

		/// <summary>
		/// Finds a method
		/// </summary>
		/// <param name="name">Method name</param>
		/// <returns></returns>
		public CorFunction FindFunction(string name, bool checkBaseClasses = true) {
			return FindFunctions(name, checkBaseClasses).FirstOrDefault();
		}

		/// <summary>
		/// Finds methods
		/// </summary>
		/// <param name="name">Method name</param>
		/// <returns></returns>
		public IEnumerable<CorFunction> FindFunctions(string name, bool checkBaseClasses = true) {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			foreach (var mdToken in MDAPI.GetMethodTokens(mdi, token)) {
				if (MDAPI.GetMethodName(mdi, mdToken) == name) {
					var func = mod.GetFunctionFromToken(mdToken);
					Debug.Assert(func != null);
					if (func != null)
						yield return func;
				}
			}
			if (checkBaseClasses) {
				var type = GetParameterizedType(CorElementType.Class);
				if (type != null)
					type = type.Base;
				if (type != null) {
					foreach (var func in type.FindFunctions(name, checkBaseClasses))
						yield return func;
				}
			}
		}

		/// <summary>
		/// Finds methods
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CorFunction> FindFunctions(bool checkBaseClasses = true) {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			foreach (var mdToken in MDAPI.GetMethodTokens(mdi, token)) {
				var func = mod.GetFunctionFromToken(mdToken);
				Debug.Assert(func != null);
				if (func != null)
					yield return func;
			}
			if (checkBaseClasses) {
				var type = GetParameterizedType(CorElementType.Class);
				if (type != null)
					type = type.Base;
				if (type != null) {
					foreach (var func in type.FindFunctions(checkBaseClasses))
						yield return func;
				}
			}
		}

		/// <summary>
		/// Finds a field
		/// </summary>
		/// <param name="name">Field name</param>
		/// <returns></returns>
		public CorField FindField(string name, bool checkBaseClasses = true) {
			return FindFields(name, checkBaseClasses).FirstOrDefault();
		}

		/// <summary>
		/// Finds fields
		/// </summary>
		/// <param name="name">Field name</param>
		/// <returns></returns>
		public IEnumerable<CorField> FindFields(string name, bool checkBaseClasses = true) {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			foreach (var fdToken in MDAPI.GetFieldTokens(mdi, token)) {
				if (MDAPI.GetFieldName(mdi, fdToken) == name)
					yield return new CorField(this, fdToken);
			}
			if (checkBaseClasses) {
				var type = GetParameterizedType(CorElementType.Class);
				if (type != null)
					type = type.Base;
				if (type != null) {
					foreach (var func in type.FindFields(name, checkBaseClasses))
						yield return func;
				}
			}
		}

		/// <summary>
		/// Finds fields
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CorField> FindFields(bool checkBaseClasses = true) {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			foreach (var fdToken in MDAPI.GetFieldTokens(mdi, token))
				yield return new CorField(this, fdToken);
			if (checkBaseClasses) {
				var type = GetParameterizedType(CorElementType.Class);
				if (type != null)
					type = type.Base;
				if (type != null) {
					foreach (var func in type.FindFields(checkBaseClasses))
						yield return func;
				}
			}
		}

		/// <summary>
		/// Finds a property
		/// </summary>
		/// <param name="name">Property name</param>
		/// <returns></returns>
		public CorProperty FindProperty(string name, bool checkBaseClasses = true) {
			return FindProperties(name, checkBaseClasses).FirstOrDefault();
		}

		/// <summary>
		/// Finds properties
		/// </summary>
		/// <param name="name">Property name</param>
		/// <returns></returns>
		public IEnumerable<CorProperty> FindProperties(string name, bool checkBaseClasses = true) {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			foreach (var pdToken in MDAPI.GetPropertyTokens(mdi, token)) {
				if (MDAPI.GetPropertyName(mdi, pdToken) == name)
					yield return new CorProperty(this, pdToken);
			}
			if (checkBaseClasses) {
				var type = GetParameterizedType(CorElementType.Class);
				if (type != null)
					type = type.Base;
				if (type != null) {
					foreach (var prop in type.FindProperties(name, checkBaseClasses))
						yield return prop;
				}
			}
		}

		/// <summary>
		/// Finds properties
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CorProperty> FindProperties(bool checkBaseClasses = true) {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			foreach (var pdToken in MDAPI.GetPropertyTokens(mdi, token))
				yield return new CorProperty(this, pdToken);
			if (checkBaseClasses) {
				var type = GetParameterizedType(CorElementType.Class);
				if (type != null)
					type = type.Base;
				if (type != null) {
					foreach (var prop in type.FindProperties(checkBaseClasses))
						yield return prop;
				}
			}
		}

		/// <summary>
		/// Finds an event
		/// </summary>
		/// <param name="name">Event name</param>
		/// <returns></returns>
		public CorEvent FindEvent(string name, bool checkBaseClasses = true) {
			return FindEvents(name, checkBaseClasses).FirstOrDefault();
		}

		/// <summary>
		/// Finds event
		/// </summary>
		/// <param name="name">Event name</param>
		/// <returns></returns>
		public IEnumerable<CorEvent> FindEvents(string name, bool checkBaseClasses = true) {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			foreach (var edToken in MDAPI.GetEventTokens(mdi, token)) {
				if (MDAPI.GetEventName(mdi, edToken) == name)
					yield return new CorEvent(this, edToken);
			}
			if (checkBaseClasses) {
				var type = GetParameterizedType(CorElementType.Class);
				if (type != null)
					type = type.Base;
				if (type != null) {
					foreach (var evt in type.FindEvents(name, checkBaseClasses))
						yield return evt;
				}
			}
		}

		/// <summary>
		/// Finds events
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CorEvent> FindEvents(bool checkBaseClasses = true) {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			foreach (var edToken in MDAPI.GetEventTokens(mdi, token))
				yield return new CorEvent(this, edToken);
			if (checkBaseClasses) {
				var type = GetParameterizedType(CorElementType.Class);
				if (type != null)
					type = type.Base;
				if (type != null) {
					foreach (var evt in type.FindEvents(checkBaseClasses))
						yield return evt;
				}
			}
		}

		/// <summary>
		/// Finds all constructors
		/// </summary>
		/// <returns></returns>
		public CorFunction[] FindConstructors() {
			var ctors = new List<CorFunction>();
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			foreach (var mdToken in MDAPI.GetMethodTokens(mdi, token)) {
				MethodAttributes attrs;
				MethodImplAttributes implAttrs;
				if (!MDAPI.GetMethodAttributes(mdi, mdToken, out attrs, out implAttrs))
					continue;
				if ((attrs & MethodAttributes.RTSpecialName) == 0)
					continue;
				if (MDAPI.GetMethodName(mdi, mdToken) != ".ctor")
					continue;

				var ctor = mod.GetFunctionFromToken(mdToken);
				Debug.Assert(ctor != null);
				if (ctor != null)
					ctors.Add(ctor);
			}
			return ctors.ToArray();
		}

		public void GetName(out string ns, out string name) {
			var mod = Module;
			var fn = MDAPI.GetTypeDefName(mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>(), Token);
			if (fn == null) {
				ns = null;
				name = null;
				return;
			}

			int i = fn.LastIndexOf('.');
			if (i < 0) {
				ns = null;
				name = fn;
			}
			else {
				ns = fn.Substring(0, i);
				name = fn.Substring(i + 1);
			}
		}

		public static bool operator ==(CorClass a, CorClass b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorClass a, CorClass b) {
			return !(a == b);
		}

		public bool Equals(CorClass other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorClass);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public T Write<T>(T output, TypePrinterFlags flags) where T : ITypeOutput {
			new TypePrinter(output, flags).Write(this);
			return output;
		}

		public string ToString(TypePrinterFlags flags) {
			return Write(new StringBuilderTypeOutput(), flags).ToString();
		}

		public override string ToString() {
			return ToString(TypePrinterFlags.Default);
		}
	}
}
