// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Threading;
using ICSharpCode.NRefactory.CSharp;

namespace Decompiler.Transforms
{
	public static class TransformationPipeline
	{
		public static IAstVisitor<object, object>[] CreatePipeline(DecompilerContext context)
		{
			return new IAstVisitor<object, object>[] {
				new PushNegation(),
				new DelegateConstruction(context),
				new ConvertConstructorCallIntoInitializer(),
				new ReplaceMethodCallsWithOperators(),
			};
		}
		
		public static void RunTransformationsUntil(AstNode node, Predicate<IAstVisitor<object, object>> abortCondition, DecompilerContext context)
		{
			if (node == null)
				return;
			for (int i = 0; i < 4; i++) {
				context.CancellationToken.ThrowIfCancellationRequested();
				if (Options.ReduceAstJumps) {
					node.AcceptVisitor(new Transforms.Ast.RemoveGotos(), null);
					node.AcceptVisitor(new Transforms.Ast.RemoveDeadLabels(), null);
				}
				if (Options.ReduceAstLoops) {
					node.AcceptVisitor(new Transforms.Ast.RestoreLoop(), null);
				}
				if (Options.ReduceAstOther) {
					node.AcceptVisitor(new Transforms.Ast.RemoveEmptyElseBody(), null);
				}
			}
			
			foreach (var visitor in CreatePipeline(context)) {
				context.CancellationToken.ThrowIfCancellationRequested();
				if (abortCondition != null && abortCondition(visitor))
					return;
				node.AcceptVisitor(visitor, null);
			}
		}
	}
}
