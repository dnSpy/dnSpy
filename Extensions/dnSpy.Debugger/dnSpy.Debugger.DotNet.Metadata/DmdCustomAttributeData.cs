/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Custom attribute data
	/// </summary>
	public sealed class DmdCustomAttributeData {
		/// <summary>
		/// Gets the custom attribute type
		/// </summary>
		public DmdType AttributeType => Constructor.DeclaringType;

		/// <summary>
		/// Gets the custom attribute constructor
		/// </summary>
		public DmdConstructorInfo Constructor { get; }

		/// <summary>
		/// Gets the constructor arguments
		/// </summary>
		public IList<DmdCustomAttributeTypedArgument> ConstructorArguments { get; }

		/// <summary>
		/// Gets all named arguments (properties and fields)
		/// </summary>
		public IList<DmdCustomAttributeNamedArgument> NamedArguments { get; }

		/// <summary>
		/// true if this custom attribute was not part of the #Blob but created from some other info
		/// </summary>
		public bool IsPseudoCustomAttribute { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="constructor">Custom attribute constructor</param>
		/// <param name="constructorArguments">Constructor arguments or null</param>
		/// <param name="namedArguments">Custom attribute named arguments (fields and properties) or null</param>
		/// <param name="isPseudoCustomAttribute">true if this custom attribute was not part of the #Blob but created from some other info</param>
		public DmdCustomAttributeData(DmdConstructorInfo constructor, IList<DmdCustomAttributeTypedArgument> constructorArguments, IList<DmdCustomAttributeNamedArgument> namedArguments, bool isPseudoCustomAttribute) {
			Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
			ConstructorArguments = ReadOnlyCollectionHelpers.Create(constructorArguments);
			NamedArguments = ReadOnlyCollectionHelpers.Create(namedArguments);
			IsPseudoCustomAttribute = isPseudoCustomAttribute;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			var sb = ObjectPools.AllocStringBuilder();
			sb.Append('[');
			sb.Append(AttributeType.FullName);
			sb.Append('(');
			bool comma = false;
			foreach (var arg in ConstructorArguments) {
				if (comma)
					sb.Append(", ");
				comma = true;
				sb.Append(arg.ToString());
			}
			foreach (var arg in NamedArguments) {
				if (comma)
					sb.Append(", ");
				comma = true;
				sb.Append(arg.ToString());
			}
			sb.Append(")]");
			return ObjectPools.FreeAndToString(ref sb);
		}
	}

	/// <summary>
	/// Custom attribute typed argument
	/// </summary>
	public readonly struct DmdCustomAttributeTypedArgument : IEquatable<DmdCustomAttributeTypedArgument> {
		/// <summary>
		/// Gets the argument type
		/// </summary>
		public DmdType ArgumentType { get; }

		/// <summary>
		/// Gets the argument value
		/// </summary>
		public object Value { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="argumentType">Argument type</param>
		/// <param name="value">Argument value</param>
		public DmdCustomAttributeTypedArgument(DmdType argumentType, object value) {
			VerifyValue(value);
			ArgumentType = argumentType ?? throw new ArgumentNullException(nameof(argumentType));
			Value = value;
		}

		[Conditional("DEBUG")]
		static void VerifyValue(object value) {
			if (value == null || value is DmdType || value is IList<DmdCustomAttributeTypedArgument>)
				return;
			switch (Type.GetTypeCode(value.GetType())) {
			case TypeCode.Boolean:
			case TypeCode.Char:
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:
			case TypeCode.Int64:
			case TypeCode.UInt64:
			case TypeCode.Single:
			case TypeCode.Double:
			case TypeCode.String:
				return;
			}
			Debug.Fail("Invalid value");
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => ToString(false);

		internal string ToString(bool typed) {
			if (Value == null)
				return typed ? "null" : "(" + ArgumentType?.Name + ")null";
			if (ArgumentType.IsEnum)
				return typed ? Value.ToString() : "(" + ArgumentType.FullName + ")" + Value.ToString();
			if (Value is string s)
				return "\"" + s + "\"";
			if (Value is char c)
				return "'" + c.ToString() + "'";
			if (Value is DmdType type)
				return "typeof(" + type.FullName + ")";
			if (ArgumentType.IsArray) {
				var list = (IList<DmdCustomAttributeTypedArgument>)Value;
				var elementType = ArgumentType.GetElementType();
				var sb = ObjectPools.AllocStringBuilder();
				sb.Append("new ");
				sb.Append(elementType.IsEnum ? elementType.FullName : elementType.Name);
				sb.Append('[');
				sb.Append(list.Count);
				sb.Append("] { ");
				for (int i = 0; i < list.Count; i++) {
					if (i != 0)
						sb.Append(", ");
					sb.Append(list[i].ToString(elementType != elementType.AppDomain.System_Object));
				}
				sb.Append(" }");
				return ObjectPools.FreeAndToString(ref sb);
			}
			return typed ? Value.ToString() : "(" + ArgumentType.Name + ")" + Value.ToString();
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DmdCustomAttributeTypedArgument left, DmdCustomAttributeTypedArgument right) => left.Equals(right);
		public static bool operator !=(DmdCustomAttributeTypedArgument left, DmdCustomAttributeTypedArgument right) => !left.Equals(right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DmdCustomAttributeTypedArgument other) => DmdMemberInfoEqualityComparer.DefaultType.Equals(ArgumentType) && Equals(Value, other.Value);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is DmdCustomAttributeTypedArgument other && Equals(other);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => DmdMemberInfoEqualityComparer.DefaultType.GetHashCode(ArgumentType) ^ (Value?.GetHashCode() ?? 0);
	}

	/// <summary>
	/// Custom attribute named argument
	/// </summary>
	public readonly struct DmdCustomAttributeNamedArgument : IEquatable<DmdCustomAttributeNamedArgument> {
		/// <summary>
		/// Gets the member (a property or a field)
		/// </summary>
		public DmdMemberInfo MemberInfo { get; }

		/// <summary>
		/// Gets the value
		/// </summary>
		public DmdCustomAttributeTypedArgument TypedValue { get; }

		/// <summary>
		/// Gets the member name
		/// </summary>
		public string MemberName => MemberInfo.Name;

		/// <summary>
		/// true if it's a field, false if it's a property
		/// </summary>
		public bool IsField => MemberInfo.MemberType == DmdMemberTypes.Field;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="memberInfo">A property or a field</param>
		/// <param name="typedArgument"></param>
		public DmdCustomAttributeNamedArgument(DmdMemberInfo memberInfo, DmdCustomAttributeTypedArgument typedArgument) {
			if ((object)typedArgument.ArgumentType == null)
				throw new ArgumentException();
			MemberInfo = memberInfo ?? throw new ArgumentNullException(nameof(memberInfo));
			TypedValue = typedArgument;
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(DmdCustomAttributeNamedArgument left, DmdCustomAttributeNamedArgument right) => left.Equals(right);
		public static bool operator !=(DmdCustomAttributeNamedArgument left, DmdCustomAttributeNamedArgument right) => !left.Equals(right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DmdCustomAttributeNamedArgument other) => MemberInfo == other.MemberInfo && TypedValue == other.TypedValue;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is DmdCustomAttributeNamedArgument other && Equals(other);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (MemberInfo?.GetHashCode() ?? 0) ^ TypedValue.GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if ((object)MemberInfo == null)
				return base.ToString();
			var argType = MemberInfo is DmdFieldInfo field ? field.FieldType : MemberInfo is DmdPropertyInfo property ? property.PropertyType : null;
			return MemberInfo.Name + " = " + TypedValue.ToString(argType != MemberInfo.AppDomain.System_Object);
		}
	}
}
