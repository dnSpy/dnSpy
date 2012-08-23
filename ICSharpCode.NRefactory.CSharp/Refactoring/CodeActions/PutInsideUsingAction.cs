// 
// PutInsideUsingAction.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
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
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction ("put inside 'using'", Description = "put IDisposable inside 'using' construct")]
	public class PutInsideUsingAction : SpecializedCodeAction <VariableInitializer>
	{
		static readonly FindReferences refFinder = new FindReferences ();
		protected override CodeAction GetAction (RefactoringContext context, VariableInitializer node)
		{
			if (node.Initializer.IsNull)
				return null;

			var variableDecl = node.Parent as VariableDeclarationStatement;
			if (variableDecl == null || !(variableDecl.Parent is BlockStatement))
				return null;

			var type = context.ResolveType (variableDecl.Type);
			if (!IsIDisposable (type))
				return null;

			var unit = context.RootNode as SyntaxTree;
			if (unit == null)
				return null;

			var resolveResult = (LocalResolveResult)context.Resolve (node);

			return new CodeAction (context.TranslateString ("put inside 'using'"),
				script =>
				{
					var lastReference = GetLastReference (resolveResult.Variable, context, unit);

					var body = new BlockStatement ();
					var variableToMoveOutside = new List<VariableDeclarationStatement> ();

					if (lastReference != node) {
						var statements = CollectStatements (variableDecl.NextSibling as Statement, 
															lastReference.EndLocation).ToArray();

						// collect statements to put inside 'using' and variable declaration to move outside 'using'
						foreach (var statement in statements) {
							script.Remove (statement);

							var decl = statement as VariableDeclarationStatement;
							if (decl == null) {
								body.Statements.Add (statement.Clone ());
								continue;
							}

							var outsideDecl = (VariableDeclarationStatement)decl.Clone ();
							outsideDecl.Variables.Clear ();
							var insideDecl = (VariableDeclarationStatement)outsideDecl.Clone ();

							foreach (var variable in decl.Variables) {
								var reference = GetLastReference (
									((LocalResolveResult)context.Resolve (variable)).Variable, context, unit);
								if (reference.StartLocation > lastReference.EndLocation)
									outsideDecl.Variables.Add ((VariableInitializer)variable.Clone ());
								else
									insideDecl.Variables.Add ((VariableInitializer)variable.Clone ());
							}
							if (outsideDecl.Variables.Count > 0)
								variableToMoveOutside.Add (outsideDecl);
							if (insideDecl.Variables.Count > 0)
								body.Statements.Add (insideDecl);
						}
					}

					foreach (var decl in variableToMoveOutside)
						script.InsertBefore (variableDecl, decl);

					var usingStatement = new UsingStatement
					{
						ResourceAcquisition = new VariableDeclarationStatement (variableDecl.Type.Clone (), node.Name,
																				node.Initializer.Clone ()),
						EmbeddedStatement = body
					};
					script.Replace (variableDecl, usingStatement);

					if (variableDecl.Variables.Count == 1)
						return;
					// other variables in the same declaration statement
					var remainingVariables = (VariableDeclarationStatement)variableDecl.Clone ();
					remainingVariables.Variables.Remove (
						remainingVariables.Variables.FirstOrDefault (v => v.Name == node.Name));
					script.InsertBefore (usingStatement, remainingVariables);
				});
		}

		static bool IsIDisposable (IType type)
		{
			return type.GetAllBaseTypeDefinitions ().Any (t => t.KnownTypeCode == KnownTypeCode.IDisposable);
		}
	
		static IEnumerable<Statement> CollectStatements (Statement statement, TextLocation end)
		{
			while (statement != null) {
				yield return statement;
				if (statement.Contains (end))
					break;
				statement = statement.NextSibling as Statement;
			}
		}

		static AstNode GetLastReference (IVariable variable, RefactoringContext context, SyntaxTree unit)
		{
			AstNode lastReference = null;
			refFinder.FindLocalReferences (variable, context.UnresolvedFile, unit, context.Compilation,
				(v, r) =>
				{
					if (lastReference == null || v.EndLocation > lastReference.EndLocation)
						lastReference = v;
				}, context.CancellationToken);
			return lastReference;
		}
	}
}
