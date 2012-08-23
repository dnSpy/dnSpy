//
// FormatStringHelper.cs
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
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp
{
	class FormatStringHelper
	{
		static string[] parameterNames = { "format", "frmt", "fmt" };
		
		public static bool TryGetFormattingParameters(CSharpInvocationResolveResult invocationResolveResult, InvocationExpression invocationExpression,
		                                     		  out Expression formatArgument, out TextLocation formatStart, out IList<Expression> arguments,
		                                              Func<IParameter, Expression, bool> argumentFilter)
		{
			if (argumentFilter == null)
				argumentFilter = (p, e) => true;

			formatArgument = null;
			formatStart = default(TextLocation);
			arguments = new List<Expression>();
			var argumentToParameterMap = invocationResolveResult.GetArgumentToParameterMap();
			var resolvedParameters = invocationResolveResult.Member.Parameters;
			var allArguments = invocationExpression.Arguments.ToArray();
			for (int i = 0; i < allArguments.Length; i++) {
				var parameterIndex = argumentToParameterMap[i];
				if (parameterIndex < 0 || parameterIndex >= resolvedParameters.Count) {
					// No valid mapping for this parameter, skip it
					continue;
				}
				var parameter = resolvedParameters[parameterIndex];
				var argument = allArguments[i];
				if (parameter.Type.IsKnownType(KnownTypeCode.String) && parameterNames.Contains(parameter.Name)) {
					formatArgument = argument;
					formatStart = argument.StartLocation;
				} else if ((formatArgument != null || parameter.IsParams) && argumentFilter(parameter, argument)) {
					arguments.Add(argument);
				}
			}
			return formatArgument != null;
		}
	}
}

