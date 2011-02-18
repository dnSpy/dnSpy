// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Threading;
using ICSharpCode.NRefactory.CSharp;

namespace Decompiler.Transforms
{
	public static class TransformationPipeline
	{
		public static IAstVisitor<object, object>[] CreatePipeline(CancellationToken cancellationToken)
		{
			return new IAstVisitor<object, object>[] {
				new DelegateConstruction() { CancellationToken = cancellationToken },
				new ConvertConstructorCallIntoInitializer(),
				new ReplaceMethodCallsWithOperators()
			};
		}
		
		public static void RunTransformationsUntil(AstNode node, Predicate<IAstVisitor<object, object>> abortCondition, CancellationToken cancellationToken)
		{
			if (node == null)
				return;
			for (int i = 0; i < 4; i++) {
				cancellationToken.ThrowIfCancellationRequested();
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
			
			foreach (var visitor in CreatePipeline(cancellationToken)) {
				cancellationToken.ThrowIfCancellationRequested();
				if (abortCondition != null && abortCondition(visitor))
					return;
				node.AcceptVisitor(visitor, null);
			}
		}
	}
}
