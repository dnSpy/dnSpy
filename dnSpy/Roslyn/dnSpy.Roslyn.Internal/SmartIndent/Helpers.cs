//TODO: Remove GetOptionsAsync() whenever roslyn gets upgraded to a later version (beta4 perhaps?)

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;

namespace dnSpy.Roslyn.Internal.SmartIndent {
	static class Helpers {
		/// <summary>
		/// Returns the options that should be applied to this document. This consists of global options from <see cref="Solution.Options"/>,
		/// merged with any settings the user has specified at the solution, project, and document levels.
		/// </summary>
		/// <remarks>
		/// This method is async because this may require reading other files. It is expected this is cheap and synchronous in most cases due to caching.</remarks>
		public static Task<DocumentOptionSet> GetOptionsAsync(this Document document, CancellationToken cancellationToken = default(CancellationToken)) {
			// TODO: merge with document-specific options
			return Task.FromResult(new DocumentOptionSet(document.Project.Solution.Options, document.Project.Language));
		}
	}
}
