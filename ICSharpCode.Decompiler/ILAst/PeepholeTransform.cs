// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.NRefactory.Utils;

namespace Decompiler
{
	public delegate void PeepholeTransform(ILBlock block, ref int i);
	
	/// <summary>
	/// Handles peephole transformations on the ILAst.
	/// </summary>
	public static class PeepholeTransforms
	{
		public static void Run(DecompilerContext context, ILBlock method)
		{
			// TODO: move this somewhere else
			// Eliminate 'dups':
			foreach (ILExpression expr in method.GetSelfAndChildrenRecursive<ILExpression>()) {
				for (int i = 0; i < expr.Arguments.Count; i++) {
					if (expr.Arguments[i].Code == ILCode.Dup)
						expr.Arguments[i] = expr.Arguments[i].Arguments[0];
				}
			}
			
			PeepholeTransform[] transforms = {
				ArrayInitializers.Transform(method)
			};
			// Traverse in post order so that nested blocks are transformed first. This is required so that
			// patterns on the parent block can assume that all nested blocks are already transformed.
			foreach (var block in TreeTraversal.PostOrder<ILNode>(method, c => c != null ? c.GetChildren() : null).OfType<ILBlock>()) {
				// go through the instructions in reverse so that
				for (int i = block.Body.Count - 1; i >= 0; i--) {
					context.CancellationToken.ThrowIfCancellationRequested();
					foreach (var t in transforms) {
						t(block, ref i);
					}
				}
			}
		}
	}
}
