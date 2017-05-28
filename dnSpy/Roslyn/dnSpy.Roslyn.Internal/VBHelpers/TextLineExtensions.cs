using Microsoft.CodeAnalysis.Text;

namespace dnSpy.Roslyn.Internal.VBHelpers {
	static class TextLineExtensions2 {
		public static int? GetLastNonWhitespacePosition(this TextLine line) => Microsoft.CodeAnalysis.Shared.Extensions.TextLineExtensions.GetLastNonWhitespacePosition(line);
		public static int? GetFirstNonWhitespacePosition(this TextLine line) => Microsoft.CodeAnalysis.Shared.Extensions.TextLineExtensions.GetFirstNonWhitespacePosition(line);
	}
}
