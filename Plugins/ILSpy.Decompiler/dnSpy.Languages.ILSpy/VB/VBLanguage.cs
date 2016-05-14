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
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Visitors;
using dnSpy.Decompiler.Shared;
using dnSpy.Languages.ILSpy.Settings;
using System.Diagnostics;
using System.Text;
using dnSpy.Contracts.Text;

namespace dnSpy.Languages.ILSpy.VB {
	sealed class LanguageProvider : ILanguageProvider {
		readonly LanguageSettingsManager languageSettingsManager;

		// Keep the default ctor. It's used by dnSpy.Console.exe
		public LanguageProvider()
			: this(LanguageSettingsManager.__Instance_DONT_USE) {
		}

		public LanguageProvider(LanguageSettingsManager languageSettingsManager) {
			Debug.Assert(languageSettingsManager != null);
			if (languageSettingsManager == null)
				throw new ArgumentNullException();
			this.languageSettingsManager = languageSettingsManager;
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

		public IEnumerable<ILanguage> Create() => new LanguageProvider(languageSettingsManager).Languages;
	}

	/// <summary>
	/// Decompiler logic for VB.
	/// </summary>
	sealed class VBLanguage : Language {
		readonly Predicate<IAstTransform> transformAbortCondition = null;
		readonly bool showAllMembers = false;

		public override IDecompilerSettings Settings => langSettings;
		readonly LanguageDecompilerSettings langSettings;

		public override double OrderUI => LanguageConstants.VISUALBASIC_ILSPY_ORDERUI;

		public VBLanguage(LanguageDecompilerSettings langSettings) {
			this.langSettings = langSettings;
		}

		public override Guid ContentTypeGuid => new Guid(ContentTypes.VISUALBASIC_ILSPY);
		public override string GenericNameUI => LanguageConstants.GENERIC_NAMEUI_VISUALBASIC;
		public override string UniqueNameUI => "Visual Basic";
		public override Guid GenericGuid => LanguageConstants.LANGUAGE_VISUALBASIC;
		public override Guid UniqueGuid => LanguageConstants.LANGUAGE_VISUALBASIC_ILSPY;
		public override string FileExtension => ".vb";
		public override string ProjectFileExtension => ".vbproj";

		public override void WriteCommentBegin(ITextOutput output, bool addSpace) {
			if (addSpace)
				output.Write("' ", BoxedTextTokenKind.Comment);
			else
				output.Write("'", BoxedTextTokenKind.Comment);
		}

		public override void WriteCommentEnd(ITextOutput output, bool addSpace) { }

		public override void Decompile(AssemblyDef asm, ITextOutput output, DecompilationContext ctx) {
			WriteAssembly(asm, output, ctx);

			using (ctx.DisableAssemblyLoad()) {
				var state = CreateAstBuilder(ctx, langSettings.Settings, currentModule: asm.ManifestModule);
				try {
					state.AstBuilder.AddAssembly(asm.ManifestModule, true, true, false);
					RunTransformsAndGenerateCode(ref state, output, ctx);
				}
				finally {
					state.Dispose();
				}
			}
		}

		public override void Decompile(ModuleDef mod, ITextOutput output, DecompilationContext ctx) {
			WriteModule(mod, output, ctx);

			using (ctx.DisableAssemblyLoad()) {
				var state = CreateAstBuilder(ctx, langSettings.Settings, currentModule: mod);
				try {
					state.AstBuilder.AddAssembly(mod, true, false, true);
					RunTransformsAndGenerateCode(ref state, output, ctx);
				}
				finally {
					state.Dispose();
				}
			}
		}

		public override void Decompile(MethodDef method, ITextOutput output, DecompilationContext ctx) {
			WriteCommentLineDeclaringType(output, method);
			var state = CreateAstBuilder(ctx, langSettings.Settings, currentType: method.DeclaringType, isSingleMember: true);
			try {
				state.AstBuilder.AddMethod(method);
				RunTransformsAndGenerateCode(ref state, output, ctx);
			}
			finally {
				state.Dispose();
			}
		}

		public override void Decompile(PropertyDef property, ITextOutput output, DecompilationContext ctx) {
			WriteCommentLineDeclaringType(output, property);
			var state = CreateAstBuilder(ctx, langSettings.Settings, currentType: property.DeclaringType, isSingleMember: true);
			try {
				state.AstBuilder.AddProperty(property);
				RunTransformsAndGenerateCode(ref state, output, ctx);
			}
			finally {
				state.Dispose();
			}
		}

		public override void Decompile(FieldDef field, ITextOutput output, DecompilationContext ctx) {
			WriteCommentLineDeclaringType(output, field);
			var state = CreateAstBuilder(ctx, langSettings.Settings, currentType: field.DeclaringType, isSingleMember: true);
			try {
				state.AstBuilder.AddField(field);
				RunTransformsAndGenerateCode(ref state, output, ctx);
			}
			finally {
				state.Dispose();
			}
		}

