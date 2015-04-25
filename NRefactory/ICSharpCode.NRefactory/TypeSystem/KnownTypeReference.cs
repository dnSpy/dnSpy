// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents some well-known types.
	/// </summary>
	public enum KnownTypeCode
	{
		// Note: DefaultResolvedTypeDefinition uses (KnownTypeCode)-1 as special value for "not yet calculated".
		// The order of type codes at the beginning must correspond to those in System.TypeCode.
		
		/// <summary>
		/// Not one of the known types.
		/// </summary>
		None,
		/// <summary><c>object</c> (System.Object)</summary>
		Object,
		/// <summary><c>System.DBNull</c></summary>
		DBNull,
		/// <summary><c>bool</c> (System.Boolean)</summary>
		Boolean,
		/// <summary><c>char</c> (System.Char)</summary>
		Char,
		/// <summary><c>sbyte</c> (System.SByte)</summary>
		SByte,
		/// <summary><c>byte</c> (System.Byte)</summary>
		Byte,
		/// <summary><c>short</c> (System.Int16)</summary>
		Int16,
		/// <summary><c>ushort</c> (System.UInt16)</summary>
		UInt16,
		/// <summary><c>int</c> (System.Int32)</summary>
		Int32,
		/// <summary><c>uint</c> (System.UInt32)</summary>
		UInt32,
		/// <summary><c>long</c> (System.Int64)</summary>
		Int64,
		/// <summary><c>ulong</c> (System.UInt64)</summary>
		UInt64,
		/// <summary><c>float</c> (System.Single)</summary>
		Single,
		/// <summary><c>double</c> (System.Double)</summary>
		Double,
		/// <summary><c>decimal</c> (System.Decimal)</summary>
		Decimal,
		/// <summary><c>System.DateTime</c></summary>
		DateTime,
		/// <summary><c>string</c> (System.String)</summary>
		String = 18,
		
		// String was the last element from System.TypeCode, now our additional known types start
		
		/// <summary><c>void</c> (System.Void)</summary>
		Void,
		/// <summary><c>System.Type</c></summary>
		Type,
		/// <summary><c>System.Array</c></summary>
		Array,
		/// <summary><c>System.Attribute</c></summary>
		Attribute,
		/// <summary><c>System.ValueType</c></summary>
		ValueType,
		/// <summary><c>System.Enum</c></summary>
		Enum,
		/// <summary><c>System.Delegate</c></summary>
		Delegate,
		/// <summary><c>System.MulticastDelegate</c></summary>
		MulticastDelegate,
		/// <summary><c>System.Exception</c></summary>
		Exception,
		/// <summary><c>System.IntPtr</c></summary>
		IntPtr,
		/// <summary><c>System.UIntPtr</c></summary>
		UIntPtr,
		/// <summary><c>System.Collections.IEnumerable</c></summary>
		IEnumerable,
		/// <summary><c>System.Collections.IEnumerator</c></summary>
		IEnumerator,
		/// <summary><c>System.Collections.Generic.IEnumerable{T}</c></summary>
		IEnumerableOfT,
		/// <summary><c>System.Collections.Generic.IEnumerator{T}</c></summary>
		IEnumeratorOfT,
		/// <summary><c>System.Collections.Generic.ICollection</c></summary>
		ICollection,
		/// <summary><c>System.Collections.Generic.ICollection{T}</c></summary>
		ICollectionOfT,
		/// <summary><c>System.Collections.Generic.IList</c></summary>
		IList,
		/// <summary><c>System.Collections.Generic.IList{T}</c></summary>
		IListOfT,
		/// <summary><c>System.Collections.Generic.IReadOnlyCollection{T}</c></summary>
		IReadOnlyCollectionOfT,
		/// <summary><c>System.Collections.Generic.IReadOnlyList{T}</c></summary>
		IReadOnlyListOfT,
		/// <summary><c>System.Threading.Tasks.Task</c></summary>
		Task,
		/// <summary><c>System.Threading.Tasks.Task{T}</c></summary>
		TaskOfT,
		/// <summary><c>System.Nullable{T}</c></summary>
		NullableOfT,
		/// <summary><c>System.IDisposable</c></summary>
		IDisposable,
		/// <summary><c>System.Runtime.CompilerServices.INotifyCompletion</c></summary>
		INotifyCompletion,
		/// <summary><c>System.Runtime.CompilerServices.ICriticalNotifyCompletion</c></summary>
		ICriticalNotifyCompletion,
	}
	
	/// <summary>
	/// Contains well-known type references.
	/// </summary>
	[Serializable]
	public sealed class KnownTypeReference : ITypeReference
	{
		internal const int KnownTypeCodeCount = (int)KnownTypeCode.ICriticalNotifyCompletion + 1;
		
		static readonly KnownTypeReference[] knownTypeReferences = new KnownTypeReference[KnownTypeCodeCount] {
			null, // None
			new KnownTypeReference(KnownTypeCode.Object,   "System", "Object", baseType: KnownTypeCode.None),
			new KnownTypeReference(KnownTypeCode.DBNull,   "System", "DBNull"),
			new KnownTypeReference(KnownTypeCode.Boolean,  "System", "Boolean",  baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.Char,     "System", "Char",     baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.SByte,    "System", "SByte",    baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.Byte,     "System", "Byte",     baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.Int16,    "System", "Int16",    baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.UInt16,   "System", "UInt16",   baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.Int32,    "System", "Int32",    baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.UInt32,   "System", "UInt32",   baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.Int64,    "System", "Int64",    baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.UInt64,   "System", "UInt64",   baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.Single,   "System", "Single",   baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.Double,   "System", "Double",   baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.Decimal,  "System", "Decimal",  baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.DateTime, "System", "DateTime", baseType: KnownTypeCode.ValueType),
			null,
			new KnownTypeReference(KnownTypeCode.String,    "System", "String"),
			new KnownTypeReference(KnownTypeCode.Void,      "System", "Void"),
			new KnownTypeReference(KnownTypeCode.Type,      "System", "Type"),
			new KnownTypeReference(KnownTypeCode.Array,     "System", "Array"),
			new KnownTypeReference(KnownTypeCode.Attribute, "System", "Attribute"),
			new KnownTypeReference(KnownTypeCode.ValueType, "System", "ValueType"),
			new KnownTypeReference(KnownTypeCode.Enum,      "System", "Enum", baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.Delegate,  "System", "Delegate"),
			new KnownTypeReference(KnownTypeCode.MulticastDelegate, "System", "MulticastDelegate", baseType: KnownTypeCode.Delegate),
			new KnownTypeReference(KnownTypeCode.Exception, "System", "Exception"),
			new KnownTypeReference(KnownTypeCode.IntPtr,    "System", "IntPtr", baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.UIntPtr,   "System", "UIntPtr", baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.IEnumerable,    "System.Collections", "IEnumerable"),
			new KnownTypeReference(KnownTypeCode.IEnumerator,    "System.Collections", "IEnumerator"),
			new KnownTypeReference(KnownTypeCode.IEnumerableOfT, "System.Collections.Generic", "IEnumerable", 1),
			new KnownTypeReference(KnownTypeCode.IEnumeratorOfT, "System.Collections.Generic", "IEnumerator", 1),
			new KnownTypeReference(KnownTypeCode.ICollection,    "System.Collections", "ICollection"),
			new KnownTypeReference(KnownTypeCode.ICollectionOfT, "System.Collections.Generic", "ICollection", 1),
			new KnownTypeReference(KnownTypeCode.IList,          "System.Collections", "IList"),
			new KnownTypeReference(KnownTypeCode.IListOfT,       "System.Collections.Generic", "IList", 1),

			new KnownTypeReference(KnownTypeCode.IReadOnlyCollectionOfT, "System.Collections.Generic", "IReadOnlyCollection", 1),
			new KnownTypeReference(KnownTypeCode.IReadOnlyListOfT, "System.Collections.Generic", "IReadOnlyList", 1),
			new KnownTypeReference(KnownTypeCode.Task,        "System.Threading.Tasks", "Task"),
			new KnownTypeReference(KnownTypeCode.TaskOfT,     "System.Threading.Tasks", "Task", 1, baseType: KnownTypeCode.Task),
			new KnownTypeReference(KnownTypeCode.NullableOfT, "System", "Nullable", 1, baseType: KnownTypeCode.ValueType),
			new KnownTypeReference(KnownTypeCode.IDisposable, "System", "IDisposable"),
			new KnownTypeReference(KnownTypeCode.INotifyCompletion, "System.Runtime.CompilerServices", "INotifyCompletion"),
			new KnownTypeReference(KnownTypeCode.ICriticalNotifyCompletion, "System.Runtime.CompilerServices", "ICriticalNotifyCompletion"),
		};
		
		/// <summary>
		/// Gets the known type reference for the specified type code.
		/// Returns null for KnownTypeCode.None.
		/// </summary>
		public static KnownTypeReference Get(KnownTypeCode typeCode)
		{
			return knownTypeReferences[(int)typeCode];
		}
		
		/// <summary>
		/// Gets a type reference pointing to the <c>object</c> type.
		/// </summary>
		public static readonly KnownTypeReference Object = Get(KnownTypeCode.Object);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.DBNull</c> type.
		/// </summary>
		public static readonly KnownTypeReference DBNull = Get(KnownTypeCode.DBNull);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>bool</c> type.
		/// </summary>
		public static readonly KnownTypeReference Boolean = Get(KnownTypeCode.Boolean);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>char</c> type.
		/// </summary>
		public static readonly KnownTypeReference Char = Get(KnownTypeCode.Char);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>sbyte</c> type.
		/// </summary>
		public static readonly KnownTypeReference SByte = Get(KnownTypeCode.SByte);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>byte</c> type.
		/// </summary>
		public static readonly KnownTypeReference Byte = Get(KnownTypeCode.Byte);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>short</c> type.
		/// </summary>
		public static readonly KnownTypeReference Int16 = Get(KnownTypeCode.Int16);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>ushort</c> type.
		/// </summary>
		public static readonly KnownTypeReference UInt16 = Get(KnownTypeCode.UInt16);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>int</c> type.
		/// </summary>
		public static readonly KnownTypeReference Int32 = Get(KnownTypeCode.Int32);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>uint</c> type.
		/// </summary>
		public static readonly KnownTypeReference UInt32 = Get(KnownTypeCode.UInt32);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>long</c> type.
		/// </summary>
		public static readonly KnownTypeReference Int64 = Get(KnownTypeCode.Int64);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>ulong</c> type.
		/// </summary>
		public static readonly KnownTypeReference UInt64 = Get(KnownTypeCode.UInt64);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>float</c> type.
		/// </summary>
		public static readonly KnownTypeReference Single = Get(KnownTypeCode.Single);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>double</c> type.
		/// </summary>
		public static readonly KnownTypeReference Double = Get(KnownTypeCode.Double);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>decimal</c> type.
		/// </summary>
		public static readonly KnownTypeReference Decimal = Get(KnownTypeCode.Decimal);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.DateTime</c> type.
		/// </summary>
		public static readonly KnownTypeReference DateTime = Get(KnownTypeCode.DateTime);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>string</c> type.
		/// </summary>
		public static readonly KnownTypeReference String = Get(KnownTypeCode.String);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>void</c> type.
		/// </summary>
		public static readonly KnownTypeReference Void = Get(KnownTypeCode.Void);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Type</c> type.
		/// </summary>
		public static readonly KnownTypeReference Type = Get(KnownTypeCode.Type);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Array</c> type.
		/// </summary>
		public static readonly KnownTypeReference Array = Get(KnownTypeCode.Array);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Attribute</c> type.
		/// </summary>
		public static readonly KnownTypeReference Attribute = Get(KnownTypeCode.Attribute);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.ValueType</c> type.
		/// </summary>
		public static readonly KnownTypeReference ValueType = Get(KnownTypeCode.ValueType);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Enum</c> type.
		/// </summary>
		public static readonly KnownTypeReference Enum = Get(KnownTypeCode.Enum);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Delegate</c> type.
		/// </summary>
		public static readonly KnownTypeReference Delegate = Get(KnownTypeCode.Delegate);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.MulticastDelegate</c> type.
		/// </summary>
		public static readonly KnownTypeReference MulticastDelegate = Get(KnownTypeCode.MulticastDelegate);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Exception</c> type.
		/// </summary>
		public static readonly KnownTypeReference Exception = Get(KnownTypeCode.Exception);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.IntPtr</c> type.
		/// </summary>
		public static readonly KnownTypeReference IntPtr = Get(KnownTypeCode.IntPtr);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.UIntPtr</c> type.
		/// </summary>
		public static readonly KnownTypeReference UIntPtr = Get(KnownTypeCode.UIntPtr);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Collections.IEnumerable</c> type.
		/// </summary>
		public static readonly KnownTypeReference IEnumerable = Get(KnownTypeCode.IEnumerable);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Collections.IEnumerator</c> type.
		/// </summary>
		public static readonly KnownTypeReference IEnumerator = Get(KnownTypeCode.IEnumerator);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Collections.Generic.IEnumerable{T}</c> type.
		/// </summary>
		public static readonly KnownTypeReference IEnumerableOfT = Get(KnownTypeCode.IEnumerableOfT);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Collections.Generic.IEnumerator{T}</c> type.
		/// </summary>
		public static readonly KnownTypeReference IEnumeratorOfT = Get(KnownTypeCode.IEnumeratorOfT);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Collections.ICollection</c> type.
		/// </summary>
		public static readonly KnownTypeReference ICollection = Get(KnownTypeCode.ICollection);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Collections.Generic.ICollection{T}</c> type.
		/// </summary>
		public static readonly KnownTypeReference ICollectionOfT = Get(KnownTypeCode.ICollectionOfT);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Collections.IList</c> type.
		/// </summary>
		public static readonly KnownTypeReference IList = Get(KnownTypeCode.IList);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Collections.Generic.IList{T}</c> type.
		/// </summary>
		public static readonly KnownTypeReference IListOfT = Get(KnownTypeCode.IListOfT);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Collections.Generic.IReadOnlyCollection{T}</c> type.
		/// </summary>
		public static readonly KnownTypeReference IReadOnlyCollectionOfT = Get(KnownTypeCode.IReadOnlyCollectionOfT);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Collections.Generic.IReadOnlyList{T}</c> type.
		/// </summary>
		public static readonly KnownTypeReference IReadOnlyListOfT = Get(KnownTypeCode.IReadOnlyListOfT);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Threading.Tasks.Task</c> type.
		/// </summary>
		public static readonly KnownTypeReference Task = Get(KnownTypeCode.Task);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Threading.Tasks.Task{T}</c> type.
		/// </summary>
		public static readonly KnownTypeReference TaskOfT = Get(KnownTypeCode.TaskOfT);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Nullable{T}</c> type.
		/// </summary>
		public static readonly KnownTypeReference NullableOfT = Get(KnownTypeCode.NullableOfT);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.IDisposable</c> type.
		/// </summary>
		public static readonly KnownTypeReference IDisposable = Get(KnownTypeCode.IDisposable);

		/// <summary>
		/// Gets a type reference pointing to the <c>System.Runtime.CompilerServices.INotifyCompletion</c> type.
		/// </summary>
		public static readonly KnownTypeReference INotifyCompletion = Get(KnownTypeCode.INotifyCompletion);

		/// <summary>
		/// Gets a type reference pointing to the <c>System.Runtime.CompilerServices.ICriticalNotifyCompletion</c> type.
		/// </summary>
		public static readonly KnownTypeReference ICriticalNotifyCompletion = Get(KnownTypeCode.ICriticalNotifyCompletion);

		readonly KnownTypeCode knownTypeCode;
		readonly string namespaceName;
		readonly string name;
		readonly int typeParameterCount;
		internal readonly KnownTypeCode baseType;
		
		private KnownTypeReference(KnownTypeCode knownTypeCode, string namespaceName, string name, int typeParameterCount = 0, KnownTypeCode baseType = KnownTypeCode.Object)
		{
			this.knownTypeCode = knownTypeCode;
			this.namespaceName = namespaceName;
			this.name = name;
			this.typeParameterCount = typeParameterCount;
			this.baseType = baseType;
		}
		
		public KnownTypeCode KnownTypeCode {
			get { return knownTypeCode; }
		}
		
		public string Namespace {
			get { return namespaceName; }
		}
		
		public string Name {
			get { return name; }
		}
		
		public int TypeParameterCount {
			get { return typeParameterCount; }
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			return context.Compilation.FindType(knownTypeCode);
		}
		
		public override string ToString()
		{
			return GetCSharpNameByTypeCode(knownTypeCode) ?? (this.Namespace + "." + this.Name);
		}
		
		/// <summary>
		/// Gets the C# primitive type name from the known type code.
		/// Returns null if there is no primitive name for the specified type.
		/// </summary>
		public static string GetCSharpNameByTypeCode(KnownTypeCode knownTypeCode)
		{
			switch (knownTypeCode) {
				case KnownTypeCode.Object:
					return "object";
				case KnownTypeCode.Boolean:
					return "bool";
				case KnownTypeCode.Char:
					return "char";
				case KnownTypeCode.SByte:
					return "sbyte";
				case KnownTypeCode.Byte:
					return "byte";
				case KnownTypeCode.Int16:
					return "short";
				case KnownTypeCode.UInt16:
					return "ushort";
				case KnownTypeCode.Int32:
					return "int";
				case KnownTypeCode.UInt32:
					return "uint";
				case KnownTypeCode.Int64:
					return "long";
				case KnownTypeCode.UInt64:
					return "ulong";
				case KnownTypeCode.Single:
					return "float";
				case KnownTypeCode.Double:
					return "double";
				case KnownTypeCode.Decimal:
					return "decimal";
				case KnownTypeCode.String:
					return "string";
				case KnownTypeCode.Void:
					return "void";
				default:
					return null;
			}
		}
	}
}
