// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Xml;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Languages;
using dnSpy.Languages.ILSpy.CSharp;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.Highlighting;
using dnSpy.Shared.UI.Languages.XmlDoc;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Visitors;

namespace dnSpy.Languages.ILSpy.VB {
	sealed class LanguageProvider : ILanguageProvider {
		public IEnumerable<ILanguage> Languages {
			get { yield return new VBLanguage(); }
		}
	}

	[Export(typeof(ILanguageCreator))]
	sealed class MyLanguageCreator : ILanguageCreator {
		public IEnumerable<ILanguage> Create() {
			return new LanguageProvider().Languages;
		}
	}

	/// <summary>
	/// Decompiler logic for VB.
	/// </summary>
	sealed class VBLanguage : Language {
		readonly Predicate<IAstTransform> transformAbortCondition = null;
		bool showAllMembers = false;

		public override double OrderUI {
			get { return LanguageConstants.VB_ILSPY_ORDERUI; }
		}

		public VBLanguage() {
		}

		public override string GenericNameUI {
			get { return LanguageConstants.GENERIC_NAMEUI_VB; }
		}

		public override string UniqueNameUI {
			get { return "VB"; }
		}

		public override Guid GenericGuid {
			get { return LanguageConstants.LANGUAGE_VB; }
		}

		public override Guid UniqueGuid {
			get { return LanguageConstants.LANGUAGE_VB_ILSPY; }
		}

		public override string FileExtension {
			get { return ".vb"; }
		}

		public override string ProjectFileExtension {
			get { return ".vbproj"; }
		}

		public override void WriteCommentBegin(ITextOutput output, bool addSpace) {
			if (addSpace)
				output.Write("' ", TextTokenType.Comment);
			else
				output.Write("'", TextTokenType.Comment);
		}

		public override void WriteCommentEnd(ITextOutput output, bool addSpace) {
		}

		public override void DecompileAssembly(IDnSpyFile file, ITextOutput output, DecompilationOptions options, DecompileAssemblyFlags flags = DecompileAssemblyFlags.AssemblyAndModule) {
			WriteModuleAssembly(file, output, options, flags);

			bool decompileAsm = (flags & DecompileAssemblyFlags.Assembly) != 0;
			bool decompileMod = (flags & DecompileAssemblyFlags.Module) != 0;
			using (options.DisableAssemblyLoad()) {
				AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: file.ModuleDef);
				codeDomBuilder.AddAssembly(file.ModuleDef, true, decompileAsm, decompileMod);
				RunTransformsAndGenerateCode(codeDomBuilder, output, options);
			}
		}

