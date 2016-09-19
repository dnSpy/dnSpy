using System.Threading;
using Microsoft.CodeAnalysis;

namespace dnSpy.Roslyn.Internal.VBHelpers {
	static class SyntaxTreeExtensions2 {
		public static SyntaxToken FindTokenOnLeftOfPosition(
					this SyntaxTree syntaxTree,
					int position,
					CancellationToken cancellationToken,
					bool includeSkipped = true,
					bool includeDirectives = false,
					bool includeDocumentationComments = false) =>
			Microsoft.CodeAnalysis.Shared.Extensions.SyntaxTreeExtensions.FindTokenOnLeftOfPosition(
					syntaxTree,
					position,
					cancellationToken,
					includeSkipped,
					includeDirectives,
					includeDocumentationComments);
	}
}
