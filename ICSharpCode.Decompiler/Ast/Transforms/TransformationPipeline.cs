// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Threading;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	public interface IAstTransform
	{
		void Run(AstNode node);
	}
	
	public static class TransformationPipeline
	{
		public static IAstTransform[] CreatePipeline(DecompilerContext context)
		{
			return new IAstTransform[] {
				new PushNegation(),
				new DelegateConstruction(context),
				new PatternStatementTransform(context),
				new ReplaceMethodCallsWithOperators(),
				new IntroduceUnsafeModifier(),
				new DeclareVariables(context), // should run after most transforms that modify statements
				new ConvertConstructorCallIntoInitializer(), // must run after DeclareVariables
				new IntroduceUsingDeclarations(context)
			};
		}
		
		public static void RunTransformationsUntil(AstNode node, Predicate<IAstTransform> abortCondition, DecompilerContext context)
		{
			if (node == null)
				return;
			
			foreach (var transform in CreatePipeline(context)) {
				context.CancellationToken.ThrowIfCancellationRequested();
				if (abortCondition != null && abortCondition(transform))
					return;
				transform.Run(node);
			}
		}
	}
}
