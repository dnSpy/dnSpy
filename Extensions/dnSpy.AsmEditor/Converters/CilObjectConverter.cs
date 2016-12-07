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
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using dnSpy.AsmEditor.MethodBody;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.AsmEditor.Converters {
	sealed class CilObjectConverter : IValueConverter {
		public static readonly CilObjectConverter Instance = new CilObjectConverter();

		static IClassificationFormatMap classificationFormatMap;
		static ITextElementProvider textElementProvider;

		[ExportAutoLoaded]
		sealed class Loader : IAutoLoaded {
			[ImportingConstructor]
			Loader(IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
				classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.TextEditor);
				CilObjectConverter.textElementProvider = textElementProvider;
			}
		}

		static class Cache {
			static readonly TextClassifierTextColorWriter writer = new TextClassifierTextColorWriter();
			public static TextClassifierTextColorWriter GetWriter() => writer;
			public static void FreeWriter(TextClassifierTextColorWriter writer) { writer.Clear(); }
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			try {
				var flags = WriteObjectFlags.None;
				if (parameter != null) {
					foreach (var c in (string)parameter) {
						if (c == 's')
							flags |= WriteObjectFlags.ShortInstruction;
					}
				}

				var writer = Cache.GetWriter();
				try {
					BodyUtils.WriteObject(writer, value, flags);
					const bool colorize = true;
					var context = new TextClassifierContext(writer.Text, PredefinedTextClassifierTags.MethodBodyEditor, colorize, writer.Colors);
					var elem = textElementProvider.CreateTextElement(classificationFormatMap, context, ContentTypes.MethodBodyEditor, TextElementFlags.CharacterEllipsis | TextElementFlags.FilterOutNewLines);
					Cache.FreeWriter(writer);
					return elem;
				}
				finally {
					Cache.FreeWriter(writer);
				}
			}
			catch (Exception ex) {
				Debug.Fail(ex.ToString());
			}

			if (value == null)
				return string.Empty;
			return value.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
