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

using dnSpy.Contracts.Themes;

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Classification type names
	/// </summary>
	public static class ThemeClassificationTypeNames {
		/// <summary>
		/// <see cref="ColorType.Text"/>
		/// </summary>
		public const string Text = "A31304D7-09FA-48A8-A369-A68ED0AD73BE";

		/// <summary>
		/// <see cref="ColorType.Operator"/>
		/// </summary>
		public const string Operator = "E9A80BFD-D687-46C8-8000-53B2B9303BBC";

		/// <summary>
		/// <see cref="ColorType.Punctuation"/>
		/// </summary>
		public const string Punctuation = "A25F1F22-77AE-4293-B40E-89E92E26BADC";

		/// <summary>
		/// <see cref="ColorType.Number"/>
		/// </summary>
		public const string Number = "BCC84271-0826-407C-955A-F6336CF411D6";

		/// <summary>
		/// <see cref="ColorType.Comment"/>
		/// </summary>
		public const string Comment = "9CC593EE-5CC6-4B86-9E63-342424279AB2";

		/// <summary>
		/// <see cref="ColorType.Keyword"/>
		/// </summary>
		public const string Keyword = "9B3B9B7B-F7A7-4661-A27E-38D2D834E733";

		/// <summary>
		/// <see cref="ColorType.String"/>
		/// </summary>
		public const string String = "46A61D6C-0EEA-4CBD-A9A1-A6F49CC74B4C";

		/// <summary>
		/// <see cref="ColorType.VerbatimString"/>
		/// </summary>
		public const string VerbatimString = "34ED136C-01A6-48D1-9A2E-1C4E5FFD2C94";

		/// <summary>
		/// <see cref="ColorType.Char"/>
		/// </summary>
		public const string Char = "9AC9369F-0559-49E7-AA84-835AE6677E9D";

		/// <summary>
		/// <see cref="ColorType.Namespace"/>
		/// </summary>
		public const string Namespace = "2361BC0B-0638-485B-8E2C-C9B52CA4E566";

		/// <summary>
		/// <see cref="ColorType.Type"/>
		/// </summary>
		public const string Type = "5DA6AB1E-7591-42DF-A3EB-641AA8F91D95";

		/// <summary>
		/// <see cref="ColorType.SealedType"/>
		/// </summary>
		public const string SealedType = "7401E7B8-BE22-4D0E-8B9E-F2E2844B91BE";

		/// <summary>
		/// <see cref="ColorType.StaticType"/>
		/// </summary>
		public const string StaticType = "C5AE34EB-5712-4EEC-9D13-FCF4A3CD797E";

		/// <summary>
		/// <see cref="ColorType.Delegate"/>
		/// </summary>
		public const string Delegate = "455A8AEF-5812-441E-A62C-4EC87462A2BB";

		/// <summary>
		/// <see cref="ColorType.Enum"/>
		/// </summary>
		public const string Enum = "C41BA031-C533-48DF-B655-0CDB09DF8406";

		/// <summary>
		/// <see cref="ColorType.Interface"/>
		/// </summary>
		public const string Interface = "01C50EA6-3FEB-4317-A865-81ADF8E878E5";

		/// <summary>
		/// <see cref="ColorType.ValueType"/>
		/// </summary>
		public const string ValueType = "2F7F767C-3A98-46B1-87BF-9B2B35AFDEA1";

		/// <summary>
		/// <see cref="ColorType.TypeGenericParameter"/>
		/// </summary>
		public const string TypeGenericParameter = "8C4D84CC-BB4A-4F25-B6B1-080A0132F11D";

		/// <summary>
		/// <see cref="ColorType.MethodGenericParameter"/>
		/// </summary>
		public const string MethodGenericParameter = "8B34FDAE-B102-4F6F-AA8B-E14328E22902";

		/// <summary>
		/// <see cref="ColorType.InstanceMethod"/>
		/// </summary>
		public const string InstanceMethod = "190FA898-AFEA-47BB-B539-D7401BEAB167";

		/// <summary>
		/// <see cref="ColorType.StaticMethod"/>
		/// </summary>
		public const string StaticMethod = "D80F103B-C386-4337-A917-754A77202D94";

		/// <summary>
		/// <see cref="ColorType.ExtensionMethod"/>
		/// </summary>
		public const string ExtensionMethod = "2C2C313D-5342-45A9-BC9A-0C93EEEFB430";

		/// <summary>
		/// <see cref="ColorType.InstanceField"/>
		/// </summary>
		public const string InstanceField = "742D4435-D4F7-4475-943F-7D3CFE9FF839";

		/// <summary>
		/// <see cref="ColorType.EnumField"/>
		/// </summary>
		public const string EnumField = "A3ED5863-D68A-4519-B1C2-A8BCAFD9657A";

		/// <summary>
		/// <see cref="ColorType.LiteralField"/>
		/// </summary>
		public const string LiteralField = "69557932-208B-4EBD-BF2F-32798F8ECF59";

		/// <summary>
		/// <see cref="ColorType.StaticField"/>
		/// </summary>
		public const string StaticField = "F20B8ABE-49AC-4A95-94BD-55EA8DD48F00";

		/// <summary>
		/// <see cref="ColorType.InstanceEvent"/>
		/// </summary>
		public const string InstanceEvent = "43132167-757A-41C8-A42D-B7D66FD8FA90";

		/// <summary>
		/// <see cref="ColorType.StaticEvent"/>
		/// </summary>
		public const string StaticEvent = "5DF06D0A-0D30-4200-A4D2-97B2C4FBDFA5";

		/// <summary>
		/// <see cref="ColorType.InstanceProperty"/>
		/// </summary>
		public const string InstanceProperty = "820B9E3A-4892-4FDF-9A95-CD14F9247DAC";

		/// <summary>
		/// <see cref="ColorType.StaticProperty"/>
		/// </summary>
		public const string StaticProperty = "52BBECD9-FB35-4D5E-9855-3186F85CCEAA";

		/// <summary>
		/// <see cref="ColorType.Local"/>
		/// </summary>
		public const string Local = "AD58C482-7406-4DAD-B539-AC78DC6F70B0";

		/// <summary>
		/// <see cref="ColorType.Parameter"/>
		/// </summary>
		public const string Parameter = "30533891-54A9-47E1-A9A0-A0978B767826";

		/// <summary>
		/// <see cref="ColorType.PreprocessorKeyword"/>
		/// </summary>
		public const string PreprocessorKeyword = "1BA40E97-C507-408E-9940-3D545C12BD55";

		/// <summary>
		/// <see cref="ColorType.PreprocessorText"/>
		/// </summary>
		public const string PreprocessorText = "B445852E-4DA5-4601-A8CC-3A2531226CDE";

		/// <summary>
		/// <see cref="ColorType.Label"/>
		/// </summary>
		public const string Label = "416ADF93-A4F4-47C0-AAB4-748DB67881C2";

		/// <summary>
		/// <see cref="ColorType.OpCode"/>
		/// </summary>
		public const string OpCode = "901A3E5C-B68F-4954-8FFE-4165D747C8AA";

		/// <summary>
		/// <see cref="ColorType.ILDirective"/>
		/// </summary>
		public const string ILDirective = "D2C1697A-ED42-460F-AD02-9F8A75B0F92E";

		/// <summary>
		/// <see cref="ColorType.ILModule"/>
		/// </summary>
		public const string ILModule = "6704FD7B-B59B-4259-8A73-52E602EAA16D";

		/// <summary>
		/// <see cref="ColorType.ExcludedCode"/>
		/// </summary>
		public const string ExcludedCode = "085F1468-FBBE-4A58-803A-98683A9C43E8";

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentAttributeName"/>
		/// </summary>
		public const string XmlDocCommentAttributeName = "DA39B55C-4921-483B-AA34-598F205832DB";

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentAttributeQuotes"/>
		/// </summary>
		public const string XmlDocCommentAttributeQuotes = "8850F5B7-4A62-4496-8DBA-6F8A13E297F0";

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentAttributeValue"/>
		/// </summary>
		public const string XmlDocCommentAttributeValue = "0553BA08-FEC0-499C-966E-85E5B014B773";

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentCDataSection"/>
		/// </summary>
		public const string XmlDocCommentCDataSection = "CBEAE623-7270-45C4-8B55-037D7D401AB9";

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentComment"/>
		/// </summary>
		public const string XmlDocCommentComment = "26B1ECD0-5C44-4910-8D15-2CD4D14CAC5C";

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentDelimiter"/>
		/// </summary>
		public const string XmlDocCommentDelimiter = "B5E04C3E-17A7-40FB-9084-E9AFA265A140";

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentEntityReference"/>
		/// </summary>
		public const string XmlDocCommentEntityReference = "700BBA16-D5B8-4B58-BA98-74E73045D8D6";

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentName"/>
		/// </summary>
		public const string XmlDocCommentName = "A65276CA-EE48-494F-B174-C5F472FA0B3D";

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentProcessingInstruction"/>
		/// </summary>
		public const string XmlDocCommentProcessingInstruction = "624C2E90-A025-44D9-8FA0-DFB9552AF915";

		/// <summary>
		/// <see cref="ColorType.XmlDocCommentText"/>
		/// </summary>
		public const string XmlDocCommentText = "E256EADD-363B-4535-AC5B-98B10FF302C1";

		/// <summary>
		/// <see cref="ColorType.XmlLiteralAttributeName"/>
		/// </summary>
		public const string XmlLiteralAttributeName = "6AD92A42-71C4-4763-9424-7D278D2CB289";

		/// <summary>
		/// <see cref="ColorType.XmlLiteralAttributeQuotes"/>
		/// </summary>
		public const string XmlLiteralAttributeQuotes = "928B7F74-C6F6-45AC-A8ED-FEEF01DCEABC";

		/// <summary>
		/// <see cref="ColorType.XmlLiteralAttributeValue"/>
		/// </summary>
		public const string XmlLiteralAttributeValue = "FBE32B3B-475A-4AD4-B61B-758DCD223546";

		/// <summary>
		/// <see cref="ColorType.XmlLiteralCDataSection"/>
		/// </summary>
		public const string XmlLiteralCDataSection = "926E5776-70B1-4AC0-9F2B-7A1C3734E5D7";

		/// <summary>
		/// <see cref="ColorType.XmlLiteralComment"/>
		/// </summary>
		public const string XmlLiteralComment = "14611BB5-9DFF-4FE5-AA21-C256D32B8A62";

		/// <summary>
		/// <see cref="ColorType.XmlLiteralDelimiter"/>
		/// </summary>
		public const string XmlLiteralDelimiter = "B5C32CB8-BB64-4128-99A7-5C450355EF4D";

		/// <summary>
		/// <see cref="ColorType.XmlLiteralEmbeddedExpression"/>
		/// </summary>
		public const string XmlLiteralEmbeddedExpression = "C4A52664-6156-4ADD-B06D-80079A89322E";

		/// <summary>
		/// <see cref="ColorType.XmlLiteralEntityReference"/>
		/// </summary>
		public const string XmlLiteralEntityReference = "BA535EDA-2917-4037-BD5F-7EF9A9E0CD09";

		/// <summary>
		/// <see cref="ColorType.XmlLiteralName"/>
		/// </summary>
		public const string XmlLiteralName = "F74A98E0-2304-4F20-846C-EFE4764A2CC5";

		/// <summary>
		/// <see cref="ColorType.XmlLiteralProcessingInstruction"/>
		/// </summary>
		public const string XmlLiteralProcessingInstruction = "0CB0AD71-5B20-4E6E-AA78-95D895741AB8";

		/// <summary>
		/// <see cref="ColorType.XmlLiteralText"/>
		/// </summary>
		public const string XmlLiteralText = "B0BCC7D5-A8DC-45EB-A89A-F134130E56D9";

		/// <summary>
		/// <see cref="ColorType.XmlAttributeName"/>
		/// </summary>
		public const string XmlAttributeName = "E51089FD-E43A-46F2-BB30-C8F3F9DB2ED4";

		/// <summary>
		/// <see cref="ColorType.XmlAttributeQuotes"/>
		/// </summary>
		public const string XmlAttributeQuotes = "E6CCFB45-0301-45DA-9866-83038AF91F3C";

		/// <summary>
		/// <see cref="ColorType.XmlAttributeValue"/>
		/// </summary>
		public const string XmlAttributeValue = "92A38B4D-7709-4FC5-80BF-A4FC89182454";

		/// <summary>
		/// <see cref="ColorType.XmlCDataSection"/>
		/// </summary>
		public const string XmlCDataSection = "F17930D2-2CD3-45D8-84DC-F86D62B09BFB";

		/// <summary>
		/// <see cref="ColorType.XmlComment"/>
		/// </summary>
		public const string XmlComment = "73BD320A-E137-413C-AB4F-E03CBC6D0A2A";

		/// <summary>
		/// <see cref="ColorType.XmlDelimiter"/>
		/// </summary>
		public const string XmlDelimiter = "5DFEED92-BFA3-4537-937F-EED78E63DB43";

		/// <summary>
		/// <see cref="ColorType.XmlKeyword"/>
		/// </summary>
		public const string XmlKeyword = "2DF72DA5-D2C8-4A7D-BF27-8E2CA086E18C";

		/// <summary>
		/// <see cref="ColorType.XmlName"/>
		/// </summary>
		public const string XmlName = "EFE0B567-1391-4208-A592-8A820D7B09AF";

		/// <summary>
		/// <see cref="ColorType.XmlProcessingInstruction"/>
		/// </summary>
		public const string XmlProcessingInstruction = "AD1A3339-6BDC-43C6-8BDB-FB2E8C555357";

		/// <summary>
		/// <see cref="ColorType.XmlText"/>
		/// </summary>
		public const string XmlText = "65AC90C9-E135-4FC3-9877-01685886A00C";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipColon"/>
		/// </summary>
		public const string XmlDocToolTipColon = "D6C22967-6A05-4AD9-89A0-9FD03BD19D65";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipExample"/>
		/// </summary>
		public const string XmlDocToolTipExample = "C6C8AEF6-6F12-429D-A3C0-47471AA54506";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipExceptionCref"/>
		/// </summary>
		public const string XmlDocToolTipExceptionCref = "9301732F-7C3F-4259-8AE0-DD46B6B9DB22";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipReturns"/>
		/// </summary>
		public const string XmlDocToolTipReturns = "D5C5DF66-C117-48DE-8325-F7DC122C99B0";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipSeeCref"/>
		/// </summary>
		public const string XmlDocToolTipSeeCref = "2E28BC41-0BA2-491F-8FE4-A528108BB2AA";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipSeeLangword"/>
		/// </summary>
		public const string XmlDocToolTipSeeLangword = "5E368EA9-2CF8-43FF-83B9-6A779722E864";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipSeeAlso"/>
		/// </summary>
		public const string XmlDocToolTipSeeAlso = "691C0173-4C43-4571-904C-8E1CB63C1073";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipSeeAlsoCref"/>
		/// </summary>
		public const string XmlDocToolTipSeeAlsoCref = "6A1945A6-65B4-4A2B-BC92-FB38926A34A3";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipParamRefName"/>
		/// </summary>
		public const string XmlDocToolTipParamRefName = "AA742963-70AE-473F-99D6-184C4F7A30DB";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipParamName"/>
		/// </summary>
		public const string XmlDocToolTipParamName = "C6E5562D-B9B1-4DD0-A662-B44E2F15A18E";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipTypeParamName"/>
		/// </summary>
		public const string XmlDocToolTipTypeParamName = "14C5D846-118E-41A2-8706-9E3A3A8DE5D4";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipValue"/>
		/// </summary>
		public const string XmlDocToolTipValue = "5FA6853E-5CE4-4D51-8AE3-DC2B40D4ECE3";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipSummary"/>
		/// </summary>
		public const string XmlDocToolTipSummary = "AEC8627D-7722-41B9-B70B-C245FA45E01C";

		/// <summary>
		/// <see cref="ColorType.XmlDocToolTipText"/>
		/// </summary>
		public const string XmlDocToolTipText = "CD468A1D-ECEF-4BF3-8E6D-BDB905B9C341";

		/// <summary>
		/// <see cref="ColorType.Assembly"/>
		/// </summary>
		public const string Assembly = "713D6124-A14F-44C3-BD11-7B1F3F3BE137";

		/// <summary>
		/// <see cref="ColorType.AssemblyExe"/>
		/// </summary>
		public const string AssemblyExe = "477772BE-E83C-48AA-9A53-AFD9BE3B2E03";

		/// <summary>
		/// <see cref="ColorType.Module"/>
		/// </summary>
		public const string Module = "1422FFB6-FB45-43C9-8BA4-CA9907237EB1";

		/// <summary>
		/// <see cref="ColorType.DirectoryPart"/>
		/// </summary>
		public const string DirectoryPart = "0B5BBDBB-2755-4C79-A970-6977531EC427";

		/// <summary>
		/// <see cref="ColorType.FileNameNoExtension"/>
		/// </summary>
		public const string FileNameNoExtension = "86668F36-D8D8-4D20-A094-C0D1F14FE84C";

		/// <summary>
		/// <see cref="ColorType.FileExtension"/>
		/// </summary>
		public const string FileExtension = "5A4E703A-C285-4B38-8692-4582DD1AD731";

		/// <summary>
		/// <see cref="ColorType.Error"/>
		/// </summary>
		public const string Error = "6AE9998C-B80F-42AB-9CE9-BED3C7817C3D";

		/// <summary>
		/// <see cref="ColorType.ToStringEval"/>
		/// </summary>
		public const string ToStringEval = "0F3163E1-3309-4120-A9E1-20F9CB19764B";

		/// <summary>
		/// <see cref="ColorType.ReplPrompt1"/>
		/// </summary>
		public const string ReplPrompt1 = "95F8230F-0B00-4D32-9D1B-7522DF91AEF5";

		/// <summary>
		/// <see cref="ColorType.ReplPrompt2"/>
		/// </summary>
		public const string ReplPrompt2 = "3964FFE6-42E8-4583-A750-0B6BDB022984";

		/// <summary>
		/// <see cref="ColorType.ReplOutputText"/>
		/// </summary>
		public const string ReplOutputText = "ED6064A3-556F-424F-AF14-A8DDCB1E5B30";

		/// <summary>
		/// <see cref="ColorType.ReplScriptOutputText"/>
		/// </summary>
		public const string ReplScriptOutputText = "0F77BC80-5538-48CE-962E-8D76FDC2AC7F";

		/// <summary>
		/// <see cref="ColorType.Black"/>
		/// </summary>
		public const string Black = "BBB012B1-62E8-4DCB-83AF-AF05EBBE39C4";

		/// <summary>
		/// <see cref="ColorType.Blue"/>
		/// </summary>
		public const string Blue = "23ED27B7-8743-46BA-822A-4221CFFBE4DD";

		/// <summary>
		/// <see cref="ColorType.Cyan"/>
		/// </summary>
		public const string Cyan = "A3A36B81-FC7E-4DA8-9A3C-C8CC1B88DA02";

		/// <summary>
		/// <see cref="ColorType.DarkBlue"/>
		/// </summary>
		public const string DarkBlue = "C0E3BE05-5B81-4D98-AEBA-3E0BD31C5A92";

		/// <summary>
		/// <see cref="ColorType.DarkCyan"/>
		/// </summary>
		public const string DarkCyan = "67721EA8-E01A-47E9-AB34-FA2067478A22";

		/// <summary>
		/// <see cref="ColorType.DarkGray"/>
		/// </summary>
		public const string DarkGray = "89A25352-2F17-48C7-91B6-6D22F02D1297";

		/// <summary>
		/// <see cref="ColorType.DarkGreen"/>
		/// </summary>
		public const string DarkGreen = "D9BA00A6-47AB-4AF6-A3A2-71EDBB5EAD0F";

		/// <summary>
		/// <see cref="ColorType.DarkMagenta"/>
		/// </summary>
		public const string DarkMagenta = "96BF5385-8FCC-47B1-AD7C-A5A8FBB11C8B";

		/// <summary>
		/// <see cref="ColorType.DarkRed"/>
		/// </summary>
		public const string DarkRed = "17FC89A4-E6A2-45E6-A901-D760C042D1C0";

		/// <summary>
		/// <see cref="ColorType.DarkYellow"/>
		/// </summary>
		public const string DarkYellow = "26A13B95-5B27-42CC-A541-FE12DE4A18C9";

		/// <summary>
		/// <see cref="ColorType.Gray"/>
		/// </summary>
		public const string Gray = "F89AF1D2-89F7-4567-8F47-4D1FBD46A64A";

		/// <summary>
		/// <see cref="ColorType.Green"/>
		/// </summary>
		public const string Green = "ABA23ACF-04B2-4261-81B0-CB670ABFCC16";

		/// <summary>
		/// <see cref="ColorType.Magenta"/>
		/// </summary>
		public const string Magenta = "AE0F715B-120E-47A1-B108-B1FE6B63EDF5";

		/// <summary>
		/// <see cref="ColorType.Red"/>
		/// </summary>
		public const string Red = "F483385E-5D6E-4806-95C2-04471B90F07B";

		/// <summary>
		/// <see cref="ColorType.White"/>
		/// </summary>
		public const string White = "472F3828-586B-4B0B-94BF-4873AFE6AFC4";

		/// <summary>
		/// <see cref="ColorType.Yellow"/>
		/// </summary>
		public const string Yellow = "C8897C23-34A5-4905-BE5E-73D11B70B050";

		/// <summary>
		/// <see cref="ColorType.InvBlack"/>
		/// </summary>
		public const string InvBlack = "4C0BD792-ACC4-4CDA-B06C-C1890D8B1EA4";

		/// <summary>
		/// <see cref="ColorType.InvBlue"/>
		/// </summary>
		public const string InvBlue = "A6C6BB96-622C-4E8E-9A45-87A3AB534ABF";

		/// <summary>
		/// <see cref="ColorType.InvCyan"/>
		/// </summary>
		public const string InvCyan = "DD756DD0-B5ED-405B-987B-237DD25D58CA";

		/// <summary>
		/// <see cref="ColorType.InvDarkBlue"/>
		/// </summary>
		public const string InvDarkBlue = "1D9B9867-DB7C-4B94-B090-757786D85B68";

		/// <summary>
		/// <see cref="ColorType.InvDarkCyan"/>
		/// </summary>
		public const string InvDarkCyan = "80949B6F-B131-44C5-970D-02A9FC39AEAF";

		/// <summary>
		/// <see cref="ColorType.InvDarkGray"/>
		/// </summary>
		public const string InvDarkGray = "8A0C343E-B5E5-499C-9EB3-1C25AA1BBE39";

		/// <summary>
		/// <see cref="ColorType.InvDarkGreen"/>
		/// </summary>
		public const string InvDarkGreen = "37D8A524-C36B-4754-B3DE-7B7D6771124E";

		/// <summary>
		/// <see cref="ColorType.InvDarkMagenta"/>
		/// </summary>
		public const string InvDarkMagenta = "3815D5FC-8E16-4DC3-B4DD-B6BA364CCD0A";

		/// <summary>
		/// <see cref="ColorType.InvDarkRed"/>
		/// </summary>
		public const string InvDarkRed = "380D93C3-624D-4A1D-A4BE-C9D9B00BE2CE";

		/// <summary>
		/// <see cref="ColorType.InvDarkYellow"/>
		/// </summary>
		public const string InvDarkYellow = "464F47BC-9091-4D51-8F19-A35A4E35DD46";

		/// <summary>
		/// <see cref="ColorType.InvGray"/>
		/// </summary>
		public const string InvGray = "73DE6B74-0DFD-4884-9624-8827269D1C3B";

		/// <summary>
		/// <see cref="ColorType.InvGreen"/>
		/// </summary>
		public const string InvGreen = "11F16E6D-E23E-41E9-829E-4F128A220CEB";

		/// <summary>
		/// <see cref="ColorType.InvMagenta"/>
		/// </summary>
		public const string InvMagenta = "9345EAA4-723B-4B01-877F-FFFA4A45743B";

		/// <summary>
		/// <see cref="ColorType.InvRed"/>
		/// </summary>
		public const string InvRed = "4896B517-A25D-4FF2-A7AA-19FCD8109885";

		/// <summary>
		/// <see cref="ColorType.InvWhite"/>
		/// </summary>
		public const string InvWhite = "02324660-110F-406E-A959-1C94DC88F01D";

		/// <summary>
		/// <see cref="ColorType.InvYellow"/>
		/// </summary>
		public const string InvYellow = "72EEB0FD-5159-4281-9BC3-C2BD71220FF2";

		/// <summary>
		/// <see cref="ColorType.DebugLogExceptionHandled"/>
		/// </summary>
		public const string DebugLogExceptionHandled = "DF605B05-5063-4333-B28A-E4B7951C2935";

		/// <summary>
		/// <see cref="ColorType.DebugLogExceptionUnhandled"/>
		/// </summary>
		public const string DebugLogExceptionUnhandled = "3EC734C4-245B-4D39-8ACD-073F7AAB1676";

		/// <summary>
		/// <see cref="ColorType.DebugLogStepFiltering"/>
		/// </summary>
		public const string DebugLogStepFiltering = "F41810E9-6744-4CFA-BD74-399827343DFA";

		/// <summary>
		/// <see cref="ColorType.DebugLogLoadModule"/>
		/// </summary>
		public const string DebugLogLoadModule = "F5DA1ACD-82D0-41A4-9DEA-3974A1A3F949";

		/// <summary>
		/// <see cref="ColorType.DebugLogUnloadModule"/>
		/// </summary>
		public const string DebugLogUnloadModule = "FB5C4C5F-1538-4DDE-BFC2-6F63C3619955";

		/// <summary>
		/// <see cref="ColorType.DebugLogExitProcess"/>
		/// </summary>
		public const string DebugLogExitProcess = "ADCE4AF6-D7B6-4178-8BB5-0D5A52E0A197";

		/// <summary>
		/// <see cref="ColorType.DebugLogExitThread"/>
		/// </summary>
		public const string DebugLogExitThread = "AF90E213-6C37-460A-9D77-B29F3ABCFD58";

		/// <summary>
		/// <see cref="ColorType.DebugLogProgramOutput"/>
		/// </summary>
		public const string DebugLogProgramOutput = "F2B04CAE-1915-4A91-AB83-E4A7D9581FD6";

		/// <summary>
		/// <see cref="ColorType.DebugLogMDA"/>
		/// </summary>
		public const string DebugLogMDA = "AD47BC5E-9606-41BC-8848-1E946F341EF7";

		/// <summary>
		/// <see cref="ColorType.DebugLogTimestamp"/>
		/// </summary>
		public const string DebugLogTimestamp = "452246A5-DDE3-480C-89FF-FB9E9591BB8B";
	}
}
