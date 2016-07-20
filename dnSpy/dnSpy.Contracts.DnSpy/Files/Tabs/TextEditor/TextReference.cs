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

using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Files.Tabs.TextEditor {
	/// <summary>
	/// A reference in the text
	/// </summary>
	public sealed class TextReference {
		/// <summary>
		/// Gets the reference or null
		/// </summary>
		public object Reference { get; }

		/// <summary>
		/// true if it's a local, parameter, or label
		/// </summary>
		public bool IsLocal { get; }

		/// <summary>
		/// true if it's a definition
		/// </summary>
		public bool IsDefinition { get; }

		/// <summary>
		/// Gets the span or null if it's unknown
		/// </summary>
		public Span? Span { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reference">Reference or null</param>
		public TextReference(object reference)
			: this(reference, false, false) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reference">Reference or null</param>
		/// <param name="span">Span</param>
		public TextReference(object reference, Span span)
			: this(reference, false, false, span) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reference">Reference or null</param>
		/// <param name="isLocal">true if it's a local, parameter, or label</param>
		/// <param name="isDefinition">true if it's a definition</param>
		public TextReference(object reference, bool isLocal, bool isDefinition) {
			this.Reference = reference;
			this.IsLocal = isLocal;
			this.IsDefinition = isDefinition;
			this.Span = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reference">Reference or null</param>
		/// <param name="isLocal">true if it's a local, parameter, or label</param>
		/// <param name="isDefinition">true if it's a definition</param>
		/// <param name="span">Span</param>
		public TextReference(object reference, bool isLocal, bool isDefinition, Span span) {
			this.Reference = reference;
			this.IsLocal = isLocal;
			this.IsDefinition = isDefinition;
			this.Span = span;
		}
	}
}
