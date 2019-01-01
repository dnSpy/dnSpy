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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.ViewHelpers {
	sealed class OpenAssembly : IOpenAssembly {
		readonly IDsDocumentService documentService;

		public OpenAssembly(IDsDocumentService documentService) => this.documentService = documentService;

		public IDsDocument Open() => Open(false).FirstOrDefault();
		public IDsDocument[] OpenMany() => Open(true);

		IDsDocument[] Open(bool many) {
			var dialog = new OpenFileDialog() {
				Filter = PickFilenameConstants.DotNetAssemblyOrModuleFilter,
				RestoreDirectory = true,
				Multiselect = many,
			};
			if (dialog.ShowDialog() != DialogResult.OK)
				return Array.Empty<IDsDocument>();

			var list = new List<IDsDocument>(dialog.FileNames.Length);
			foreach (var filename in dialog.FileNames) {
				var info = DsDocumentInfo.CreateDocument(filename);
				var file = documentService.TryGetOrCreate(info);
				if (file != null)
					list.Add(file);
			}
			return list.ToArray();
		}
	}
}
