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
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Hex.Classification.DnSpy;
using dnSpy.Contracts.Hex.Files.DnSpy;
using dnSpy.Contracts.Hex.Files.ToolTips;
using dnSpy.Contracts.Hex.Text;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;

namespace dnSpy.Hex.Files.DnSpy {
	[Export(typeof(ToolTipObjectFactory))]
	sealed class ToolTipObjectFactoryImpl : ToolTipObjectFactory {
		readonly HexTextElementCreatorProvider hexTextElementCreatorProvider;

		[ImportingConstructor]
		ToolTipObjectFactoryImpl(HexTextElementCreatorProvider hexTextElementCreatorProvider) => this.hexTextElementCreatorProvider = hexTextElementCreatorProvider;

		public override object? Create(HexToolTipContent content) {
			if (content is null)
				throw new ArgumentNullException(nameof(content));

			var res = new StackPanel {
				Orientation = Orientation.Vertical,
			};
			var sigGrid = new Grid();
			sigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			sigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			res.Children.Add(sigGrid);
			for (int i = 1; i < content.Text.Length; i++) {
				var text = content.Text[i];
				if (text.Count == 0)
					continue;
				res.Children.Add(CreateTextElement(text));
			}
			var imgRef = content.Image as ImageReference?;
			if (!(imgRef is null)) {
				var img = new DsImage {
					ImageReference = imgRef.Value,
					Margin = new Thickness(0, 0, 4, 0),
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalAlignment = HorizontalAlignment.Left,
				};
				Grid.SetColumn(img, 0);
				sigGrid.Children.Add(img);
			}
			if (content.Text.Length > 0) {
				var sig = CreateTextElement(content.Text[0]);
				Grid.SetColumn(sig, 1);
				sigGrid.Children.Add(sig);
			}
			return res;
		}

		FrameworkElement CreateTextElement(HexClassifiedTextCollection coll) {
			var creator = hexTextElementCreatorProvider.Create();
			foreach (var text in coll)
				creator.Writer.Write(GetColor(text.Tag), text.Text);
			return creator.CreateTextElement();
		}

		static object GetColor(string tag) {
			switch (tag) {
			case PredefinedClassifiedTextTags.Text:					return BoxedTextColor.Text;
			case PredefinedClassifiedTextTags.Error:				return BoxedTextColor.Error;
			case PredefinedClassifiedTextTags.Keyword:				return BoxedTextColor.Keyword;
			case PredefinedClassifiedTextTags.Number:				return BoxedTextColor.Number;
			case PredefinedClassifiedTextTags.String:				return BoxedTextColor.String;
			case PredefinedClassifiedTextTags.Char:					return BoxedTextColor.Char;
			case PredefinedClassifiedTextTags.Operator:				return BoxedTextColor.Operator;
			case PredefinedClassifiedTextTags.Punctuation:			return BoxedTextColor.Punctuation;
			case PredefinedClassifiedTextTags.ArrayName:			return BoxedTextColor.Type;
			case PredefinedClassifiedTextTags.StructureName:		return BoxedTextColor.ValueType;
			case PredefinedClassifiedTextTags.ValueType:			return BoxedTextColor.ValueType;
			case PredefinedClassifiedTextTags.Enum:					return BoxedTextColor.Enum;
			case PredefinedClassifiedTextTags.EnumField:			return BoxedTextColor.EnumField;
			case PredefinedClassifiedTextTags.Field:				return BoxedTextColor.InstanceField;
			case PredefinedClassifiedTextTags.PathName:				return BoxedTextColor.DirectoryPart;
			case PredefinedClassifiedTextTags.PathSeparator:		return BoxedTextColor.Text;
			case PredefinedClassifiedTextTags.Filename:				return BoxedTextColor.FileNameNoExtension;
			case PredefinedClassifiedTextTags.FileDot:				return BoxedTextColor.Text;
			case PredefinedClassifiedTextTags.FileExtension:		return BoxedTextColor.FileExtension;
			case PredefinedClassifiedTextTags.DotNetHeapName:		return BoxedTextColor.HexTableName;
			default:												return BoxedTextColor.Text;
			}
		}
	}
}
