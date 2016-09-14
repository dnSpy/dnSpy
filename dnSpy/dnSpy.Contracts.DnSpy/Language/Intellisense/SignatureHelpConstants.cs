/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Signature help constants
	/// </summary>
	static class SignatureHelpConstants {
		/// <summary>
		/// Suffix added to the current signature's content type name (<see cref="IContentType.TypeName"/>)
		/// to get the name of the content type for the signature help text.
		/// This content type is created if it doesn't exist.
		/// </summary>
		public const string SignatureHelpContentTypeSuffix = " Signature Help";

		/// <summary>
		/// <see cref="ITextBuffer"/> property key of a <see cref="bool"/> that indicates whether
		/// the pretty printed content should be used (<see cref="ISignature.PrettyPrintedContent"/>
		/// vs <see cref="ISignature.Content"/> and <see cref="IParameter.Locus"/> vs
		/// <see cref="IParameter.PrettyPrintedLocus"/>)
		/// </summary>
		public const string UsePrettyPrintedContentBufferKey = "UsePrettyPrintedContent";

		/// <summary>
		/// <see cref="ITextBuffer"/> property key to get the <see cref="ISignatureHelpSession"/> instance.
		/// It's used by the signature help classifiers to get the session to classify.
		/// </summary>
		public static readonly object SessionBufferKey = typeof(ISignatureHelpSession);

		/// <summary>
		/// Returns the signature help session or null if <paramref name="buffer"/> is not a signature help buffer
		/// </summary>
		/// <param name="buffer">Text buffer</param>
		/// <returns></returns>
		public static ISignatureHelpSession TryGetSignatureHelpSession(this ITextBuffer buffer) {
			ISignatureHelpSession session;
			if (buffer.Properties.TryGetProperty(SessionBufferKey, out session))
				return session;
			return null;
		}

		/// <summary>
		/// Gets the <c>UsePrettyPrintedContent</c> value
		/// </summary>
		/// <param name="buffer">Signature help text buffer</param>
		/// <returns></returns>
		public static bool GetUsePrettyPrintedContent(this ITextBuffer buffer) {
			bool usePrettyPrintedContent;
			if (buffer.Properties.TryGetProperty(UsePrettyPrintedContentBufferKey, out usePrettyPrintedContent))
				return usePrettyPrintedContent;
			Debug.Fail(nameof(UsePrettyPrintedContentBufferKey) + " hasn't been initialized yet");
			return false;
		}
	}
}
