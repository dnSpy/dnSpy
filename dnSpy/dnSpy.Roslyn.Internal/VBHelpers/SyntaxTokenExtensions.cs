using System;
using Microsoft.CodeAnalysis;

namespace dnSpy.Roslyn.Internal.VBHelpers {
	static class SyntaxTokenExtensions2 {
		public static T GetAncestor<T>(this SyntaxToken token, Func<T, bool> predicate = null) where T : SyntaxNode => Microsoft.CodeAnalysis.Shared.Extensions.SyntaxTokenExtensions.GetAncestor<T>(token, predicate);
	}
}