		public override void Decompile(EventDef ev, ITextOutput output, DecompilationContext ctx) {
			WriteCommentLineDeclaringType(output, ev);
			var state = CreateAstBuilder(ctx, langSettings.Settings, currentType: ev.DeclaringType, isSingleMember: true);
			try {
				state.AstBuilder.AddEvent(ev);
				RunTransformsAndGenerateCode(ref state, output, ctx);
			}
			finally {
				state.Dispose();
			}
		}

		public override void Decompile(TypeDef type, ITextOutput output, DecompilationContext ctx) {
			var state = CreateAstBuilder(ctx, langSettings.Settings, currentType: type);
			try {
				state.AstBuilder.AddType(type);
				RunTransformsAndGenerateCode(ref state, output, ctx);
			}
			finally {
				state.Dispose();
			}
		}

		public override bool ShowMember(IMemberRef member) => showAllMembers || !AstBuilder.MemberIsHidden(member, langSettings.Settings);

		void RunTransformsAndGenerateCode(ref BuilderState state, ITextOutput output, DecompilationContext ctx, IAstTransform additionalTransform = null) {
			var astBuilder = state.AstBuilder;
			astBuilder.RunTransformations(transformAbortCondition);
			if (additionalTransform != null) {
				additionalTransform.Run(astBuilder.SyntaxTree);
			}
			CSharpLanguage.AddXmlDocumentation(ref state, langSettings.Settings, astBuilder);
			var csharpUnit = astBuilder.SyntaxTree;
			csharpUnit.AcceptVisitor(new ICSharpCode.NRefactory.CSharp.InsertParenthesesVisitor() { InsertParenthesesForReadability = true });
			var unit = csharpUnit.AcceptVisitor(new CSharpToVBConverterVisitor(new ILSpyEnvironmentProvider(state.State.XmlDoc_StringBuilder)), null);
			var outputFormatter = new VBTextOutputFormatter(output);
			var formattingPolicy = new VBFormattingOptions();
			unit.AcceptVisitor(new OutputVisitor(outputFormatter, formattingPolicy), null);
		}

		BuilderState CreateAstBuilder(DecompilationContext ctx, DecompilerSettings settings, ModuleDef currentModule = null, TypeDef currentType = null, bool isSingleMember = false) {
			if (currentModule == null)
				currentModule = currentType.Module;
			settings = settings.Clone();
			if (isSingleMember)
				settings.UsingDeclarations = false;
			settings.IntroduceIncrementAndDecrement = false;
			settings.MakeAssignmentExpressions = false;
			settings.QueryExpressions = false;
			settings.AlwaysGenerateExceptionVariableForCatchBlocks = true;
			var cache = ctx.GetOrCreate<BuilderCache>();
			var state = new BuilderState(ctx, cache);
			state.AstBuilder.Context.CurrentModule = currentModule;
			state.AstBuilder.Context.CancellationToken = ctx.CancellationToken;
			state.AstBuilder.Context.CurrentType = currentType;
			state.AstBuilder.Context.Settings = settings;
			return state;
		}

		protected override void FormatTypeName(ITextOutput output, TypeDef type) {
			if (type == null)
				throw new ArgumentNullException(nameof(type));

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
			var astType = AstBuilder.ConvertType(type, new StringBuilder(), typeAttributes, options);

			if (type.TryGetByRefSig() != null) {
				output.Write("ByRef", BoxedTextTokenKind.Keyword);
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
			case DecompilationType.TypeMethods:
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
			case DecompilationType.TypeMethods:
				DecompileTypeMethods((DecompileTypeMethods)data);
				return;
			}
			base.Decompile(decompilationType, data);
		}

		void DecompilePartial(DecompilePartialType info) {
			var state = CreateAstBuilder(info.Context, CSharpLanguage.CreateDecompilerSettings(langSettings.Settings, info.UseUsingDeclarations), currentType: info.Type);
			try {
				state.AstBuilder.AddType(info.Type);
				RunTransformsAndGenerateCode(ref state, info.Output, info.Context, new DecompilePartialTransform(info.Type, info.Definitions, info.ShowDefinitions, info.AddPartialKeyword, info.InterfacesToRemove));
			}
			finally {
				state.Dispose();
			}
		}

		void DecompileAssemblyInfo(DecompileAssemblyInfo info) {
			var state = CreateAstBuilder(info.Context, langSettings.Settings, currentModule: info.Module);
			try {
				state.AstBuilder.AddAssembly(info.Module, true, info.Module.IsManifestModule, true);
				RunTransformsAndGenerateCode(ref state, info.Output, info.Context, new AssemblyInfoTransform());
			}
			finally {
				state.Dispose();
			}
		}

		void DecompileTypeMethods(DecompileTypeMethods info) {
			var state = CreateAstBuilder(info.Context, CSharpLanguage.CreateDecompilerSettings(langSettings.Settings, !info.DecompileHidden), currentType: info.Type);
			try {
				state.AstBuilder.GetDecompiledBodyKind = (builder, method) => CSharpLanguage.GetDecompiledBodyKind(info, builder, method);
				state.AstBuilder.AddType(info.Type);
				RunTransformsAndGenerateCode(ref state, info.Output, info.Context, new DecompileTypeMethodsTransform(info.Methods, !info.DecompileHidden, info.MakeEverythingPublic));
			}
			finally {
				state.Dispose();
			}
		}
	}
}
