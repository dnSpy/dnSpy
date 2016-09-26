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

namespace dnSpy.Contracts.Documents {
	/// <summary>
	/// Event args
	/// </summary>
	public sealed class NotifyDocumentCollectionChangedEventArgs : EventArgs {
		/// <summary>
		/// Event type
		/// </summary>
		public NotifyDocumentCollectionType Type { get; private set; }

		/// <summary>
		/// All documents
		/// </summary>
		public IDsDocument[] Documents { get; private set; }

		/// <summary>
		/// User data
		/// </summary>
		public object Data { get; private set; }

		NotifyDocumentCollectionChangedEventArgs() {
		}

		/// <summary>
		/// Creates a <see cref="NotifyDocumentCollectionType.Clear"/> instance
		/// </summary>
		/// <param name="clearedDocuments">All cleared documents</param>
		/// <param name="data">Data to send to listeners</param>
		/// <returns></returns>
		public static NotifyDocumentCollectionChangedEventArgs CreateClear(IDsDocument[] clearedDocuments, object data) {
			if (clearedDocuments == null)
				throw new ArgumentNullException(nameof(clearedDocuments));
			var e = new NotifyDocumentCollectionChangedEventArgs();
			e.Type = NotifyDocumentCollectionType.Clear;
			e.Documents = clearedDocuments;
			e.Data = data;
			return e;
		}

		/// <summary>
		/// Creates a <see cref="NotifyDocumentCollectionType.Add"/> instance
		/// </summary>
		/// <param name="document">Added document</param>
		/// <param name="data">Data to send to listeners</param>
		/// <returns></returns>
		public static NotifyDocumentCollectionChangedEventArgs CreateAdd(IDsDocument document, object data) {
			if (document == null)
				throw new ArgumentNullException(nameof(document));
			var e = new NotifyDocumentCollectionChangedEventArgs();
			e.Type = NotifyDocumentCollectionType.Add;
			e.Documents = new IDsDocument[] { document };
			e.Data = data;
			return e;
		}

		/// <summary>
		/// Creates a <see cref="NotifyDocumentCollectionType.Remove"/> instance
		/// </summary>
		/// <param name="document">Removed document</param>
		/// <param name="data">Data to send to listeners</param>
		/// <returns></returns>
		public static NotifyDocumentCollectionChangedEventArgs CreateRemove(IDsDocument document, object data) {
			if (document == null)
				throw new ArgumentNullException(nameof(document));
			return CreateRemove(new[] { document }, data);
		}

		/// <summary>
		/// Creates a <see cref="NotifyDocumentCollectionType.Remove"/> instance
		/// </summary>
		/// <param name="documents">Removed documents</param>
		/// <param name="data">Data to send to listeners</param>
		/// <returns></returns>
		public static NotifyDocumentCollectionChangedEventArgs CreateRemove(IDsDocument[] documents, object data) {
			if (documents == null)
				throw new ArgumentNullException(nameof(documents));
			var e = new NotifyDocumentCollectionChangedEventArgs();
			e.Type = NotifyDocumentCollectionType.Remove;
			e.Documents = documents;
			e.Data = data;
			return e;
		}
	}
}
