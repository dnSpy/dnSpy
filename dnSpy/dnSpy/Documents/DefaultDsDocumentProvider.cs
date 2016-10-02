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

using System.ComponentModel.Composition;
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Documents {
	[Export(typeof(IDsDocumentProvider))]
	sealed class DefaultDsDocumentProvider : IDsDocumentProvider {
		public double Order => DocumentConstants.ORDER_DEFAULT_DOCUMENT_PROVIDER;

		public IDsDocument Create(IDsDocumentService documentService, DsDocumentInfo documentInfo) {
			var filename = GetFilename(documentInfo);
			if (filename != null)
				return documentService.CreateDocument(documentInfo, filename);
			return null;
		}

		public IDsDocumentNameKey CreateKey(IDsDocumentService documentService, DsDocumentInfo documentInfo) {
			var filename = GetFilename(documentInfo);
			if (filename != null)
				return new FilenameKey(filename);
			return null;
		}

		static string GetFilename(DsDocumentInfo documentInfo) {
			if (documentInfo.Type == DocumentConstants.DOCUMENTTYPE_FILE)
				return documentInfo.Name;
			if (documentInfo.Type == DocumentConstants.DOCUMENTTYPE_GAC)
				return GetGacFilename(documentInfo.Name);
			if (documentInfo.Type == DocumentConstants.DOCUMENTTYPE_REFASM)
				return GetRefFileFilename(documentInfo.Name);
			return null;
		}

		static string GetGacFilename(string asmFullName) => GacInfo.FindInGac(new AssemblyNameInfo(asmFullName));

		static string GetRefFileFilename(string s) {
			int index = s.LastIndexOf(DocumentConstants.REFERENCE_ASSEMBLY_SEPARATOR);
			Debug.Assert(index >= 0);
			if (index < 0)
				return null;

			var f = GetGacFilename(s.Substring(0, index));
			if (f != null)
				return f;
			return s.Substring(index + DocumentConstants.REFERENCE_ASSEMBLY_SEPARATOR.Length).Trim();
		}
	}
}
