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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Themes;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Themes;

namespace dnSpy.Text.Classification {
	[Export(typeof(IThemeClassificationTypes))]
	sealed class ThemeClassificationTypes : IThemeClassificationTypes {
		readonly IClassificationType[] classificationTypes;

		[ImportingConstructor]
		ThemeClassificationTypes(IClassificationTypeRegistryService classificationTypeRegistryService) {
			this.classificationTypes = new IClassificationType[(int)(ColorType.LastNR - ColorType.FirstNR)] {
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Text),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Operator),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Punctuation),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Number),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Comment),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Keyword),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.String),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.VerbatimString),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Char),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Namespace),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Type),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.SealedType),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.StaticType),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Delegate),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Enum),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Interface),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.ValueType),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.TypeGenericParameter),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.MethodGenericParameter),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InstanceMethod),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.StaticMethod),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.ExtensionMethod),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InstanceField),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.EnumField),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.LiteralField),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.StaticField),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InstanceEvent),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.StaticEvent),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InstanceProperty),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.StaticProperty),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Local),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Parameter),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.PreprocessorKeyword),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.PreprocessorText),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Label),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.OpCode),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.ILDirective),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.ILModule),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.ExcludedCode),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocCommentAttributeName),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocCommentAttributeQuotes),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocCommentAttributeValue),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocCommentCDataSection),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocCommentComment),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocCommentDelimiter),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocCommentEntityReference),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocCommentName),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocCommentProcessingInstruction),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocCommentText),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlLiteralAttributeName),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlLiteralAttributeQuotes),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlLiteralAttributeValue),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlLiteralCDataSection),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlLiteralComment),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlLiteralDelimiter),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlLiteralEmbeddedExpression),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlLiteralEntityReference),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlLiteralName),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlLiteralProcessingInstruction),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlLiteralText),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlAttributeName),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlAttributeQuotes),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlAttributeValue),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlCDataSection),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlComment),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDelimiter),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlKeyword),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlName),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlProcessingInstruction),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlText),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocToolTipColon),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocToolTipExample),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocToolTipExceptionCref),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocToolTipReturns),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocToolTipSeeCref),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocToolTipSeeLangword),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocToolTipSeeAlso),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocToolTipSeeAlsoCref),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocToolTipParamRefName),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocToolTipParamName),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocToolTipTypeParamName),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocToolTipValue),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocToolTipSummary),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.XmlDocToolTipText),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Assembly),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.AssemblyExe),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Module),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DirectoryPart),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.FileNameNoExtension),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.FileExtension),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Error),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.ToStringEval),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.ReplPrompt1),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.ReplPrompt2),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.ReplOutputText),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.ReplScriptOutputText),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Black),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Blue),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Cyan),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DarkBlue),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DarkCyan),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DarkGray),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DarkGreen),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DarkMagenta),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DarkRed),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DarkYellow),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Gray),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Green),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Magenta),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Red),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.White),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Yellow),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvBlack),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvBlue),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvCyan),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvDarkBlue),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvDarkCyan),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvDarkGray),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvDarkGreen),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvDarkMagenta),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvDarkRed),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvDarkYellow),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvGray),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvGreen),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvMagenta),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvRed),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvWhite),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.InvYellow),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DebugLogExceptionHandled),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DebugLogExceptionUnhandled),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DebugLogStepFiltering),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DebugLogLoadModule),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DebugLogUnloadModule),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DebugLogExitProcess),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DebugLogExitThread),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DebugLogProgramOutput),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DebugLogMDA),
				classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.DebugLogTimestamp),
			};
		}

		const ColorType DEFAULT_COLOR = ColorType.Text;

		public IClassificationType GetClassificationType(ColorType color) {
			if (!(ColorType.FirstNR <= color && color < ColorType.LastNR))
				color = DEFAULT_COLOR;
			int index = (int)(color - ColorType.FirstNR);
			return classificationTypes[index];
		}

		public IClassificationType GetClassificationTypeByColorObject(object color) {
			if (color is TextTokenKind)
				return GetClassificationType(((TextTokenKind)color).ToColorType());

			if (color is ColorType)
				return GetClassificationType((ColorType)color);

			if (color is OutputColor)
				return GetClassificationType(((OutputColor)color).ToColorType());

			return GetClassificationType(DEFAULT_COLOR);
		}
	}
}
