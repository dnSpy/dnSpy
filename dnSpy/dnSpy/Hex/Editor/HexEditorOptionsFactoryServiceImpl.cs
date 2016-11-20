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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Hex.Editor;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(HexEditorOptionsFactoryService))]
	sealed class HexEditorOptionsFactoryServiceImpl : HexEditorOptionsFactoryService {
		public override VSTE.IEditorOptions GlobalOptions => HexGlobalOptions;
		internal HexEditorOptions HexGlobalOptions { get; }

		internal IEnumerable<VSTE.EditorOptionDefinition> EditorOptionDefinitions => editorOptionDefinitions.Values;
		readonly Dictionary<string, HexEditorOptionDefinition> editorOptionDefinitions;

		[ImportingConstructor]
		HexEditorOptionsFactoryServiceImpl([ImportMany] IEnumerable<HexEditorOptionDefinition> editorOptionDefinitions) {
			this.editorOptionDefinitions = new Dictionary<string, HexEditorOptionDefinition>();
			foreach (var o in editorOptionDefinitions) {
				Debug.Assert(!this.editorOptionDefinitions.ContainsKey(o.Name));
				this.editorOptionDefinitions[o.Name] = o;
			}
			HexGlobalOptions = new HexEditorOptions(this, null, null);
		}

		public override VSTE.IEditorOptions CreateOptions() => new HexEditorOptions(this, HexGlobalOptions, null);

		public override VSTE.IEditorOptions GetOptions(VSUTIL.IPropertyOwner scope) {
			if (scope == null)
				throw new ArgumentNullException(nameof(scope));
			return scope.Properties.GetOrCreateSingletonProperty(typeof(VSTE.IEditorOptions), () => new HexEditorOptions(this, HexGlobalOptions, scope));
		}

		internal VSTE.EditorOptionDefinition GetOption(string optionId) => editorOptionDefinitions[optionId];
	}
}
