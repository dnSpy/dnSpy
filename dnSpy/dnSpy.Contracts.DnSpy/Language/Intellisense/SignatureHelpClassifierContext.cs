/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using System;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Signature help classifier context. Use <see cref="SignatureHelpConstants.TryGetSignatureHelpClassifierContext(ITextBuffer)"/>
	/// to get the instance.
	/// </summary>
	public class SignatureHelpClassifierContext {
		/// <summary>
		/// Gets the type, eg. <see cref="SignatureHelpClassifierContextTypes.ParameterDocumentation"/>
		/// </summary>
		public string Type { get; }

		/// <summary>
		/// Gets the signature help session
		/// </summary>
		public ISignatureHelpSession Session { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Context type, eg. <see cref="SignatureHelpClassifierContextTypes.ParameterDocumentation"/></param>
		/// <param name="session">Signature help session</param>
		protected internal SignatureHelpClassifierContext(string type, ISignatureHelpSession session) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Session = session ?? throw new ArgumentNullException(nameof(session));
		}
	}

	/// <summary>
	/// Signature documentation signature help classifier context
	/// </summary>
	public sealed class SignatureDocumentationSignatureHelpClassifierContext : SignatureHelpClassifierContext {
		/// <summary>
		/// Gets the signature to classify
		/// </summary>
		public ISignature Signature { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="session">Signature help session</param>
		/// <param name="signature">Signature to classify</param>
		public SignatureDocumentationSignatureHelpClassifierContext(ISignatureHelpSession session, ISignature signature)
			: base(SignatureHelpClassifierContextTypes.SignatureDocumentation, session) => Signature = signature ?? throw new ArgumentNullException(nameof(signature));
	}

	/// <summary>
	/// Parameter name signature help classifier context
	/// </summary>
	public sealed class ParameterNameSignatureHelpClassifierContext : SignatureHelpClassifierContext {
		/// <summary>
		/// Gets the parameter to classify
		/// </summary>
		public IParameter Parameter { get; }

		/// <summary>
		/// Gets the offset of <see cref="IParameter.Name"/> in the text buffer.
		/// </summary>
		public int NameOffset { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="session">Signature help session</param>
		/// <param name="parameter">Parameter to classify</param>
		/// <param name="nameOffset">Offset of <see cref="IParameter.Name"/> in the text buffer</param>
		public ParameterNameSignatureHelpClassifierContext(ISignatureHelpSession session, IParameter parameter, int nameOffset)
			: base(SignatureHelpClassifierContextTypes.ParameterName, session) {
			if (nameOffset < 0)
				throw new ArgumentOutOfRangeException(nameof(nameOffset));
			Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
			NameOffset = nameOffset;
		}
	}

	/// <summary>
	/// Parameter documentation signature help classifier context
	/// </summary>
	public sealed class ParameterDocumentationSignatureHelpClassifierContext : SignatureHelpClassifierContext {
		/// <summary>
		/// Gets the parameter to classify
		/// </summary>
		public IParameter Parameter { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="session">Signature help session</param>
		/// <param name="parameter">Parameter to classify</param>
		public ParameterDocumentationSignatureHelpClassifierContext(ISignatureHelpSession session, IParameter parameter)
			: base(SignatureHelpClassifierContextTypes.ParameterDocumentation, session) => Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
	}

	/// <summary>
	/// Signature help context types (see <see cref="SignatureHelpClassifierContext.Type"/>)
	/// </summary>
	public static class SignatureHelpClassifierContextTypes {
		/// <summary>
		/// Signature documentation
		/// </summary>
		public static readonly string SignatureDocumentation = nameof(SignatureDocumentation);

		/// <summary>
		/// Parameter name
		/// </summary>
		public static readonly string ParameterName = nameof(ParameterName);

		/// <summary>
		/// Parameter documentation
		/// </summary>
		public static readonly string ParameterDocumentation = nameof(ParameterDocumentation);
	}
}
