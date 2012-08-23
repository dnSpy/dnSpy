//
// SupportsIndexingCriterionTests.cs
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
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.CodeActions;
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class SupportsIndexingCriterionTests
	{
		ITypeResolveContext typeResolveContext;

		IType intType;

		ICompilation compilation;

		[SetUp]
		public void SetUp()
		{
			compilation = TestRefactoringContext.Create("").Compilation;
			typeResolveContext = compilation.TypeResolveContext;
			intType = GetIType<int>();
		}

		IType GetIType<T>()
		{
			return typeof(T).ToTypeReference().Resolve(typeResolveContext);
		}
		
		void AssertMatches(IType candidateType, IType elementType, bool isWriteAccess, params IType[] indexTypes)
		{
			var criterion = new SupportsIndexingCriterion(elementType, indexTypes, CSharpConversions.Get(compilation), isWriteAccess);
			Assert.IsTrue(criterion.SatisfiedBy(candidateType));
		}
		
		void AssertDoesNotMatch(IType candidateType, IType elementType, bool isWriteAccess, params IType[] indexTypes)
		{
			var criterion = new SupportsIndexingCriterion(elementType, indexTypes, CSharpConversions.Get(compilation), isWriteAccess);
			Assert.IsFalse(criterion.SatisfiedBy(candidateType));
		}
		
		[Test]
		public void ListWithOneIntegerIndex()
		{
			var intListType = GetIType<IList<int>>();
			AssertMatches(intListType, intType, false, intType);
			AssertMatches(intListType, intType, true, intType);
		}

		[Test]
		public void ListWithTwoIntegerIndexes()
		{
			var intListType = GetIType<IList<int>>();
			AssertDoesNotMatch(intListType, intType, false, intType, intType);
			AssertDoesNotMatch(intListType, intType, true, intType, intType);
		}
		
		[Test]
		public void ObjectCandidate()
		{
			AssertDoesNotMatch(GetIType<object>(), intType, false, intType);
			AssertDoesNotMatch(GetIType<object>(), intType, true, intType);
		}
	}
}

