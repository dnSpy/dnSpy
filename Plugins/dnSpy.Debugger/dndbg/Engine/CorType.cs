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
	public sealed class CorType : COMObject<ICorDebugType>, IEquatable<CorType> {
		/// <summary>
		/// Gets the element type
		/// </summary>
		public CorElementType ElementType {
			get { return elemType; }
		}
		readonly CorElementType elemType;

		/// <summary>
		/// Gets the rank of the array
		/// </summary>
		public uint Rank {
			get { return rank; }
		}
		readonly uint rank;

		/// <summary>
		/// Gets the first type parameter
		/// </summary>
		public CorType FirstTypeParameter {
			get {
				ICorDebugType type;
				int hr = obj.GetFirstTypeParameter(out type);
				return hr < 0 || type == null ? null : new CorType(type);
			}
		}

		/// <summary>
		/// Gets all type parameters. If it's a <see cref="CorElementType.Class"/> or a <see cref="CorElementType.ValueType"/>,
		/// then the returned parameters are the type parameters in the correct order. If it's
		/// a <see cref="CorElementType.FnPtr"/>, the first type is the return type, followed by
		/// all the method argument types in correct order. If it's a <see cref="CorElementType.Array"/>,
		/// <see cref="CorElementType.SZArray"/>, <see cref="CorElementType.ByRef"/> or a
		/// <see cref="CorElementType.Ptr"/>, the returned type is the inner type, eg. <c>int</c> if the
		/// type is <int>int[]</c>. In this case, <see cref="FirstTypeParameter"/> can be called instead.
		/// </summary>
		public IEnumerable<CorType> TypeParameters {
			get {
				ICorDebugTypeEnum typeEnum;
				int hr = obj.EnumerateTypeParameters(out typeEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					ICorDebugType type = null;
					uint count;
					hr = typeEnum.Next(1, out type, out count);
					if (hr != 0 || type == null)
						break;
					yield return new CorType(type);
				}
			}
		}

		/// <summary>
		/// true if <see cref="Class"/> can be accessed
		/// </summary>
		public bool HasClass {
			get { return ElementType == CorElementType.Class || ElementType == CorElementType.ValueType; }
		}

		/// <summary>
		/// Gets the class or null. Should only be called if <see cref="ElementType"/> is
		/// <see cref="CorElementType.Class"/> or <see cref="CorElementType.ValueType"/>,
		/// see <see cref="HasClass"/>.
		/// </summary>
		public CorClass Class {
			get {
				ICorDebugClass cls;
				int hr = obj.GetClass(out cls);
				return hr < 0 || cls == null ? null : new CorClass(cls);
			}
		}

		/// <summary>
		/// Gets the base class or null
		/// </summary>
		public CorType Base {
			get {
				ICorDebugType @base;
				int hr = obj.GetBase(out @base);
				return hr < 0 || @base == null ? null : new CorType(@base);
			}
		}

		/// <summary>
		/// true if this class derives from <c>System.Enum</c>
		/// </summary>
		public bool IsEnum {
			get {
				// Also check for Class since the CLR debugger doesn't care if it's correct
				if (ElementType != CorElementType.ValueType && ElementType != CorElementType.Class)
					return false;
				var b = Base;
				if (b == null)
					return false;
				return b.IsSystemEnum;
			}
		}

		/// <summary>
		/// true if this class derives from <c>System.ValueType</c> or <c>System.Enum</c>
		/// </summary>
		public bool IsValueType {
			get { return IsEnum || DerivesFromSystemValueType; }
		}

		/// <summary>
		/// true if it's one of the primitive value types
		/// </summary>
		public bool IsPrimitiveValueType {
			get {
				return (CorElementType.Void <= elemType && elemType <= CorElementType.R8) ||
						elemType == CorElementType.I || elemType == CorElementType.U ||
						elemType == CorElementType.TypedByRef;
			}
		}

		/// <summary>
		/// true if this class directly derives from <c>System.ValueType</c>
		/// </summary>
		public bool DerivesFromSystemValueType {
			get {
				if (IsPrimitiveValueType)
					return true;
				var b = Base;
				return b != null && b.IsSystemValueType;
			}
		}

		/// <summary>
		/// true if this is <c>System.Enum</c>
		/// </summary>
		public bool IsSystemEnum {
			get {
				if (TypeParameters.Any())
					return false;   // System.Enum is not generic
				var cls = Class;
				if (cls == null || !cls.IsSystemEnum)
					return false;
				var b = Base;
				return b != null && b.IsSystemValueType;
			}
		}

		/// <summary>
		/// true if this is <c>System.ValueType</c>
		/// </summary>
		public bool IsSystemValueType {
			get {
				if (TypeParameters.Any())
					return false;   // System.ValueType is not generic
				var cls = Class;
				if (cls == null || !cls.IsSystemValueType)
					return false;
				var b = Base;
				return b != null && b.IsSystemObject;
			}
		}

		/// <summary>
		/// true if this is <c>System.Object</c>
		/// </summary>
		public bool IsSystemObject {
			get {
				if (TypeParameters.Any())
					return false;   // System.Object is not generic
				var cls = Class;
				if (cls == null || !cls.IsSystemObject)
					return false;
				return Base == null;
			}
		}

		/// <summary>
		/// true if this is <c>System.Decimal</c>
		/// </summary>
		public bool IsSystemDecimal {
			get {
				if (TypeParameters.Any())
					return false;   // System.Decimal is not generic
				var cls = Class;
				if (cls == null || !cls.IsSystemDecimal)
					return false;
				var b = Base;
				return b != null && b.IsSystemValueType;
			}
		}

		/// <summary>
		/// Returns <see cref="System.Object"/> or null if it wasn't found in the class hierarchy.
		/// Can only be called if this is a class or a value type but not an interface
		/// </summary>
		public CorType SystemObject {
			get {
				var t = this;
				while (t != null && !t.IsSystemObject)
					t = t.Base;
				return t;
			}
		}

		internal IMetaDataImport MetaDataImport {
			get {
				uint token;
				return GetMetaDataImport(out token);
			}
		}

		internal IMetaDataImport GetMetaDataImport(out uint token) {
			var cls = Class;
			var mod = cls == null ? null : cls.Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			token = cls == null ? 0 : cls.Token;
			return mdi;
		}

		/// <summary>
		/// true if this is <c>System.Nullable`1</c>
		/// </summary>
		public bool IsSystemNullable {
			get {
				if (TypeParameters.Count() != 1)
					return false;
				uint token;
				var mdi = GetMetaDataImport(out token);
				if (MetaDataUtils.GetCountGenericParameters(mdi, token) != 1)
					return false;
				var names = MetaDataUtils.GetTypeDefFullNames(mdi, token);
				if (names.Count != 1 || names[0].Name != "System.Nullable`1")
					return false;

				var b = Base;
				return b != null && b.IsSystemValueType;
			}
		}

		/// <summary>
		/// Same as <see cref="ElementType"/> except that it tries to return a primitive element
		/// type (eg. <see cref="CorElementType.U4"/>) if it's a primitive type.
		/// </summary>
		public CorElementType TryGetPrimitiveType() {
			var etype = ElementType;
			if (etype != CorElementType.Class && etype != CorElementType.ValueType)
				return etype;

			uint token;
			var mdi = GetMetaDataImport(out token);
			var list = MetaDataUtils.GetTypeDefFullNames(mdi, token);
			if (list.Count != 1)
				return etype;
			if (DerivesFromSystemValueType) {
				switch (list[0].Name) {
				case "System.Boolean":	return CorElementType.Boolean;
				case "System.Byte":		return CorElementType.U1;
				case "System.Char":		return CorElementType.Char;
				case "System.Double":	return CorElementType.R8;
				case "System.Int16":	return CorElementType.I2;
				case "System.Int32":	return CorElementType.I4;
				case "System.Int64":	return CorElementType.I8;
				case "System.IntPtr":	return CorElementType.I;
				case "System.SByte":	return CorElementType.I1;
				case "System.Single":	return CorElementType.R4;
				case "System.TypedReference": return CorElementType.TypedByRef;
				case "System.UInt16":	return CorElementType.U2;
				case "System.UInt32":	return CorElementType.U4;
				case "System.UInt64":	return CorElementType.U8;
				case "System.UIntPtr":	return CorElementType.U;
				case "System.Void":		return CorElementType.Void;
				}
			}
			else {
				switch (list[0].Name) {
				case "System.Object":
					if (Base == null)
						return CorElementType.Object;
					break;
				case "System.String":
					var b = Base;
					if (b != null && b.IsSystemObject)
						return CorElementType.String;
					break;
				}
			}
			return etype;
		}

		/// <summary>
		/// Returns the enum underlying type and shouldn't be called unless <see cref="IsEnum"/>
		/// is true. <see cref="CorElementType.End"/> is returned if the underlying type wasn't found.
		/// </summary>
		public CorElementType EnumUnderlyingType {
			get {
				foreach (var info in MetaDataUtils.GetFieldInfos(this, false)) {
					if ((info.Attributes & FieldAttributes.Literal) != 0)
						continue;
					if ((info.Attributes & FieldAttributes.Static) != 0)
						continue;

					var type = info.FieldType.RemovePinnedAndModifiers();
					var etype = (CorElementType)type.GetElementType();
					if (CorElementType.Boolean <= etype && etype <= CorElementType.R8)
						return etype;
					if (etype == CorElementType.I || etype == CorElementType.U)
						return etype;
					break;
				}
				return CorElementType.End;
			}
		}

		/// <summary>
		/// Returns the enum underlying type if it's an enum, else <see cref="ElementType"/> is returned
		/// </summary>
		public CorElementType TypeOrEnumUnderlyingType {
			get {
				if (!IsEnum)
					return ElementType;
				var etype = EnumUnderlyingType;
				return etype == CorElementType.End ? ElementType : etype;
			}
		}

		public CorType(ICorDebugType type)
			: base(type) {
			int hr = type.GetType(out this.elemType);
			if (hr < 0)
				this.elemType = 0;

			hr = type.GetRank(out this.rank);
			if (hr < 0)
				this.rank = 0;
		}

		public TypeAttributes GetTypeAttributes() {
			var cls = Class;
			var mod = cls == null ? null : cls.Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			return MDAPI.GetTypeDefAttributes(mdi, cls.Token) ?? 0;
		}

		/// <summary>
		/// Returns true if it's a System.XXX type in the corlib (eg. mscorlib)
		/// </summary>
		/// <param name="name">Name (not including namespace)</param>
		/// <returns></returns>
		public bool IsSystem(string name) {
			var cls = Class;
			return cls != null && cls.IsSystem(name);
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
		/// Gets the <c>System.Nullable</c> fields if it's a nullable type
		/// </summary>
		/// <param name="hasValueInfo">Updated with <c>hasValue</c> field</param>
		/// <param name="valueInfo">Updated with 'value' field</param>
		/// <returns></returns>
		public bool GetSystemNullableFields(out TokenAndName hasValueInfo, out TokenAndName valueInfo) {
			return Utils.GetSystemNullableFields(this, out hasValueInfo, out valueInfo);
		}

		/// <summary>
		/// Gets the <c>System.Nullable</c> fields if it's a nullable type
		/// </summary>
		/// <param name="hasValueInfo">Updated with <c>hasValue</c> field</param>
		/// <param name="valueInfo">Updated with 'value' field</param>
		/// <param name="nullableElemType">Updated with nullable element type</param>
		/// <returns></returns>
		public bool GetSystemNullableFields(out TokenAndName hasValueInfo, out TokenAndName valueInfo, out CorType nullableElemType) {
			return Utils.GetSystemNullableFields(this, out hasValueInfo, out valueInfo, out nullableElemType);
		}

		/// <summary>
		/// Gets all fields
		/// </summary>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		public IEnumerable<CorFieldInfo> GetFields(bool checkBaseClasses = true) {
			return MetaDataUtils.GetFieldInfos(this, checkBaseClasses);
		}

		/// <summary>
		/// Gets all properties
		/// </summary>
		/// <param name="checkBaseClasses">true to check base classes</param>
		/// <returns></returns>
		public IEnumerable<CorPropertyInfo> GetProperties(bool checkBaseClasses = true) {
			return MetaDataUtils.GetProperties(this, checkBaseClasses);
		}

		/// <summary>
		/// Gets the ToString() method or null if there was an error
		/// </summary>
		/// <returns></returns>
		public CorMethodInfo GetToStringMethod() {
			return MetaDataUtils.GetToStringMethod(this);
		}

		/// <summary>
		/// Gets the <see cref="System.Object"/>'s <c>ToString()</c> method or null if there was
		/// an error.
		/// </summary>
		/// <returns></returns>
		public CorMethodInfo GetSystemObjectToStringMethod() {
			var t = SystemObject;
			return t == null ? null : t.GetToStringMethod();
		}

		/// <summary>
		/// Returns true if an attribute is present
		/// </summary>
		/// <param name="attributeName">Full name of attribute type</param>
		/// <returns></returns>
		public bool HasAttribute(string attributeName) {
			var cls = Class;
			return cls != null && cls.HasAttribute(attributeName);
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
			for (var type = this; type != null; type = type.Base) {
				if (!checkBaseClasses && (object)this != (object)type)
					break;
				if (!type.HasClass)
					continue;
				var cls = type.Class;
				if (cls == null)
					continue;

				var mod = cls.Module;
				var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
				foreach (var mdToken in MDAPI.GetMethodTokens(mdi, cls.Token)) {
					if (MDAPI.GetMethodName(mdi, mdToken) == name) {
						var func = mod.GetFunctionFromToken(mdToken);
						Debug.Assert(func != null);
						if (func != null)
							yield return func;
					}
				}
			}
		}

		/// <summary>
		/// Finds methods
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CorFunction> FindFunctions(bool checkBaseClasses = true) {
			for (var type = this; type != null; type = type.Base) {
				if (!checkBaseClasses && (object)this != (object)type)
					break;
				if (!type.HasClass)
					continue;
				var cls = type.Class;
				if (cls == null)
					continue;

				var mod = cls.Module;
				var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
				foreach (var mdToken in MDAPI.GetMethodTokens(mdi, cls.Token)) {
					var func = mod.GetFunctionFromToken(mdToken);
					Debug.Assert(func != null);
					if (func != null)
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
			for (var type = this; type != null; type = type.Base) {
				if (!checkBaseClasses && (object)this != (object)type)
					break;
				if (!type.HasClass)
					continue;
				var cls = type.Class;
				if (cls == null)
					continue;

				var mod = cls.Module;
				var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
				foreach (var fdToken in MDAPI.GetFieldTokens(mdi, cls.Token)) {
					if (MDAPI.GetFieldName(mdi, fdToken) == name)
						yield return new CorField(cls, fdToken);
				}
			}
		}

		/// <summary>
		/// Finds fields
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CorField> FindFields(bool checkBaseClasses = true) {
			for (var type = this; type != null; type = type.Base) {
				if (!checkBaseClasses && (object)this != (object)type)
					break;
				if (!type.HasClass)
					continue;
				var cls = type.Class;
				if (cls == null)
					continue;

				var mod = cls.Module;
				var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
				foreach (var fdToken in MDAPI.GetFieldTokens(mdi, cls.Token))
					yield return new CorField(cls, fdToken);
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
			for (var type = this; type != null; type = type.Base) {
				if (!checkBaseClasses && (object)this != (object)type)
					break;
				if (!type.HasClass)
					continue;
				var cls = type.Class;
				if (cls == null)
					continue;

				var mod = cls.Module;
				var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
				foreach (var pdToken in MDAPI.GetPropertyTokens(mdi, cls.Token)) {
					if (MDAPI.GetPropertyName(mdi, pdToken) == name)
						yield return new CorProperty(cls, pdToken);
				}
			}
		}

		/// <summary>
		/// Finds properties
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CorProperty> FindProperties(bool checkBaseClasses = true) {
			for (var type = this; type != null; type = type.Base) {
				if (!checkBaseClasses && (object)this != (object)type)
					break;
				if (!type.HasClass)
					continue;
				var cls = type.Class;
				if (cls == null)
					continue;

				var mod = cls.Module;
				var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
				foreach (var pdToken in MDAPI.GetPropertyTokens(mdi, cls.Token))
					yield return new CorProperty(cls, pdToken);
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
		/// Finds events
		/// </summary>
		/// <param name="name">Event name</param>
		/// <returns></returns>
		public IEnumerable<CorEvent> FindEvents(string name, bool checkBaseClasses = true) {
			for (var type = this; type != null; type = type.Base) {
				if (!checkBaseClasses && (object)this != (object)type)
					break;
				if (!type.HasClass)
					continue;
				var cls = type.Class;
				if (cls == null)
					continue;

				var mod = cls.Module;
				var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
				foreach (var edToken in MDAPI.GetEventTokens(mdi, cls.Token)) {
					if (MDAPI.GetEventName(mdi, edToken) == name)
						yield return new CorEvent(cls, edToken);
				}
			}
		}

		/// <summary>
		/// Finds events
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CorEvent> FindEvents(bool checkBaseClasses = true) {
			for (var type = this; type != null; type = type.Base) {
				if (!checkBaseClasses && (object)this != (object)type)
					break;
				if (!type.HasClass)
					continue;
				var cls = type.Class;
				if (cls == null)
					continue;

				var mod = cls.Module;
				var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
				foreach (var edToken in MDAPI.GetEventTokens(mdi, cls.Token))
					yield return new CorEvent(cls, edToken);
			}
		}

		public static bool operator ==(CorType a, CorType b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorType a, CorType b) {
			return !(a == b);
		}

		public bool Equals(CorType other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorType);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public T Write<T>(T output, TypeSig type, TypePrinterFlags flags) where T : ITypeOutput {
			new TypePrinter(output, flags).Write(type, TypeParameters.ToArray());
			return output;
		}

		public string ToString(TypeSig type, TypePrinterFlags flags) {
			return Write(new StringBuilderTypeOutput(), type, flags).ToString();
		}

		public string ToString(TypeSig type) {
			return ToString(type, TypePrinterFlags.Default);
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
