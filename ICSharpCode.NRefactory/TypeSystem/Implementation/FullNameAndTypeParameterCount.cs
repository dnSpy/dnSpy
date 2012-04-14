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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	[Serializable]
	public struct FullNameAndTypeParameterCount : IEquatable<FullNameAndTypeParameterCount>
	{
		public readonly string Namespace;
		public readonly string Name;
		public readonly int TypeParameterCount;
		
		public FullNameAndTypeParameterCount(string nameSpace, string name, int typeParameterCount)
		{
			if (nameSpace == null)
				throw new ArgumentNullException("nameSpace");
			if (name == null)
				throw new ArgumentNullException("name");
			this.Namespace = nameSpace;
			this.Name = name;
			this.TypeParameterCount = typeParameterCount;
		}
		
		public override bool Equals(object obj)
		{
			return (obj is FullNameAndTypeParameterCount) && Equals((FullNameAndTypeParameterCount)obj);
		}
		
		public bool Equals(FullNameAndTypeParameterCount other)
		{
			return this.Namespace == other.Namespace && this.Name == other.Name && this.TypeParameterCount == other.TypeParameterCount;
		}
		
		public override int GetHashCode()
		{
			return Name.GetHashCode() ^ Namespace.GetHashCode() ^ TypeParameterCount;
		}
		
		public static bool operator ==(FullNameAndTypeParameterCount lhs, FullNameAndTypeParameterCount rhs)
		{
			return lhs.Equals(rhs);
		}
		
		public static bool operator !=(FullNameAndTypeParameterCount lhs, FullNameAndTypeParameterCount rhs)
		{
			return !lhs.Equals(rhs);
		}
	}
	
	[Serializable]
	public sealed class FullNameAndTypeParameterCountComparer : IEqualityComparer<FullNameAndTypeParameterCount>
	{
		public static readonly FullNameAndTypeParameterCountComparer Ordinal = new FullNameAndTypeParameterCountComparer(StringComparer.Ordinal);
		public static readonly FullNameAndTypeParameterCountComparer OrdinalIgnoreCase = new FullNameAndTypeParameterCountComparer(StringComparer.OrdinalIgnoreCase);
		
		public readonly StringComparer NameComparer;
		
		public FullNameAndTypeParameterCountComparer(StringComparer nameComparer)
		{
			this.NameComparer = nameComparer;
		}
		
		public bool Equals(FullNameAndTypeParameterCount x, FullNameAndTypeParameterCount y)
		{
			return x.TypeParameterCount == y.TypeParameterCount
				&& NameComparer.Equals(x.Name, y.Name)
				&& NameComparer.Equals(x.Namespace, y.Namespace);
		}
		
		public int GetHashCode(FullNameAndTypeParameterCount obj)
		{
			return NameComparer.GetHashCode(obj.Name) ^ NameComparer.GetHashCode(obj.Namespace) ^ obj.TypeParameterCount;
		}
	}
}
