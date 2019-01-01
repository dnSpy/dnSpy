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
using System.ComponentModel.Composition;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.HexGroups;
using dnSpy.Contracts.Hex.Files;

namespace dnSpy.Hex.Commands {
	abstract class HexCommandOperationsFactoryService {
		public abstract HexCommandOperations GetCommandOperations(HexView hexView);
	}

	[Export(typeof(HexCommandOperationsFactoryService))]
	sealed class HexCommandOperationsFactoryServiceImpl : HexCommandOperationsFactoryService {
		readonly IMessageBoxService messageBoxService;
		readonly Lazy<HexEditorGroupFactoryService> hexEditorGroupFactoryService;
		readonly Lazy<HexBufferFileServiceFactory> hexBufferFileServiceFactory;

		[ImportingConstructor]
		HexCommandOperationsFactoryServiceImpl(IMessageBoxService messageBoxService, Lazy<HexEditorGroupFactoryService> hexEditorGroupFactoryService, Lazy<HexBufferFileServiceFactory> hexBufferFileServiceFactory) {
			this.messageBoxService = messageBoxService;
			this.hexEditorGroupFactoryService = hexEditorGroupFactoryService;
			this.hexBufferFileServiceFactory = hexBufferFileServiceFactory;
		}

		public override HexCommandOperations GetCommandOperations(HexView hexView) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			return hexView.Properties.GetOrCreateSingletonProperty(typeof(HexCommandOperations),
				() => new HexCommandOperationsImpl(messageBoxService, hexEditorGroupFactoryService, hexBufferFileServiceFactory, hexView));
		}

		internal static void RemoveFromProperties(HexCommandOperations hexCommandOperations) =>
			hexCommandOperations.HexView.Properties.RemoveProperty(typeof(HexCommandOperations));
	}
}
