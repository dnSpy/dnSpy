/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dnSpy.AsmEditor {
	sealed class HexDocumentManager {
		public static readonly HexDocumentManager Instance = new HexDocumentManager();

		object lockObj = new object();
		Dictionary<string, AsmEdHexDocument> filenameToDoc = new Dictionary<string, AsmEdHexDocument>(StringComparer.OrdinalIgnoreCase);

		HexDocumentManager() {
		}

		public bool Exists(string filename) {
			return TryGet(filename) != null;
		}

		public AsmEdHexDocument TryGet(string filename) {
			filename = GetFullPath(filename);

			lock (lockObj) {
				AsmEdHexDocument doc;
				filenameToDoc.TryGetValue(filename, out doc);
				return doc;
			}
		}

		public AsmEdHexDocument GetOrCreate(string filename) {
			filename = GetFullPath(filename);

			lock (lockObj) {
				AsmEdHexDocument doc;
				if (filenameToDoc.TryGetValue(filename, out doc))
					return doc;

				//TODO: This reads the whole file into memory
				doc = new AsmEdHexDocument(filename);
				filenameToDoc.Add(filename, doc);
				return doc;
			}
		}

		public AsmEdHexDocument[] GetDocuments() {
			lock (lockObj)
				return filenameToDoc.Values.ToArray();
		}

		static string GetFullPath(string filename) {
			if (filename == null)
				return string.Empty;
			try {
				return Path.GetFullPath(filename);
			}
			catch (PathTooLongException) {
			}
			return filename;
		}
	}
}
