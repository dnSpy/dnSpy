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

using dnSpy.Contracts.Files.Tabs.TextEditor.ToolTips;
using dnSpy.Contracts.Text;

namespace dnSpy.BamlDecompiler {
	[ExportToolTipContentCreator(Order = 0)]
	sealed class BamlToolTipContentCreator : IToolTipContentCreator {
		public object Create(IToolTipContentCreatorContext context, object @ref) {
			var bref = @ref as BamlToolTipReference;
			if (bref != null) {
				var creator = context.Create();
				creator.Output.Write(BoxedOutputColor.Text, bref.String);
				return creator.Create();
			}

			return null;
		}
	}

	// Don't use a string since it should only show tooltips if it's from the baml disassembler
	sealed class BamlToolTipReference {
		public static object Create(string s) => string.IsNullOrEmpty(s) ? null : new BamlToolTipReference(s);
		public string String { get; }

		BamlToolTipReference(string s) {
			this.String = s;
		}
	}
}
