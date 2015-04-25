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
	static class FormatStringHelper
	{
		static readonly string[] parameterNames = { "format", "frmt", "fmt" };
		
		public static bool TryGetFormattingParameters(CSharpInvocationResolveResult invocationResolveResult, InvocationExpression invocationExpression,
		                                     		  out Expression formatArgument, out IList<Expression> arguments,
		                                              Func<IParameter, Expression, bool> argumentFilter)
		{
			if (argumentFilter == null)
				argumentFilter = (p, e) => true;

			formatArgument = null;
			arguments = new List<Expression>();

			// Serach for method of type: void Name(string format, params object[] args);
			if (invocationResolveResult.Member.SymbolKind == SymbolKind.Method) {
				var methods = invocationResolveResult.Member.DeclaringType.GetMethods(m => m.Name == invocationResolveResult.Member.Name).ToList();
				if (!methods.Any(m => m.Parameters.Count == 2 && 
					m.Parameters[0].Type.IsKnownType(KnownTypeCode.String) && parameterNames.Contains(m.Parameters[0].Name) && 
					m.Parameters[1].IsParams))
					return false;
			}

			var argumentToParameterMap = invocationResolveResult.GetArgumentToParameterMap();
			var resolvedParameters = invocationResolveResult.Member.Parameters;
			var allArguments = invocationExpression.Arguments.ToArray();
			for (int i = 0; i < allArguments.Length; i++) {
				var parameterIndex = argumentToParameterMap[i];
				if (parameterIndex < 0 || parameterIndex >= resolvedParameters.Count) {
					// No valid mapping for this argument, skip it
					continue;
				}
				var parameter = resolvedParameters[parameterIndex];
				var argument = allArguments[i];
				if (i == 0 && parameter.Type.IsKnownType(KnownTypeCode.String) && parameterNames.Contains(parameter.Name)) {
					formatArgument = argument;
				} else if (formatArgument != null && parameter.IsParams && !invocationResolveResult.IsExpandedForm) {
					var ace = argument as ArrayCreateExpression;
					if (ace == null || ace.Initializer.IsNull)
						return false;
					foreach (var element in ace.Initializer.Elements) {
						if (argumentFilter(parameter, element))
							arguments.Add(argument);
					}
				} else if (formatArgument != null && argumentFilter(parameter, argument)) {
					arguments.Add(argument);
				}
			}
			return formatArgument != null;
		}
	}
}

