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
using dnlib.DotNet;
using dnSpy.Contracts.Languages;
using dnSpy.Languages.ILSpy.CSharp;
using dnSpy.Shared.Highlighting;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Visitors;
using dnSpy.Decompiler.Shared;
using dnSpy.Languages.ILSpy.Settings;

namespace dnSpy.Languages.ILSpy.VB {
	sealed class LanguageProvider : ILanguageProvider {
		readonly LanguageSettingsManager languageSettingsManager;

		// Keep the default ctor. It's used by dnSpy.Console.exe
		public LanguageProvider()
			: this(null) {
		}

		public LanguageProvider(LanguageSettingsManager languageSettingsManager) {
			this.languageSettingsManager = languageSettingsManager ?? LanguageSettingsManager.__Instance_DONT_USE;
		}

		public IEnumerable<ILanguage> Languages {
			get { yield return new VBLanguage(languageSettingsManager.LanguageDecompilerSettings); }
		}
	}

	[Export(typeof(ILanguageCreator))]
	sealed class MyLanguageCreator : ILanguageCreator {
		readonly LanguageSettingsManager languageSettingsManager;

		[ImportingConstructor]
		MyLanguageCreator(LanguageSettingsManager languageSettingsManager) {
			this.languageSettingsManager = languageSettingsManager;
		}

		public IEnumerable<ILanguage> Create() {
			return new LanguageProvider(languageSettingsManager).Languages;
		}
	}

	/// <summary>
	/// Decompiler logic for VB.
	/// </summary>
	sealed class VBLanguage : Language {
		readonly Predicate<IAstTransform> transformAbortCondition = null;
		readonly bool showAllMembers = false;

		public override IDecompilerSettings Settings {
			get { return langSettings; }
		}
		readonly LanguageDecompilerSettings langSettings;

		public override double OrderUI {
			get { return LanguageConstants.VB_ILSPY_ORDERUI; }
		}

		public VBLanguage(LanguageDecompilerSettings langSettings) {
			this.langSettings = langSettings;
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
				output.Write("' ", TextTokenKind.Comment);
			else
				output.Write("'", TextTokenKind.Comment);
		}

		public override void WriteCommentEnd(ITextOutput output, bool addSpace) {
		}

		public override void Decompile(AssemblyDef asm, ITextOutput output, DecompilationContext ctx) {
			WriteAssembly(asm, output, ctx);

			using (ctx.DisableAssemblyLoad()) {
				AstBuilder codeDomBuilder = CreateAstBuilder(ctx, langSettings.Settings, currentModule: asm.ManifestModule);
				codeDomBuilder.AddAssembly(asm.ManifestModule, true, true, false);
				RunTransformsAndGenerateCode(codeDomBuilder, output, ctx);
			}
		}

		public override void Decompile(ModuleDef mod, ITextOutput output, DecompilationContext ctx) {
			WriteModule(mod, output, ctx);

			using (ctx.DisableAssemblyLoad()) {
				AstBuilder codeDomBuilder = CreateAstBuilder(ctx, langSettings.Settings, currentModule: mod);
				codeDomBuilder.AddAssembly(mod, true, false, true);
				RunTransformsAndGenerateCode(codeDomBuilder, output, ctx);
			}
		}

