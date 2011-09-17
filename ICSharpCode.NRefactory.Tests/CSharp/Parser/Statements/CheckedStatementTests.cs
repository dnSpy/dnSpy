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
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.Statements
{
	[TestFixture]
	public class CheckedStatementTests
	{
		[Test]
		public void CheckedStatementTest()
		{
			CheckedStatement checkedStatement = ParseUtilCSharp.ParseStatement<CheckedStatement>("checked { }");
			Assert.IsFalse(checkedStatement.Body.IsNull);
		}
		
		[Test]
		public void CheckedStatementAndExpressionTest()
		{
			CheckedStatement checkedStatement = ParseUtilCSharp.ParseStatement<CheckedStatement>("checked { checked(++i).ToString(); }");
			ExpressionStatement es = (ExpressionStatement)checkedStatement.Body.Statements.Single();
			CheckedExpression ce = (CheckedExpression)((MemberReferenceExpression)((InvocationExpression)es.Expression).Target).Target;
			Assert.IsTrue(ce.Expression is UnaryOperatorExpression);
		}
		
		[Test]
		public void UncheckedStatementTest()
		{
			UncheckedStatement uncheckedStatement = ParseUtilCSharp.ParseStatement<UncheckedStatement>("unchecked { }");
			Assert.IsFalse(uncheckedStatement.Body.IsNull);
		}
		
		[Test]
		public void UncheckedStatementAndExpressionTest()
		{
			UncheckedStatement uncheckedStatement = ParseUtilCSharp.ParseStatement<UncheckedStatement>("unchecked { unchecked(++i).ToString(); }");
			ExpressionStatement es = (ExpressionStatement)uncheckedStatement.Body.Statements.Single();
			UncheckedExpression ce = (UncheckedExpression)((MemberReferenceExpression)((InvocationExpression)es.Expression).Target).Target;
			Assert.IsTrue(ce.Expression is UnaryOperatorExpression);
		}
	}
}
