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
using System.Linq.Expressions;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Semantics
{
	/// <summary>
	/// Represents a unary/binary/ternary operator invocation.
	/// </summary>
	public class OperatorResolveResult : ResolveResult
	{
		readonly ExpressionType operatorType;
		readonly ResolveResult[] operands;
		
		public OperatorResolveResult(IType resultType, ExpressionType expressionType, params ResolveResult[] operands)
			: base(resultType)
		{
			if (operands == null)
				throw new ArgumentNullException("arguments");
			this.operatorType = expressionType;
			this.operands = operands;
		}
		
		/// <summary>
		/// Gets the operator type.
		/// </summary>
		public ExpressionType OperatorType {
			get { return operatorType; }
		}
		
		public IList<ResolveResult> Operands {
			get { return operands; }
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return operands;
		}
	}
}
