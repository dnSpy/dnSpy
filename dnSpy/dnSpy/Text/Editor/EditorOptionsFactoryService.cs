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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IEditorOptionsFactoryService))]
	sealed class EditorOptionsFactoryService : IEditorOptionsFactoryService {
		IEditorOptions IEditorOptionsFactoryService.GlobalOptions => GlobalOptions;
		internal EditorOptions GlobalOptions { get; }

		internal IEnumerable<EditorOptionDefinition> EditorOptionDefinitions => editorOptionDefinitions.Values;
		readonly Dictionary<string, EditorOptionDefinition> editorOptionDefinitions;

		[ImportingConstructor]
		EditorOptionsFactoryService([ImportMany] IEnumerable<EditorOptionDefinition> editorOptionDefinitions) {
			this.editorOptionDefinitions = new Dictionary<string, EditorOptionDefinition>();
			foreach (var o in editorOptionDefinitions) {
				Debug.Assert(!this.editorOptionDefinitions.ContainsKey(o.Name));
				this.editorOptionDefinitions[o.Name] = o;
			}
			GlobalOptions = new EditorOptions(this, null, null);
			GlobalOptions.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, WordWrapStylesConstants.DefaultValue);
		}

		public IEditorOptions CreateOptions() => new EditorOptions(this, GlobalOptions, null);
		public IEditorOptions GetOptions(IPropertyOwner scope) {
			if (scope == null)
				throw new ArgumentNullException(nameof(scope));
			return scope.Properties.GetOrCreateSingletonProperty(typeof(IEditorOptions), () => new EditorOptions(this, GlobalOptions, scope));
		}

		internal EditorOptionDefinition GetOption(string optionId) => editorOptionDefinitions[optionId];
	}
}
