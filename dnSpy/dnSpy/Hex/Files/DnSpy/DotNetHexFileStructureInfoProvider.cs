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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DnSpy;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Hex.Text;
using dnSpy.Contracts.Images;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Files.DnSpy {
	[Export(typeof(HexFileStructureInfoProviderFactory))]
	[VSUTIL.Name("dnSpy-DotNet")]
	[VSUTIL.Order(Before = PredefinedHexFileStructureInfoProviderFactoryNames.Default)]
	sealed class DotNetHexFileStructureInfoProviderFactory : HexFileStructureInfoProviderFactory {
		readonly ToolTipCreatorFactory toolTipCreatorFactory;

		[ImportingConstructor]
		DotNetHexFileStructureInfoProviderFactory(ToolTipCreatorFactory toolTipCreatorFactory) {
			this.toolTipCreatorFactory = toolTipCreatorFactory;
		}

		public override HexFileStructureInfoProvider Create(HexView hexView) =>
			new DotNetHexFileStructureInfoProvider(toolTipCreatorFactory);
	}

	sealed class DotNetHexFileStructureInfoProvider : HexFileStructureInfoProvider {
		readonly ToolTipCreatorFactory toolTipCreatorFactory;

		public DotNetHexFileStructureInfoProvider(ToolTipCreatorFactory toolTipCreatorFactory) {
			if (toolTipCreatorFactory == null)
				throw new ArgumentNullException(nameof(toolTipCreatorFactory));
			this.toolTipCreatorFactory = toolTipCreatorFactory;
		}

		public override object GetToolTip(HexBufferFile file, ComplexData structure, HexPosition position) {
			var body = structure as DotNetMethodBody;
			if (body != null)
				return GetToolTip(body, position);

			return base.GetToolTip(file, structure, position);
		}

		object GetToolTip(DotNetMethodBody body, HexPosition position) {
			var toolTipCreator = toolTipCreatorFactory.Create();
			var contentCreator = toolTipCreator.ToolTipContentCreator;

			contentCreator.Image = DsImages.MethodPublic;

			var writer = contentCreator.Writer;
			writer.Write("Method", PredefinedClassifiedTextTags.Text);
			writer.WriteSpace();
			const int maxRids = 10;
			for (int i = 0; i < body.Tokens.Count; i++) {
				if (i > 0) {
					writer.Write(",", PredefinedClassifiedTextTags.Punctuation);
					writer.WriteSpace();
				}
				if (i >= maxRids) {
					writer.Write("...", PredefinedClassifiedTextTags.Error);
					break;
				}
				writer.Write("0x" + body.Tokens[i].ToString("X8"), PredefinedClassifiedTextTags.Number);
			}
			contentCreator.CreateNewWriter();

			contentCreator.Writer.WriteFieldAndValue(body, position);

			return toolTipCreator.Create();
		}

		public override object GetReference(HexBufferFile file, ComplexData structure, HexPosition position) {
			var body = structure as DotNetMethodBody;
			if (body != null)
				return GetReference(file, body, position);
			return base.GetReference(file, structure, position);
		}

		object GetReference(HexBufferFile file, DotNetMethodBody body, HexPosition position) {
			if (body.Instructions.Data.Span.Span.Contains(position))
				return new HexMethodReference(file, body.Tokens[0], (uint)(position - body.Instructions.Data.Span.Span.Start).ToUInt64());
			return new HexMethodReference(file, body.Tokens[0], null);
		}
	}
}