		public override void Decompile(MethodDef method, ITextOutput output, DecompilationContext ctx) {
			WriteCommentLineDeclaringType(output, method);
			AstBuilder codeDomBuilder = CreateAstBuilder(ctx, langSettings.Settings, currentType: method.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddMethod(method);
			RunTransformsAndGenerateCode(codeDomBuilder, output, ctx);
		}

		public override void Decompile(PropertyDef property, ITextOutput output, DecompilationContext ctx) {
			WriteCommentLineDeclaringType(output, property);
			AstBuilder codeDomBuilder = CreateAstBuilder(ctx, langSettings.Settings, currentType: property.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddProperty(property);
			RunTransformsAndGenerateCode(codeDomBuilder, output, ctx);
		}

		public override void Decompile(FieldDef field, ITextOutput output, DecompilationContext ctx) {
			WriteCommentLineDeclaringType(output, field);
			AstBuilder codeDomBuilder = CreateAstBuilder(ctx, langSettings.Settings, currentType: field.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddField(field);
			RunTransformsAndGenerateCode(codeDomBuilder, output, ctx);
		}

		public override void Decompile(EventDef ev, ITextOutput output, DecompilationContext ctx) {
			WriteCommentLineDeclaringType(output, ev);
			AstBuilder codeDomBuilder = CreateAstBuilder(ctx, langSettings.Settings, currentType: ev.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddEvent(ev);
			RunTransformsAndGenerateCode(codeDomBuilder, output, ctx);
		}

		public override void Decompile(TypeDef type, ITextOutput output, DecompilationContext ctx) {
			AstBuilder codeDomBuilder = CreateAstBuilder(ctx, langSettings.Settings, currentType: type);
			codeDomBuilder.AddType(type);
			RunTransformsAndGenerateCode(codeDomBuilder, output, ctx);
		}

		public override bool ShowMember(IMemberRef member) {
			return showAllMembers || !AstBuilder.MemberIsHidden(member, langSettings.Settings);
		}

		void RunTransformsAndGenerateCode(AstBuilder astBuilder, ITextOutput output, DecompilationContext ctx, IAstTransform additionalTransform = null) {
			astBuilder.RunTransformations(transformAbortCondition);
			if (additionalTransform != null) {
				additionalTransform.Run(astBuilder.SyntaxTree);
			}
			CSharpLanguage.AddXmlDocumentation(langSettings.Settings, astBuilder);
			var csharpUnit = astBuilder.SyntaxTree;
			csharpUnit.AcceptVisitor(new ICSharpCode.NRefactory.CSharp.InsertParenthesesVisitor() { InsertParenthesesForReadability = true });
			var unit = csharpUnit.AcceptVisitor(new CSharpToVBConverterVisitor(new ILSpyEnvironmentProvider()), null);
			var outputFormatter = new VBTextOutputFormatter(output);
			var formattingPolicy = new VBFormattingOptions();
			unit.AcceptVisitor(new OutputVisitor(outputFormatter, formattingPolicy), null);
		}

		AstBuilder CreateAstBuilder(DecompilationContext ctx, DecompilerSettings settings, ModuleDef currentModule = null, TypeDef currentType = null, bool isSingleMember = false) {
			if (currentModule == null)
				currentModule = currentType.Module;
			settings = settings.Clone();
			if (isSingleMember)
				settings.UsingDeclarations = false;
			settings.IntroduceIncrementAndDecrement = false;
			settings.MakeAssignmentExpressions = false;
			settings.QueryExpressions = false;
			settings.AlwaysGenerateExceptionVariableForCatchBlocks = true;
			return new AstBuilder(
				new DecompilerContext(currentModule) {
					CancellationToken = ctx.CancellationToken,
					CurrentType = currentType,
					Settings = settings
				}) {
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
				output.Write("ByRef", TextTokenKind.Keyword);
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
			var builder = CreateAstBuilder(info.Context, CSharpLanguage.CreateDecompilerSettings(langSettings.Settings, info.UseUsingDeclarations), currentType: info.Type);
			builder.AddType(info.Type);
			RunTransformsAndGenerateCode(builder, info.Output, info.Context, new DecompilePartialTransform(info.Type, info.Definitions, info.ShowDefinitions, info.AddPartialKeyword, info.InterfacesToRemove));
		}

		void DecompileAssemblyInfo(DecompileAssemblyInfo info) {
			var builder = CreateAstBuilder(info.Context, langSettings.Settings, currentModule: info.Module);
			builder.AddAssembly(info.Module, true, info.Module.IsManifestModule, true);
			RunTransformsAndGenerateCode(builder, info.Output, info.Context, new AssemblyInfoTransform());
		}
	}
}
