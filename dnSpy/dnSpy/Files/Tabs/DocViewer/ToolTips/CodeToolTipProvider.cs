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
using System.Windows;
using System.Windows.Controls;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Files.Tabs.DocViewer.ToolTips {
	sealed class CodeToolTipProvider : ICodeToolTipProvider {
		public ImageReference? Image { get; set; }

		public ICodeToolTipWriter Output => writers[writers.Count - 1];
		readonly List<CodeToolTipWriter> writers;

		readonly IImageManager imageManager;
		readonly IDotNetImageManager dotNetImageManager;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly IThemeClassificationTypeService themeClassificationTypeService;
		readonly bool syntaxHighlight;

		public CodeToolTipProvider(IImageManager imageManager, IDotNetImageManager dotNetImageManager, IClassificationFormatMap classificationFormatMap, IThemeClassificationTypeService themeClassificationTypeService, bool syntaxHighlight) {
			if (imageManager == null)
				throw new ArgumentNullException(nameof(imageManager));
			if (dotNetImageManager == null)
				throw new ArgumentNullException(nameof(dotNetImageManager));
			if (classificationFormatMap == null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			if (themeClassificationTypeService == null)
				throw new ArgumentNullException(nameof(themeClassificationTypeService));
			this.imageManager = imageManager;
			this.dotNetImageManager = dotNetImageManager;
			this.classificationFormatMap = classificationFormatMap;
			this.themeClassificationTypeService = themeClassificationTypeService;
			this.syntaxHighlight = syntaxHighlight;
			this.writers = new List<CodeToolTipWriter>();
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
				var img = new Image {
					Width = 16,
					Height = 16,
					Source = imageManager.GetImage(Image.Value, BackgroundType.QuickInfo),
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
			writers.Add(new CodeToolTipWriter(classificationFormatMap, themeClassificationTypeService, syntaxHighlight));
			return writers[writers.Count - 1];
		}

		public void SetImage(object @ref) => Image = TryGetImageReference(@ref);

		ImageReference? TryGetImageReference(object @ref) {
			var td = @ref as TypeDef;
			if (td != null)
				return dotNetImageManager.GetImageReference(td);

			var md = @ref as MethodDef;
			if (md != null)
				return dotNetImageManager.GetImageReference(md);

			var pd = @ref as PropertyDef;
			if (pd != null)
				return dotNetImageManager.GetImageReference(pd);

			var ed = @ref as EventDef;
			if (ed != null)
				return dotNetImageManager.GetImageReference(ed);

			var fd = @ref as FieldDef;
			if (fd != null)
				return dotNetImageManager.GetImageReference(fd);

			if (@ref is NamespaceReference)
				return dotNetImageManager.GetNamespaceImageReference();

			if (@ref is GenericParam)
				return dotNetImageManager.GetImageReferenceGenericParameter();
			if (@ref is Local)
				return dotNetImageManager.GetImageReferenceLocal();
			if (@ref is Parameter)
				return dotNetImageManager.GetImageReferenceParameter();
			if (@ref is IType)
				return dotNetImageManager.GetImageReferenceType();
			if (@ref is IMethod && ((IMethod)@ref).IsMethod)
				return dotNetImageManager.GetImageReferenceMethod();
			if (@ref is IField && ((IField)@ref).IsField)
				return dotNetImageManager.GetImageReferenceField();

			return null;
		}
	}
}
