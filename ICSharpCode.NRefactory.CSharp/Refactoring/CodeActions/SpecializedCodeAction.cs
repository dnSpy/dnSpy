using System;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// A specialized code action creates a code action assoziated with one special type of ast nodes.
	/// </summary>
	public abstract class SpecializedCodeAction<T> : ICodeActionProvider where T : AstNode
	{
		/// <summary>
		/// Gets the action for the specified ast node.
		/// </summary>
		/// <returns>
		/// The code action. May return <c>null</c>, if no action can be provided.
		/// </returns>
		/// <param name='context'>
		/// The refactoring conext.
		/// </param>
		/// <param name='node'>
		/// The AstNode it's ensured that the node is always != null, if called.
		/// </param>
		protected abstract CodeAction GetAction(RefactoringContext context, T node);

		#region ICodeActionProvider implementation
		public System.Collections.Generic.IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var node = context.GetNode<T>();
			if (node == null)
				yield break;
			var action = GetAction(context, node);
			if (action == null)
				yield break;
			yield return action;
		}
		#endregion
	}
}