		public override void Decompile(MethodDef method, ITextOutput output, DecompilationOptions options) {
			WriteCommentLineDeclaringType(output, method);
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: method.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddMethod(method);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options);
		}

		public override void Decompile(PropertyDef property, ITextOutput output, DecompilationOptions options) {
			WriteCommentLineDeclaringType(output, property);
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: property.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddProperty(property);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options);
		}

		public override void Decompile(FieldDef field, ITextOutput output, DecompilationOptions options) {
			WriteCommentLineDeclaringType(output, field);
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: field.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddField(field);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options);
		}

		public override void Decompile(EventDef ev, ITextOutput output, DecompilationOptions options) {
			WriteCommentLineDeclaringType(output, ev);
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: ev.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddEvent(ev);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options);
		}

		public override void Decompile(TypeDef type, ITextOutput output, DecompilationOptions options) {
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: type);
			codeDomBuilder.AddType(type);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options);
		}

		public override bool ShowMember(IMemberRef member, DecompilerSettings decompilerSettings) {
			return showAllMembers || !AstBuilder.MemberIsHidden(member, decompilerSettings);
		}

		void RunTransformsAndGenerateCode(AstBuilder astBuilder, ITextOutput output, DecompilationOptions options, IAstTransform additionalTransform = null) {
			astBuilder.RunTransformations(transformAbortCondition);
			if (additionalTransform != null) {
				additionalTransform.Run(astBuilder.SyntaxTree);
			}
			AddXmlDocumentation(options.DecompilerSettings, astBuilder);
			var csharpUnit = astBuilder.SyntaxTree;
			csharpUnit.AcceptVisitor(new ICSharpCode.NRefactory.CSharp.InsertParenthesesVisitor() { InsertParenthesesForReadability = true });
			var unit = csharpUnit.AcceptVisitor(new CSharpToVBConverterVisitor(new ILSpyEnvironmentProvider()), null);
			var outputFormatter = new VBTextOutputFormatter(output);
			var formattingPolicy = new VBFormattingOptions();
			unit.AcceptVisitor(new OutputVisitor(outputFormatter, formattingPolicy), null);
		}

		static void AddXmlDocumentation(DecompilerSettings settings, AstBuilder astBuilder) {
			if (settings.ShowXmlDocumentation) {
				try {
					AddXmlDocTransform.Run(astBuilder.SyntaxTree);
				}
				catch (XmlException ex) {
					string[] msg = (" Exception while reading XmlDoc: " + ex.ToString()).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					var insertionPoint = astBuilder.SyntaxTree.FirstChild;
					for (int i = 0; i < msg.Length; i++)
						astBuilder.SyntaxTree.InsertChildBefore(insertionPoint, new ICSharpCode.NRefactory.CSharp.Comment(msg[i], ICSharpCode.NRefactory.CSharp.CommentType.Documentation), ICSharpCode.NRefactory.CSharp.Roles.Comment);
				}
			}
		}

		AstBuilder CreateAstBuilder(DecompilationOptions options, ModuleDef currentModule = null, TypeDef currentType = null, bool isSingleMember = false) {
			if (currentModule == null)
				currentModule = currentType.Module;
			DecompilerSettings settings = options.DecompilerSettings;
			settings = settings.Clone();
			if (isSingleMember)
				settings.UsingDeclarations = false;
			settings.IntroduceIncrementAndDecrement = false;
			settings.MakeAssignmentExpressions = false;
			settings.QueryExpressions = false;
			settings.AlwaysGenerateExceptionVariableForCatchBlocks = true;
			return new AstBuilder(
				new DecompilerContext(currentModule) {
					CancellationToken = options.CancellationToken,
					CurrentType = currentType,
					Settings = settings
				}) {
				DontShowCreateMethodBodyExceptions = options.DontShowCreateMethodBodyExceptions,
			};
		}

		protected override void FormatTypeName(ITextOutput output, TypeDef type) {
			if (type == null)
				throw new ArgumentNullException("type");

			TypeToString(output, ConvertTypeOptions.DoNotUsePrimitiveTypeNames | ConvertTypeOptions.IncludeTypeParameterDefinitions, type);
		}

		protected override void TypeToString(ITextOutput output, ITypeDefOrRef type, bool includeNamespace, IHasCustomAttribute typeAttributes = null) {
			ConvertTypeOptions options = ConvertTypeOptions.IncludeTypeParameterDefinitions;
			if (includeNamespace)
				options |= ConvertTypeOptions.IncludeNamespace;

			TypeToString(output, options, type, typeAttributes);
		}

		void TypeToString(ITextOutput output, ConvertTypeOptions options, ITypeDefOrRef type, IHasCustomAttribute typeAttributes = null) {
			var envProvider = new ILSpyEnvironmentProvider();
			var converter = new CSharpToVBConverterVisitor(envProvider);
			var astType = AstBuilder.ConvertType(type, typeAttributes, options);

			if (type.TryGetByRefSig() != null) {
				output.Write("ByRef", TextTokenType.Keyword);
				output.WriteSpace();
				if (astType is ICSharpCode.NRefactory.CSharp.ComposedType && ((ICSharpCode.NRefactory.CSharp.ComposedType)astType).PointerRank > 0)
					((ICSharpCode.NRefactory.CSharp.ComposedType)astType).PointerRank--;
			}

			var vbAstType = astType.AcceptVisitor(converter, null);

			vbAstType.AcceptVisitor(new OutputVisitor(new VBTextOutputFormatter(output), new VBFormattingOptions()), null);
		}

		public override bool CanDecompile(DecompilationType decompilationType) {
			switch (decompilationType) {
			case DecompilationType.PartialType:
			case DecompilationType.AssemblyInfo:
				return true;
			}
			return base.CanDecompile(decompilationType);
		}

		public override void Decompile(DecompilationType decompilationType, object data) {
			switch (decompilationType) {
			case DecompilationType.PartialType:
				DecompilePartial((DecompilePartialType)data);
				return;
			case DecompilationType.AssemblyInfo:
				DecompileAssemblyInfo((DecompileAssemblyInfo)data);
				return;
			}
			base.Decompile(decompilationType, data);
		}

		void DecompilePartial(DecompilePartialType info) {
			var builder = CreateAstBuilder(CSharpLanguage.CreateDecompilationOptions(info.Options, info.UseUsingDeclarations), currentType: info.Type);
			builder.AddType(info.Type);
			RunTransformsAndGenerateCode(builder, info.Output, info.Options, new DecompilePartialTransform(info.Type, info.Definitions, info.ShowDefinitions, info.AddPartialKeyword, info.InterfacesToRemove));
		}

		void DecompileAssemblyInfo(DecompileAssemblyInfo info) {
			var builder = CreateAstBuilder(info.Options, currentModule: info.Module);
			builder.AddAssembly(info.Module, true, info.Module.IsManifestModule, true);
			RunTransformsAndGenerateCode(builder, info.Output, info.Options, new AssemblyInfoTransform());
		}
	}
}
