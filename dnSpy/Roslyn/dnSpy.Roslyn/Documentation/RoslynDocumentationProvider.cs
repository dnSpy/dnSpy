/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Globalization;
using System.Threading;
using dnSpy.Contracts.Decompiler.XmlDoc;
using Microsoft.CodeAnalysis;

namespace dnSpy.Roslyn.Documentation {
	sealed class RoslynDocumentationProvider : DocumentationProvider {
		static readonly StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
		Contracts.Decompiler.XmlDoc.XmlDocumentationProvider? xmlDocumentationProvider;
		bool hasLoaded;
		readonly string filename;

		public RoslynDocumentationProvider(string filename) => this.filename = filename;

		protected override string? GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken) {
			if (!hasLoaded) {
				lock (this) {
					if (!hasLoaded) {
						try {
							xmlDocumentationProvider = XmlDocLoader.LoadDocumentation(this, filename);
						}
						catch (ArgumentException) {
						}
					}
					hasLoaded = true;
				}
			}
			return xmlDocumentationProvider?.GetDocumentation(documentationMemberID);
		}

		public override int GetHashCode() => stringComparer.GetHashCode(filename);

		public override bool Equals(object? obj) {
			var other = obj as RoslynDocumentationProvider;
			return !(other is null) && stringComparer.Equals(filename, other.filename);
		}
	}
}
