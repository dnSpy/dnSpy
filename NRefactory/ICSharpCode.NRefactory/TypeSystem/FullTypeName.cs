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
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Holds the full name of a type definition.
	/// A full type name uniquely identifies a type definition within a single assembly.
	/// </summary>
	/// <remarks>
	/// A full type name can only represent type definitions, not arbitrary types.
	/// It does not include any type arguments, and can not refer to array or pointer types.
	/// 
	/// A full type name represented as reflection name has the syntax:
	/// <c>NamespaceName '.' TopLevelTypeName ['`'#] { '+' NestedTypeName ['`'#] }</c>
	/// </remarks>
	[Serializable]
	public struct FullTypeName : IEquatable<FullTypeName>
	{
		[Serializable]
		struct NestedTypeName
		{
			public readonly string Name;
			public readonly int AdditionalTypeParameterCount;
			
			public NestedTypeName(string name, int additionalTypeParameterCount)
			{
				if (name == null)
					throw new ArgumentNullException("name");
				this.Name = name;
				this.AdditionalTypeParameterCount = additionalTypeParameterCount;
			}
		}
		
		readonly TopLevelTypeName topLevelType;
		readonly NestedTypeName[] nestedTypes;
		
		FullTypeName(TopLevelTypeName topLevelTypeName, NestedTypeName[] nestedTypes)
		{
			this.topLevelType = topLevelTypeName;
			this.nestedTypes = nestedTypes;
		}
		
		/// <summary>
		/// Constructs a FullTypeName representing the given top-level type.
		/// </summary>
		/// <remarks>
		/// FullTypeName has an implicit conversion operator from TopLevelTypeName,
		/// so you can simply write:
		/// <c>FullTypeName f = new TopLevelTypeName(...);</c>
		/// </remarks>
		public FullTypeName(TopLevelTypeName topLevelTypeName)
		{
			this.topLevelType = topLevelTypeName;
			this.nestedTypes = null;
		}
		
		/// <summary>
		/// Constructs a FullTypeName by parsing the given reflection name.
		/// Note that FullTypeName can only represent type definition names. If the reflection name
		/// might refer to a parameterized type or array etc., use
		/// <see cref="ReflectionHelper.ParseReflectionName(string)"/> instead.
		/// </summary>
		/// <remarks>
		/// Expected syntax: <c>NamespaceName '.' TopLevelTypeName ['`'#] { '+' NestedTypeName ['`'#] }</c>
		/// where # are type parameter counts
		/// </remarks>
		public FullTypeName(string reflectionName)
		{
			int pos = reflectionName.IndexOf('+');
			if (pos < 0) {
				// top-level type
				this.topLevelType = new TopLevelTypeName(reflectionName);
				this.nestedTypes = null;
			} else {
				// nested type
				string[] parts = reflectionName.Split('+');
				this.topLevelType = new TopLevelTypeName(parts[0]);
				this.nestedTypes = new NestedTypeName[parts.Length - 1];
				for (int i = 0; i < nestedTypes.Length; i++) {
					int tpc;
					string name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(parts[i + 1], out tpc);
					nestedTypes[i] = new NestedTypeName(name, tpc);
				}
			}
		}
		
		/// <summary>
		/// Gets the top-level type name.
		/// </summary>
		public TopLevelTypeName TopLevelTypeName {
			get { return topLevelType; }
		}
		
		/// <summary>
		/// Gets whether this is a nested type.
		/// </summary>
		public bool IsNested {
			get {
				return nestedTypes != null;
			}
		}
		
		/// <summary>
		/// Gets the nesting level.
		/// </summary>
		public int NestingLevel {
			get {
				return nestedTypes != null ? nestedTypes.Length : 0;
			}
		}
		
		/// <summary>
		/// Gets the name of the type.
		/// For nested types, this is the name of the innermost type.
		/// </summary>
		public string Name {
			get {
				if (nestedTypes != null)
					return nestedTypes[nestedTypes.Length - 1].Name;
				else
					return topLevelType.Name;
			}
		}
		
		public string ReflectionName {
			get {
				if (nestedTypes == null)
					return topLevelType.ReflectionName;
				StringBuilder b = new StringBuilder(topLevelType.ReflectionName);
				foreach (NestedTypeName nt in nestedTypes) {
					b.Append('+');
					b.Append(nt.Name);
					if (nt.AdditionalTypeParameterCount > 0) {
						b.Append('`');
						b.Append(nt.AdditionalTypeParameterCount);
					}
				}
				return b.ToString();
			}
		}
		
		/// <summary>
		/// Gets the total type parameter count.
		/// </summary>
		public int TypeParameterCount {
			get {
				int tpc = topLevelType.TypeParameterCount;
				if (nestedTypes != null) {
					foreach (var nt in nestedTypes) {
						tpc += nt.AdditionalTypeParameterCount;
					}
				}
				return tpc;
			}
		}
		
		/// <summary>
		/// Gets the name of the nested type at the given level.
		/// </summary>
		public string GetNestedTypeName(int nestingLevel)
		{
			if (nestedTypes == null)
				throw new InvalidOperationException();
			return nestedTypes[nestingLevel].Name;
		}
		
		/// <summary>
		/// Gets the number of additional type parameters of the nested type at the given level.
		/// </summary>
		public int GetNestedTypeAdditionalTypeParameterCount(int nestingLevel)
		{
			if (nestedTypes == null)
				throw new InvalidOperationException();
			return nestedTypes[nestingLevel].AdditionalTypeParameterCount;
		}
		
		/// <summary>
		/// Gets the declaring type name.
		/// </summary>
		/// <exception cref="InvalidOperationException">This is a top-level type name.</exception>
		/// <example><c>new FullTypeName("NS.A+B+C").GetDeclaringType()</c> will return <c>new FullTypeName("NS.A+B")</c></example>
		public FullTypeName GetDeclaringType()
		{
			if (nestedTypes == null)
				throw new InvalidOperationException();
			if (nestedTypes.Length == 1)
				return topLevelType;
			NestedTypeName[] outerNestedTypeNames = new NestedTypeName[nestedTypes.Length - 1];
			Array.Copy(nestedTypes, 0, outerNestedTypeNames, 0, outerNestedTypeNames.Length);
			return new FullTypeName(topLevelType, nestedTypes);
		}
		
		/// <summary>
		/// Creates a nested type name.
		/// </summary>
		/// <example><c>new FullTypeName("NS.A+B").NestedType("C", 1)</c> will return <c>new FullTypeName("NS.A+B+C`1")</c></example>
		public FullTypeName NestedType(string name, int additionalTypeParameterCount)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			var newNestedType = new NestedTypeName(name, additionalTypeParameterCount);
			if (nestedTypes == null)
				return new FullTypeName(topLevelType, new[] { newNestedType });
			NestedTypeName[] newNestedTypeNames = new NestedTypeName[nestedTypes.Length + 1];
			nestedTypes.CopyTo(newNestedTypeNames, 0);
			newNestedTypeNames[newNestedTypeNames.Length - 1] = newNestedType;
			return new FullTypeName(topLevelType, newNestedTypeNames);
		}
		
		public static implicit operator FullTypeName(TopLevelTypeName topLevelTypeName)
		{
			return new FullTypeName(topLevelTypeName);
		}
		
		public override string ToString()
		{
			return this.ReflectionName;
		}
		
		#region Equals and GetHashCode implementation
		public override bool Equals(object obj)
		{
			return obj is FullTypeName && Equals((FullTypeName)obj);
		}
		
		public bool Equals(FullTypeName other)
		{
			return FullTypeNameComparer.Ordinal.Equals(this, other);
		}
		
		public override int GetHashCode()
		{
			return FullTypeNameComparer.Ordinal.GetHashCode(this);
		}
		
		public static bool operator ==(FullTypeName left, FullTypeName right)
		{
			return left.Equals(right);
		}
		
		public static bool operator !=(FullTypeName left, FullTypeName right)
		{
			return !left.Equals(right);
		}
		#endregion
	}
	
	[Serializable]
	public sealed class FullTypeNameComparer : IEqualityComparer<FullTypeName>
	{
		public static readonly FullTypeNameComparer Ordinal = new FullTypeNameComparer(StringComparer.Ordinal);
		public static readonly FullTypeNameComparer OrdinalIgnoreCase = new FullTypeNameComparer(StringComparer.OrdinalIgnoreCase);
		
		public readonly StringComparer NameComparer;
		
		public FullTypeNameComparer(StringComparer nameComparer)
		{
			this.NameComparer = nameComparer;
		}
		
		public bool Equals(FullTypeName x, FullTypeName y)
		{
			if (x.NestingLevel != y.NestingLevel)
				return false;
			TopLevelTypeName topX = x.TopLevelTypeName;
			TopLevelTypeName topY = y.TopLevelTypeName;
			if (topX.TypeParameterCount == topY.TypeParameterCount
			    && NameComparer.Equals(topX.Name, topY.Name)
			    && NameComparer.Equals(topX.Namespace, topY.Namespace))
			{
				for (int i = 0; i < x.NestingLevel; i++) {
					if (x.GetNestedTypeAdditionalTypeParameterCount(i) != y.GetNestedTypeAdditionalTypeParameterCount(i))
						return false;
					if (!NameComparer.Equals(x.GetNestedTypeName(i), y.GetNestedTypeName(i)))
						return false;
				}
				return true;
			}
			return false;
		}
		
		public int GetHashCode(FullTypeName obj)
		{
			TopLevelTypeName top = obj.TopLevelTypeName;
			int hash = NameComparer.GetHashCode(top.Name) ^ NameComparer.GetHashCode(top.Namespace) ^ top.TypeParameterCount;
			unchecked {
				for (int i = 0; i < obj.NestingLevel; i++) {
					hash *= 31;
					hash += NameComparer.GetHashCode(obj.Name) ^ obj.TypeParameterCount;
				}
			}
			return hash;
		}
	}
}
