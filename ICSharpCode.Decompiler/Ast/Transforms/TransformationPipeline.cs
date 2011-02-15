// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.CSharp;

namespace Decompiler.Transforms
{
	public static class TransformationPipeline
	{
		static IAstVisitor<object, object>[] CreatePipeline()
		{
			return new IAstVisitor<object, object>[] {
				new DelegateConstruction(),
				new ConvertConstructorCallIntoInitializer(),
				new ReplaceMethodCallsWithOperators()
			};
		}
		
		public static void RunTransformations(AstNode node)
		{
			RunTransformationsUntil(node, v => false);
		}
		
		public static void RunTransformationsUntil(AstNode node, Predicate<IAstVisitor<object, object>> abortCondition)
		{
			if (node == null)
				return;
			for (int i = 0; i < 4; i++) {
				if (Options.ReduceAstJumps) {
					node.AcceptVisitor(new Transforms.Ast.RemoveGotos(), null);
					node.AcceptVisitor(new Transforms.Ast.RemoveDeadLabels(), null);
				}
				if (Options.ReduceAstLoops) {
					node.AcceptVisitor(new Transforms.Ast.RestoreLoop(), null);
				}
				if (Options.ReduceAstOther) {
					node.AcceptVisitor(new Transforms.Ast.RemoveEmptyElseBody(), null);
					node.AcceptVisitor(new Transforms.Ast.PushNegation(), null);
				}
			}
			
			foreach (var visitor in CreatePipeline()) {
				if (abortCondition(visitor))
					return;
				node.AcceptVisitor(visitor, null);
			}
		}
	}
}
