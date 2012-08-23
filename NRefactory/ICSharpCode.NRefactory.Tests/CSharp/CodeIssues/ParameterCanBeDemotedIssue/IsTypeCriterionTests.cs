//
// IsTypeCriterionTests.cs
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
using NUnit.Framework;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class IsTypeCriterionTests
	{
		[Test]
		public void SameType()
		{
			IType type = new MockType();
			
			var criterion = new IsTypeCriterion(type);
			Assert.IsTrue(criterion.SatisfiedBy(type));
		}

		[Test]
		public void InheritedType()
		{
			IType baseType = new MockType();
			var type = new MockType() {
				BaseTypes = { baseType }
			};
			
			var criterion = new IsTypeCriterion(baseType);
			Assert.IsTrue(criterion.SatisfiedBy(type));
		}
	}

	class MockType : AbstractType
	{
		string name;

		bool? isReferenceType;

		TypeKind kind;

		public MockType(string name = "", bool? isReferenceType = false, TypeKind kind = TypeKind.Class)
		{
			this.name = name;
			this.isReferenceType = isReferenceType;
			this.kind = kind;
			BaseTypes = new List<IType>();
		}

		#region implemented abstract members of ICSharpCode.NRefactory.TypeSystem.Implementation.AbstractType
		public override string Name {
			get {
				return name;
			}
		}

		public override bool? IsReferenceType {
			get {
				return isReferenceType;
			}
		}

		public override TypeKind Kind {
			get {
				return kind;
			}
		}

		public override ITypeReference ToTypeReference ()
		{
			throw new System.NotImplementedException ();
		}
		#endregion

		#region convenience overrides

		public override IEnumerable<IType> DirectBaseTypes {
			get {
				return BaseTypes;
			}
		}

		public IList<IType> BaseTypes {
			get;
			set;
		}
		#endregion
	}
}

