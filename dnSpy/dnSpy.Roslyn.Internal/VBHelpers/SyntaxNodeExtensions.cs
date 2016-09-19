using Microsoft.CodeAnalysis;

namespace dnSpy.Roslyn.Internal.VBHelpers {
	static class SyntaxNodeExtensions2 {
		public static TNode GetAncestorOrThis<TNode>(this SyntaxNode node) where TNode : SyntaxNode => Microsoft.CodeAnalysis.Shared.Extensions.SyntaxNodeExtensions.GetAncestorOrThis<TNode>(node);
	}
}
