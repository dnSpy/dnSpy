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
		readonly IMethod userDefinedOperatorMethod;
		readonly IList<ResolveResult> operands;
		readonly bool isLiftedOperator;
		
		public OperatorResolveResult(IType resultType, ExpressionType operatorType, params ResolveResult[] operands)
			: base(resultType)
		{
			if (operands == null)
				throw new ArgumentNullException("operands");
			this.operatorType = operatorType;
			this.operands = operands;
		}
		
		public OperatorResolveResult(IType resultType, ExpressionType operatorType, IMethod userDefinedOperatorMethod, bool isLiftedOperator, IList<ResolveResult> operands)
			: base(resultType)
		{
			if (operands == null)
				throw new ArgumentNullException("operands");
			this.operatorType = operatorType;
			this.userDefinedOperatorMethod = userDefinedOperatorMethod;
			this.isLiftedOperator = isLiftedOperator;
			this.operands = operands;
		}
		
		/// <summary>
		/// Gets the operator type.
		/// </summary>
		public ExpressionType OperatorType {
			get { return operatorType; }
		}
		
		/// <summary>
		/// Gets the operands.
		/// </summary>
		public IList<ResolveResult> Operands {
			get { return operands; }
		}
		
		/// <summary>
		/// Gets the user defined operator method.
		/// Returns null if this is a predefined operator.
		/// </summary>
		public IMethod UserDefinedOperatorMethod {
			get { return userDefinedOperatorMethod; }
		}
		
		/// <summary>
		/// Gets whether this is a lifted operator.
		/// </summary>
		public bool IsLiftedOperator {
			get { return isLiftedOperator; }
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return operands;
		}
	}
}
