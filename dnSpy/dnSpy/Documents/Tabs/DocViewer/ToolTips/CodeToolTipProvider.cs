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
using System.Windows;
using System.Windows.Controls;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Documents.Tabs.DocViewer.ToolTips {
	sealed class CodeToolTipProvider : ICodeToolTipProvider {
		public ImageReference? Image { get; set; }

		public ICodeToolTipWriter Output => writers[writers.Count - 1];
		readonly List<CodeToolTipWriter> writers;

		readonly IWpfTextView wpfTextView;
		readonly IDotNetImageService dotNetImageService;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly IThemeClassificationTypeService themeClassificationTypeService;
		readonly IClassificationTypeRegistryService classificationTypeRegistryService;
		readonly bool syntaxHighlight;

		public CodeToolTipProvider(IWpfTextView wpfTextView, IDotNetImageService dotNetImageService, IClassificationFormatMap classificationFormatMap, IThemeClassificationTypeService themeClassificationTypeService, IClassificationTypeRegistryService classificationTypeRegistryService, bool syntaxHighlight) {
			this.wpfTextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
			this.dotNetImageService = dotNetImageService ?? throw new ArgumentNullException(nameof(dotNetImageService));
			this.classificationFormatMap = classificationFormatMap ?? throw new ArgumentNullException(nameof(classificationFormatMap));
			this.themeClassificationTypeService = themeClassificationTypeService ?? throw new ArgumentNullException(nameof(themeClassificationTypeService));
			this.classificationTypeRegistryService = classificationTypeRegistryService ?? throw new ArgumentNullException(nameof(classificationTypeRegistryService));
			this.syntaxHighlight = syntaxHighlight;
			writers = new List<CodeToolTipWriter>();
			CreateNewOutput();
		}

		public object Create() {
			var res = new StackPanel {
				Orientation = Orientation.Vertical,
			};
			var sigGrid = new Grid();
			sigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			sigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			res.Children.Add(sigGrid);
			for (int i = 1; i < writers.Count; i++) {
				var output = writers[i];
				if (!output.IsEmpty)
					res.Children.Add(output.Create());
			}
			if (Image != null) {
				var img = new DsImage {
					ImageReference = Image.Value,
					Margin = new Thickness(0, 0, 4, 0),
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalAlignment = HorizontalAlignment.Left,
				};
				Grid.SetColumn(img, 0);
				sigGrid.Children.Add(img);
			}
			var sig = writers[0].Create();
			Grid.SetColumn(sig, 1);
			sigGrid.Children.Add(sig);
			return res;
		}

		public ICodeToolTipWriter CreateNewOutput() {
			writers.Add(new CodeToolTipWriter(classificationFormatMap, themeClassificationTypeService, classificationTypeRegistryService, syntaxHighlight));
			return writers[writers.Count - 1];
		}

		public void SetImage(object @ref) => Image = TryGetImageReference(@ref);

		ImageReference? TryGetImageReference(object @ref) {
			if (@ref is TypeDef td)
				return dotNetImageService.GetImageReference(td);

			if (@ref is MethodDef md)
				return dotNetImageService.GetImageReference(md);

			if (@ref is PropertyDef pd)
				return dotNetImageService.GetImageReference(pd);

			if (@ref is EventDef ed)
				return dotNetImageService.GetImageReference(ed);

			if (@ref is FieldDef fd)
				return dotNetImageService.GetImageReference(fd);

			if (@ref is NamespaceReference)
				return dotNetImageService.GetNamespaceImageReference();

			if (@ref is GenericParam)
				return dotNetImageService.GetImageReferenceGenericParameter();
			if (@ref is Local || @ref is SourceLocal)
				return dotNetImageService.GetImageReferenceLocal();
			if (@ref is Parameter || @ref is SourceParameter)
				return dotNetImageService.GetImageReferenceParameter();
			if (@ref is IType)
				return dotNetImageService.GetImageReferenceType();
			if (@ref is IMethod && ((IMethod)@ref).IsMethod)
				return dotNetImageService.GetImageReferenceMethod();
			if (@ref is IField && ((IField)@ref).IsField)
				return dotNetImageService.GetImageReferenceField();

			return null;
		}
	}
}
