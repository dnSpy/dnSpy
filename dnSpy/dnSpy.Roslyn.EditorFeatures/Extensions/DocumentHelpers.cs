using System.Threading;
using Microsoft.CodeAnalysis;

namespace dnSpy.Roslyn.EditorFeatures.Extensions {
	static class DocumentHelpers {
		public static SyntaxTree GetSyntaxTreeSynchronously(Document document, CancellationToken cancellationToken) {
			//TODO: Roslyn 2.0: use document.GetSyntaxTreeSynchronously()
			SyntaxTree syntaxTree;
			if (document.TryGetSyntaxTree(out syntaxTree))
				return syntaxTree;
			return document.GetSyntaxTreeAsync(cancellationToken).GetAwaiter().GetResult();
		}
	}
}
