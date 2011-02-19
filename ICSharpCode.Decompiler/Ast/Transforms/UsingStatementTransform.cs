// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using Decompiler.Transforms;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.PatternMatching;

namespace Decompiler.Transforms
{
	/// <summary>
	/// Description of UsingStatementTransform.
	/// </summary>
	public class UsingStatementTransform : IAstTransform
	{
		static readonly AstNode usingVarDeclPattern = new VariableDeclarationStatement {
			Type = new AnyNode("type").ToType(),
			Variables = {
				new NamedNode(
					"variable",
					new VariableInitializer {
						Initializer = new AnyNode().ToExpression()
					}
				).ToVariable()
			}
		};
		static readonly AstNode usingTryCatchPattern = new TryCatchStatement {
			TryBlock = new AnyNode("body").ToBlock(),
			FinallyBlock = new BlockStatement {
				Statements = {
					new IfElseStatement {
						Condition = new BinaryOperatorExpression(
							new NamedNode("ident", new IdentifierExpression()).ToExpression(),
							BinaryOperatorType.InEquality,
							new NullReferenceExpression()
						),
						TrueStatement = new BlockStatement {
							Statements = {
								new ExpressionStatement(new Backreference("ident").ToExpression().Invoke("Dispose"))
							}
						}
					}
				}
			}
		};
		
		public void Run(AstNode compilationUnit)
		{
			foreach (AstNode node in compilationUnit.Descendants.ToArray()) {
				Match m1 = usingVarDeclPattern.Match(node);
				if (m1 == null) continue;
				Match m2 = usingTryCatchPattern.Match(node.NextSibling);
				if (m2 == null) continue;
				if (((VariableInitializer)m1["variable"].Single()).Name == ((IdentifierExpression)m2["ident"].Single()).Identifier) {
					BlockStatement body = (BlockStatement)m2["body"].Single();
					body.Remove();
					node.NextSibling.Remove();
					node.ReplaceWith(
						varDecl => new UsingStatement {
							ResourceAcquisition = varDecl,
							EmbeddedStatement = body
						});
				}
			}
		}
	}
}
