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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Files.Tabs.TextEditor.ToolTips;
using dnSpy.Contracts.Images;

namespace dnSpy.Files.Tabs.TextEditor.ToolTips {
	sealed class CodeToolTipCreator : ICodeToolTipCreator {
		public ImageReference? Image { get; set; }

		public ICodeToolTipWriter Output => writers[writers.Count - 1];
		readonly List<CodeToolTipWriter> writers;

		readonly IImageManager imageManager;
		readonly IDotNetImageManager dotNetImageManager;
		readonly bool syntaxHighlight;

		public CodeToolTipCreator(IImageManager imageManager, IDotNetImageManager dotNetImageManager, bool syntaxHighlight) {
			this.imageManager = imageManager;
			this.dotNetImageManager = dotNetImageManager;
			this.syntaxHighlight = syntaxHighlight;
			this.writers = new List<CodeToolTipWriter>();
			CreateNewOutput();
		}

		public object Create() {
			var res = new StackPanel {
				Orientation = Orientation.Vertical,
			};
			var sp = new StackPanel {
				Orientation = Orientation.Horizontal,
			};
			res.Children.Add(sp);
			for (int i = 1; i < writers.Count; i++) {
				var output = writers[i];
				if (!output.IsEmpty)
					res.Children.Add(output.Create());
			}
			if (Image != null) {
				sp.Children.Add(new Image {
					Width = 16,
					Height = 16,
					Source = imageManager.GetImage(Image.Value.Assembly, Image.Value.Name, BackgroundType.CodeToolTip),
					Margin = new Thickness(0, 0, 4, 0),
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalAlignment = HorizontalAlignment.Left,
				});
			}
			sp.Children.Add(writers[0].Create());
			return res;
		}

		public ICodeToolTipWriter CreateNewOutput() {
			writers.Add(new CodeToolTipWriter(syntaxHighlight));
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
