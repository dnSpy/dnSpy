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

namespace dnSpy.Contracts.Documents {
	/// <summary>
	/// Creates <see cref="IDsDocument"/>s
	/// </summary>
	public interface IDsDocumentProvider {
		/// <summary>
		/// Order of this instance
		/// </summary>
		double Order { get; }

		/// <summary>
		/// Creates a new <see cref="IDsDocument"/> instance or returns null. This method can be
		/// called in <c>any</c> thread so the code must be thread safe.
		/// </summary>
		/// <param name="documentService">Document manager</param>
		/// <param name="documentInfo">Document to create</param>
		/// <returns></returns>
		IDsDocument Create(IDsDocumentService documentService, DsDocumentInfo documentInfo);

		/// <summary>
		/// Creates a <see cref="IDsDocumentNameKey"/> instance
		/// </summary>
		/// <param name="documentService">Document manager</param>
		/// <param name="documentInfo">Document to create</param>
		/// <returns></returns>
		IDsDocumentNameKey CreateKey(IDsDocumentService documentService, DsDocumentInfo documentInfo);
	}
}
