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

using System;
using System.Diagnostics;

namespace dnSpy.Contracts.Documents {
	/// <summary>
	/// Document info
	/// </summary>
	public struct DsDocumentInfo {
		/// <summary>
		/// Name, eg. filename if <see cref="Type"/> is <see cref="DocumentConstants.DOCUMENTTYPE_FILE"/>
		/// </summary>
		public string Name => name ?? string.Empty;
		readonly string name;

		/// <summary>
		/// Document type, eg. <see cref="DocumentConstants.DOCUMENTTYPE_FILE"/>
		/// </summary>
		public Guid Type { get; }

		/// <summary>
		/// Creates a <see cref="DsDocumentInfo"/> used by files on disk
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <returns></returns>
		public static DsDocumentInfo CreateDocument(string filename) => new DsDocumentInfo(filename, DocumentConstants.DOCUMENTTYPE_FILE);

		/// <summary>
		/// Creates a <see cref="DsDocumentInfo"/> used by files in the GAC
		/// </summary>
		/// <param name="asmFullName">Full name of assembly</param>
		/// <returns></returns>
		public static DsDocumentInfo CreateGacDocument(string asmFullName) => new DsDocumentInfo(asmFullName, DocumentConstants.DOCUMENTTYPE_GAC);

		/// <summary>
		/// Creates a <see cref="DsDocumentInfo"/> used by reference assemblies
		/// </summary>
		/// <param name="asmFullName">Full name of assembly</param>
		/// <param name="refFilePath">Path to the reference assembly. It's used if it's not found
		/// in the GAC.</param>
		/// <returns></returns>
		public static DsDocumentInfo CreateReferenceAssembly(string asmFullName, string refFilePath) {
			Debug.Assert(!refFilePath.Contains(DocumentConstants.REFERENCE_ASSEMBLY_SEPARATOR));
			return new DsDocumentInfo(asmFullName + DocumentConstants.REFERENCE_ASSEMBLY_SEPARATOR + refFilePath, DocumentConstants.DOCUMENTTYPE_REFASM);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name, see <see cref="Name"/></param>
		/// <param name="type">Type, see <see cref="Type"/></param>
		public DsDocumentInfo(string name, Guid type) {
			this.name = name ?? string.Empty;
			Type = type;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => string.Format("{0} {1}", Name, Type);
	}
}
