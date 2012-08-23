//
// HasMemberCriterion.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public class HasMemberCriterion : ITypeCriterion
	{
		IMember neededMember;
		IList<IMember> acceptableMembers;

		public HasMemberCriterion(IMember neededMember)
		{
			this.neededMember = neededMember;

			if (neededMember.ImplementedInterfaceMembers.Any()) {
				acceptableMembers = neededMember.ImplementedInterfaceMembers.ToList();
			} else if (neededMember.IsOverride) {
				acceptableMembers = new List<IMember>();
				foreach (var member in InheritanceHelper.GetBaseMembers(neededMember, true)) {
					acceptableMembers.Add(member);
					if (member.IsShadowing)
						break;
				}
				acceptableMembers.Add(neededMember);
			} else {
				acceptableMembers = new List<IMember> { neededMember };
			}
		}

		#region ITypeCriterion implementation
		public bool SatisfiedBy (IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			
			var typeMembers = type.GetMembers();
			return typeMembers.Any(member => HasCommonMemberDeclaration(acceptableMembers, member));
		}
		#endregion
		
		static bool HasCommonMemberDeclaration(IEnumerable<IMember> acceptableMembers, IMember member)
		{
			var implementedInterfaceMembers = member.MemberDefinition.ImplementedInterfaceMembers;
			if (implementedInterfaceMembers.Any()) {
				return ContainsAny(acceptableMembers, implementedInterfaceMembers);
			} else {
				return acceptableMembers.Contains(member/*.MemberDefinition*/);
			}
		}

		static bool ContainsAny<T>(IEnumerable<T> collection, IEnumerable<T> items)
		{
			foreach (var item in items) {
				if (collection.Contains(item))
					return true;
			}
			return false;
		}
	}
}

