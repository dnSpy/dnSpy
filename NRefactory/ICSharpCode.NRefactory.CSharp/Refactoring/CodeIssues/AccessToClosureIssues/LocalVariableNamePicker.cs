// 
// NamePicker.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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

using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public static class LocalVariableNamePicker
	{
		public static string PickSafeName (AstNode node, IEnumerable<string> candidates)
		{
			var existingNames = new VariableNameCollector ().Collect (node);
			return candidates.FirstOrDefault (name => !existingNames.Contains (name));
		}

		class VariableNameCollector : DepthFirstAstVisitor
		{
			private ISet<string> variableNames = new HashSet<string> ();

			public ISet<string> Collect (AstNode node)
			{
				variableNames.Clear ();
				node.AcceptVisitor (this);
				return variableNames;
			}

			public override void VisitParameterDeclaration (ParameterDeclaration parameterDeclaration)
			{
				variableNames.Add (parameterDeclaration.Name);
				base.VisitParameterDeclaration (parameterDeclaration);
			}

			public override void VisitVariableInitializer (VariableInitializer variableInitializer)
			{
				variableNames.Add (variableInitializer.Name);
				base.VisitVariableInitializer (variableInitializer);
			}

			public override void VisitForeachStatement (ForeachStatement foreachStatement)
			{
				variableNames.Add (foreachStatement.VariableName);
				base.VisitForeachStatement (foreachStatement);
			}
		}
	}
}
