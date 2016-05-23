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
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Roslyn;

namespace dnSpy.Roslyn.Shared.Text {
	[Export(typeof(IRoslynCodeEditorCreator))]
	sealed class RoslynCodeEditorCreator : IRoslynCodeEditorCreator {
		readonly ICodeEditorCreator codeEditorCreator;

		[ImportingConstructor]
		RoslynCodeEditorCreator(ICodeEditorCreator codeEditorCreator) {
			this.codeEditorCreator = codeEditorCreator;
		}

		public IRoslynCodeEditorUI Create(RoslynCodeEditorOptions options) {
			var helper = new CreateGuidObjectHelper(options.CreateGuidObjects);
			var newOpts = options.ToCodeEditorOptions();
			newOpts.CreateGuidObjects = helper.CreateFunc;
			var rce = new RoslynCodeEditor(options, codeEditorCreator.Create(newOpts));
			helper.RoslynCodeEditor = rce;
			return rce;
		}

		sealed class CreateGuidObjectHelper {
			readonly Func<GuidObjectsCreatorArgs, IEnumerable<GuidObject>> origCreateGuidObjects;

			public RoslynCodeEditor RoslynCodeEditor { get; set; }

			public CreateGuidObjectHelper(Func<GuidObjectsCreatorArgs, IEnumerable<GuidObject>> origCreateGuidObjects) {
				this.origCreateGuidObjects = origCreateGuidObjects;
			}

			public IEnumerable<GuidObject> CreateFunc(GuidObjectsCreatorArgs args) {
				if (origCreateGuidObjects != null) {
					foreach (var go in origCreateGuidObjects(args))
						yield return go;
				}

				Debug.Assert(RoslynCodeEditor != null);
				yield return new GuidObject(MenuConstants.GUIDOBJ_ROSLYN_CODE_EDITOR_GUID, RoslynCodeEditor);
			}
		}
	}
}
