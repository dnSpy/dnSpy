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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents the intersection of several types.
	/// </summary>
	public class IntersectionType : AbstractType
	{
		readonly ReadOnlyCollection<IType> types;
		
		public ReadOnlyCollection<IType> Types {
			get { return types; }
		}
		
		private IntersectionType(IType[] types)
		{
			Debug.Assert(types.Length >= 2);
			this.types = Array.AsReadOnly(types);
		}
		
		public static IType Create(IEnumerable<IType> types)
		{
			IType[] arr = types.Distinct().ToArray();
			foreach (IType type in arr) {
				if (type == null)
					throw new ArgumentNullException();
			}
			if (arr.Length == 0)
				return SpecialType.UnknownType;
			else if (arr.Length == 1)
				return arr[0];
			else
				return new IntersectionType(arr);
		}
		
		public override TypeKind Kind {
			get { return TypeKind.Intersection; }
		}
		
		public override string Name {
			get {
				StringBuilder b = new StringBuilder();
				foreach (var t in types) {
					if (b.Length > 0)
						b.Append(" & ");
					b.Append(t.Name);
				}
				return b.ToString();
			}
		}
		
		public override string ReflectionName {
			get {
				StringBuilder b = new StringBuilder();
				foreach (var t in types) {
					if (b.Length > 0)
						b.Append(" & ");
					b.Append(t.ReflectionName);
				}
				return b.ToString();
			}
		}
		
		public override bool? IsReferenceType {
			get {
				foreach (var t in types) {
					bool? isReferenceType = t.IsReferenceType;
					if (isReferenceType.HasValue)
						return isReferenceType.Value;
				}
				return null;
			}
		}
		
		public override int GetHashCode()
		{
			int hashCode = 0;
			unchecked {
				foreach (var t in types) {
					hashCode *= 7137517;
					hashCode += t.GetHashCode();
				}
			}
			return hashCode;
		}
		
		public override bool Equals(IType other)
		{
			IntersectionType o = other as IntersectionType;
			if (o != null && types.Count == o.types.Count) {
				for (int i = 0; i < types.Count; i++) {
					if (!types[i].Equals(o.types[i]))
						return false;
				}
				return true;
			}
			return false;
		}
		
		public override IEnumerable<IType> DirectBaseTypes {
			get { return types; }
		}
		
		public override ITypeReference ToTypeReference()
		{
			throw new NotSupportedException();
		}
		
		public override IEnumerable<IMethod> GetMethods(Predicate<IUnresolvedMethod> filter, GetMemberOptions options)
		{
			return GetMembersHelper.GetMethods(this, FilterNonStatic(filter), options);
		}
		
		public override IEnumerable<IMethod> GetMethods(IList<IType> typeArguments, Predicate<IUnresolvedMethod> filter, GetMemberOptions options)
		{
			return GetMembersHelper.GetMethods(this, typeArguments, filter, options);
		}
		
		public override IEnumerable<IProperty> GetProperties(Predicate<IUnresolvedProperty> filter, GetMemberOptions options)
		{
			return GetMembersHelper.GetProperties(this, FilterNonStatic(filter), options);
		}
		
		public override IEnumerable<IField> GetFields(Predicate<IUnresolvedField> filter, GetMemberOptions options)
		{
			return GetMembersHelper.GetFields(this, FilterNonStatic(filter), options);
		}
		
		public override IEnumerable<IEvent> GetEvents(Predicate<IUnresolvedEvent> filter, GetMemberOptions options)
		{
			return GetMembersHelper.GetEvents(this, FilterNonStatic(filter), options);
		}
		
		public override IEnumerable<IMember> GetMembers(Predicate<IUnresolvedMember> filter, GetMemberOptions options)
		{
			return GetMembersHelper.GetMembers(this, FilterNonStatic(filter), options);
		}
		
		public override IEnumerable<IMethod> GetAccessors(Predicate<IUnresolvedMethod> filter, GetMemberOptions options)
		{
			return GetMembersHelper.GetAccessors(this, FilterNonStatic(filter), options);
		}
		
		static Predicate<T> FilterNonStatic<T>(Predicate<T> filter) where T : class, IUnresolvedMember
		{
			if (filter == null)
				return member => !member.IsStatic;
			else
				return member => !member.IsStatic && filter(member);
		}
	}
}
