// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Contains well-known type references.
	/// </summary>
	[Serializable]
	public sealed class KnownTypeReference : ITypeReference
	{
		/// <summary>
		/// Gets a type reference pointing to the <c>void</c> type.
		/// </summary>
		public static readonly ITypeReference Void = new KnownTypeReference(TypeCode.Empty);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>object</c> type.
		/// </summary>
		public static readonly ITypeReference Object = new KnownTypeReference(TypeCode.Object);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>bool</c> type.
		/// </summary>
		public static readonly ITypeReference Boolean = new KnownTypeReference(TypeCode.Boolean);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>char</c> type.
		/// </summary>
		public static readonly ITypeReference Char = new KnownTypeReference(TypeCode.Char);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>sbyte</c> type.
		/// </summary>
		public static readonly ITypeReference SByte = new KnownTypeReference(TypeCode.SByte);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>byte</c> type.
		/// </summary>
		public static readonly ITypeReference Byte = new KnownTypeReference(TypeCode.Byte);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>short</c> type.
		/// </summary>
		public static readonly ITypeReference Int16 = new KnownTypeReference(TypeCode.Int16);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>ushort</c> type.
		/// </summary>
		public static readonly ITypeReference UInt16 = new KnownTypeReference(TypeCode.UInt16);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>int</c> type.
		/// </summary>
		public static readonly ITypeReference Int32 = new KnownTypeReference(TypeCode.Int32);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>uint</c> type.
		/// </summary>
		public static readonly ITypeReference UInt32 = new KnownTypeReference(TypeCode.UInt32);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>long</c> type.
		/// </summary>
		public static readonly ITypeReference Int64 = new KnownTypeReference(TypeCode.Int64);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>ulong</c> type.
		/// </summary>
		public static readonly ITypeReference UInt64 = new KnownTypeReference(TypeCode.UInt64);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>float</c> type.
		/// </summary>
		public static readonly ITypeReference Single = new KnownTypeReference(TypeCode.Single);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>double</c> type.
		/// </summary>
		public static readonly ITypeReference Double = new KnownTypeReference(TypeCode.Double);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>decimal</c> type.
		/// </summary>
		public static readonly ITypeReference Decimal = new KnownTypeReference(TypeCode.Decimal);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>string</c> type.
		/// </summary>
		public static readonly ITypeReference String = new KnownTypeReference(TypeCode.String);
		
		/// <summary>
		/// Gets a type reference pointing to the <c>System.Type</c> type.
		/// </summary>
		public static readonly ITypeReference Type = new GetClassTypeReference("System", "Type", 0);
		
		/// <summary>
		/// Gets all known type references.
		/// </summary>
		public static IEnumerable<ITypeReference> AllKnownTypeReferences {
			get {
				return new[] {
					Void, Object, Boolean,
					SByte, Byte, Int16, UInt16, Int32, UInt32, Int64, UInt64,
					String, Char, Single, Double, Decimal,
					Type
				};
			}
		}
		
		readonly TypeCode typeCode;
		
		public TypeCode TypeCode {
			get { return typeCode; }
		}
		
		public KnownTypeReference(TypeCode typeCode)
		{
			this.typeCode = typeCode;
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			return context.GetKnownTypeDefinition(typeCode) ?? SharedTypes.UnknownType;
		}
		
		public string Namespace {
			get { return "System"; }
		}
		
		public string Name {
			get { return ReflectionHelper.GetShortNameByTypeCode(typeCode); }
		}
		
		public override string ToString()
		{
			return ReflectionHelper.GetCSharpNameByTypeCode(typeCode) ?? (this.Namespace + "." + this.Name);
		}
	}
}
