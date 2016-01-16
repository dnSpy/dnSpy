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
using System.Linq;
using System.Xml;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Languages;
using dnSpy.Decompiler.Shared;
using dnSpy.Languages.ILSpy.Settings;
using dnSpy.Languages.ILSpy.XmlDoc;
using dnSpy.Shared.UI.Highlighting;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.CSharp;

namespace dnSpy.Languages.ILSpy.CSharp {
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
			get {
				yield return new CSharpLanguage(languageSettingsManager.LanguageDecompilerSettings, LanguageConstants.CSHARP_ILSPY_ORDERUI);
#if DEBUG
				foreach (var l in CSharpLanguage.GetDebugLanguages(languageSettingsManager.LanguageDecompilerSettings))
					yield return l;
#endif
			}
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
	/// Decompiler logic for C#.
	/// </summary>
	sealed class CSharpLanguage : Language {
		string uniqueNameUI = "C#";
		Guid uniqueGuid = LanguageConstants.LANGUAGE_CSHARP_ILSPY;
		bool showAllMembers = false;
		Predicate<IAstTransform> transformAbortCondition = null;

		public override IDecompilerSettings Settings {
			get { return langSettings; }
		}
		readonly LanguageDecompilerSettings langSettings;

		public CSharpLanguage(LanguageDecompilerSettings langSettings, double orderUI) {
			this.langSettings = langSettings;
			this.orderUI = orderUI;
		}

#if DEBUG
		internal static IEnumerable<CSharpLanguage> GetDebugLanguages(LanguageDecompilerSettings langSettings) {
			DecompilerContext context = new DecompilerContext(new ModuleDefUser("dummy"));
			string lastTransformName = "no transforms";
			double orderUI = LanguageConstants.CSHARP_ILSPY_DEBUG_ORDERUI;
			uint id = 0xBF67AF3F;
			foreach (Type _transformType in TransformationPipeline.CreatePipeline(context).Select(v => v.GetType()).Distinct()) {
				Type transformType = _transformType; // copy for lambda
				yield return new CSharpLanguage(langSettings, orderUI++) {
					transformAbortCondition = v => transformType.IsInstanceOfType(v),
					uniqueNameUI = "C# - " + lastTransformName,
					uniqueGuid = new Guid(string.Format("203F702E-7E87-4F01-84CD-B0E8{0:X8}", id++)),
					showAllMembers = true
				};
				lastTransformName = "after " + transformType.Name;
			}
			yield return new CSharpLanguage(langSettings, orderUI++) {
				uniqueNameUI = "C# - " + lastTransformName,
				uniqueGuid = new Guid(string.Format("203F702E-7E87-4F01-84CD-B0E8{0:X8}", id++)),
				showAllMembers = true
			};
		}
#endif

		public override string GenericNameUI {
			get { return LanguageConstants.GENERIC_NAMEUI_CSHARP; }
		}

		public override string UniqueNameUI {
			get { return uniqueNameUI; }
		}

		public override double OrderUI {
			get { return orderUI; }
		}
		readonly double orderUI;

		public override Guid GenericGuid {
			get { return LanguageConstants.LANGUAGE_CSHARP; }
		}

		public override Guid UniqueGuid {
			get { return uniqueGuid; }
		}

		public override string FileExtension {
			get { return ".cs"; }
		}

		public override string ProjectFileExtension {
			get { return ".csproj"; }
		}

		public override void Decompile(MethodDef method, ITextOutput output, DecompilationContext ctx) {
			WriteCommentLineDeclaringType(output, method);
			AstBuilder codeDomBuilder = CreateAstBuilder(ctx, langSettings.Settings, currentType: method.DeclaringType, isSingleMember: true);
			if (method.IsConstructor && !method.IsStatic && !method.DeclaringType.IsValueType) {
				// also fields and other ctors so that the field initializers can be shown as such
				AddFieldsAndCtors(codeDomBuilder, method.DeclaringType, method.IsStatic);
				RunTransformsAndGenerateCode(codeDomBuilder, output, ctx, new SelectCtorTransform(method));
			}
			else {
				codeDomBuilder.AddMethod(method);
				RunTransformsAndGenerateCode(codeDomBuilder, output, ctx);
			}
		}

		class SelectCtorTransform : IAstTransform {
			readonly MethodDef ctorDef;

			public SelectCtorTransform(MethodDef ctorDef) {
				this.ctorDef = ctorDef;
			}

			public void Run(AstNode compilationUnit) {
				ConstructorDeclaration ctorDecl = null;
				foreach (var node in compilationUnit.Children) {
					ConstructorDeclaration ctor = node as ConstructorDeclaration;
					if (ctor != null) {
						if (ctor.Annotation<MethodDef>() == ctorDef) {
							ctorDecl = ctor;
						}
						else {
							// remove other ctors
							ctor.Remove();
						}
					}
					// Remove any fields without initializers
					FieldDeclaration fd = node as FieldDeclaration;
					if (fd != null && fd.Variables.All(v => v.Initializer.IsNull))
						fd.Remove();
				}
				if (ctorDecl.Initializer.ConstructorInitializerType == ConstructorInitializerType.This) {
					// remove all fields
					foreach (var node in compilationUnit.Children)
						if (node is FieldDeclaration)
							node.Remove();
				}
			}
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
			if (field.IsLiteral) {
				codeDomBuilder.AddField(field);
			}
			else {
				// also decompile ctors so that the field initializer can be shown
				AddFieldsAndCtors(codeDomBuilder, field.DeclaringType, field.IsStatic);
			}
			RunTransformsAndGenerateCode(codeDomBuilder, output, ctx, new SelectFieldTransform(field));
		}

		/// <summary>
		/// Removes all top-level members except for the specified fields.
		/// </summary>
		sealed class SelectFieldTransform : IAstTransform {
			readonly FieldDef field;

			public SelectFieldTransform(FieldDef field) {
				this.field = field;
			}

			public void Run(AstNode compilationUnit) {
				foreach (var child in compilationUnit.Children) {
					if (child is EntityDeclaration) {
						if (child.Annotation<FieldDef>() != field)
							child.Remove();
					}
				}
			}
		}

		void AddFieldsAndCtors(AstBuilder codeDomBuilder, TypeDef declaringType, bool isStatic) {
			foreach (var field in declaringType.Fields) {
				if (field.IsStatic == isStatic)
					codeDomBuilder.AddField(field);
			}
			foreach (var ctor in declaringType.Methods) {
				if (ctor.IsConstructor && ctor.IsStatic == isStatic)
					codeDomBuilder.AddMethod(ctor);
			}
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

		void RunTransformsAndGenerateCode(AstBuilder astBuilder, ITextOutput output, DecompilationContext ctx, IAstTransform additionalTransform = null) {
			astBuilder.RunTransformations(transformAbortCondition);
			if (additionalTransform != null) {
				additionalTransform.Run(astBuilder.SyntaxTree);
			}
			AddXmlDocumentation(langSettings.Settings, astBuilder);
			astBuilder.GenerateCode(output);
		}

		internal static void AddXmlDocumentation(DecompilerSettings settings, AstBuilder astBuilder) { 
			if (settings.ShowXmlDocumentation) {
				try {
					AddXmlDocTransform.Run(astBuilder.SyntaxTree);
				}
				catch (XmlException ex) {
					string[] msg = (" Exception while reading XmlDoc: " + ex.ToString()).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					var insertionPoint = astBuilder.SyntaxTree.FirstChild;
					for (int i = 0; i < msg.Length; i++)
						astBuilder.SyntaxTree.InsertChildBefore(insertionPoint, new Comment(msg[i], CommentType.Documentation), Roles.Comment);
				}
			}
		}

		public override void DecompileAssembly(IDnSpyFile file, ITextOutput output, DecompilationContext ctx, DecompileAssemblyFlags flags = DecompileAssemblyFlags.AssemblyAndModule) {
			WriteModuleAssembly(file, output, ctx, flags);

			bool decompileAsm = (flags & DecompileAssemblyFlags.Assembly) != 0;
			bool decompileMod = (flags & DecompileAssemblyFlags.Module) != 0;
			using (ctx.DisableAssemblyLoad()) {
				AstBuilder codeDomBuilder = CreateAstBuilder(ctx, langSettings.Settings, currentModule: file.ModuleDef);
				codeDomBuilder.AddAssembly(file.ModuleDef, true, decompileAsm, decompileMod);
				RunTransformsAndGenerateCode(codeDomBuilder, output, ctx);
			}
		}

		AstBuilder CreateAstBuilder(DecompilationContext ctx, DecompilerSettings settings, ModuleDef currentModule = null, TypeDef currentType = null, bool isSingleMember = false) {
			if (currentModule == null)
				currentModule = currentType.Module;
			if (isSingleMember) {
				settings = settings.Clone();
				settings.UsingDeclarations = false;
			}
			return new AstBuilder(
				new DecompilerContext(currentModule) {
					CancellationToken = ctx.CancellationToken,
					CurrentType = currentType,
					Settings = settings
				}) {
			};
		}

		protected override void TypeToString(ITextOutput output, ITypeDefOrRef type, bool includeNamespace, IHasCustomAttribute typeAttributes = null) {
			ConvertTypeOptions options = ConvertTypeOptions.IncludeTypeParameterDefinitions;
			if (includeNamespace)
				options |= ConvertTypeOptions.IncludeNamespace;

			TypeToString(output, options, type, typeAttributes);
		}

		bool WriteRefIfByRef(ITextOutput output, TypeSig typeSig, ParamDef pd) {
			if (typeSig.RemovePinnedAndModifiers() is ByRefSig) {
				if (pd != null && (!pd.IsIn && pd.IsOut)) {
					output.Write("out", TextTokenKind.Keyword);
					output.WriteSpace();
				}
				else {
					output.Write("ref", TextTokenKind.Keyword);
					output.WriteSpace();
				}
				return true;
			}
			return false;
		}

		void TypeToString(ITextOutput output, ConvertTypeOptions options, ITypeDefOrRef type, IHasCustomAttribute typeAttributes = null) {
			if (type == null)
				return;
			AstType astType = AstBuilder.ConvertType(type, typeAttributes, options);

			if (WriteRefIfByRef(output, type.TryGetByRefSig(), typeAttributes as ParamDef)) {
				if (astType is ComposedType && ((ComposedType)astType).PointerRank > 0)
					((ComposedType)astType).PointerRank--;
			}

			var module = type.Module;
			if (module == null && type is TypeSpec && ((TypeSpec)type).TypeSig.RemovePinnedAndModifiers() is GenericSig) {
				var sig = (GenericSig)((TypeSpec)type).TypeSig.RemovePinnedAndModifiers();
				if (sig.OwnerType != null)
					module = sig.OwnerType.Module;
				if (module == null && sig.OwnerMethod != null && sig.OwnerMethod.DeclaringType != null)
					module = sig.OwnerMethod.DeclaringType.Module;
			}
			var ctx = new DecompilerContext(type.Module);
			astType.AcceptVisitor(new CSharpOutputVisitor(new TextTokenWriter(output, ctx), FormattingOptionsFactory.CreateAllman()));
		}

		protected override void FormatPropertyName(ITextOutput output, PropertyDef property, bool? isIndexer) {
			if (property == null)
				throw new ArgumentNullException("property");

			if (!isIndexer.HasValue) {
				isIndexer = property.IsIndexer();
			}
			if (isIndexer.Value) {
				var accessor = property.GetMethod ?? property.SetMethod;
				if (accessor != null && accessor.HasOverrides) {
					var methDecl = accessor.Overrides.First().MethodDeclaration;
					var declaringType = methDecl == null ? null : methDecl.DeclaringType;
					TypeToString(output, declaringType, includeNamespace: true);
					output.Write(".", TextTokenKind.Operator);
				}
				output.Write("this", TextTokenKind.Keyword);
				output.Write("[", TextTokenKind.Operator);
				bool addSeparator = false;
				foreach (var p in property.PropertySig.GetParams()) {
					if (addSeparator) {
						output.Write(",", TextTokenKind.Operator);
						output.WriteSpace();
					}
					else
						addSeparator = true;
					TypeToString(output, p.ToTypeDefOrRef(), includeNamespace: true);
				}
				output.Write("]", TextTokenKind.Operator);
			}
			else
				WriteIdentifier(output, property.Name, TextTokenKindUtils.GetTextTokenType(property));
		}

		static readonly HashSet<string> isKeyword = new HashSet<string>(StringComparer.Ordinal) {
			"abstract", "as", "base", "bool", "break", "byte", "case", "catch",
			"char", "checked", "class", "const", "continue", "decimal", "default", "delegate",
			"do", "double", "else", "enum", "event", "explicit", "extern", "false",
			"finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
			"in", "int", "interface", "internal", "is", "lock", "long", "namespace",
			"new", "null", "object", "operator", "out", "override", "params", "private",
			"protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
			"sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
			"true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
			"using", "virtual", "void", "volatile", "while",
		};

		static void WriteIdentifier(ITextOutput output, string id, TextTokenKind tokenKind) {
			if (isKeyword.Contains(id))
				output.Write("@", TextTokenKind.Operator);
			output.Write(IdentifierEscaper.Escape(id), tokenKind);
		}

		protected override void FormatTypeName(ITextOutput output, TypeDef type) {
			if (type == null)
				throw new ArgumentNullException("type");

			TypeToString(output, ConvertTypeOptions.DoNotUsePrimitiveTypeNames | ConvertTypeOptions.IncludeTypeParameterDefinitions | ConvertTypeOptions.DoNotIncludeEnclosingType, type);
		}

		public override bool ShowMember(IMemberRef member) {
			return showAllMembers || !AstBuilder.MemberIsHidden(member, langSettings.Settings);
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
			var builder = CreateAstBuilder(info.Context, CreateDecompilerSettings(langSettings.Settings, info.UseUsingDeclarations), currentType: info.Type);
			builder.AddType(info.Type);
			RunTransformsAndGenerateCode(builder, info.Output, info.Context, new DecompilePartialTransform(info.Type, info.Definitions, info.ShowDefinitions, info.AddPartialKeyword, info.InterfacesToRemove));
		}

		internal static DecompilerSettings CreateDecompilerSettings(DecompilerSettings settings, bool useUsingDeclarations) {
			var newOne = settings.Clone();
			newOne.UsingDeclarations = useUsingDeclarations;
			newOne.FullyQualifyAllTypes = !useUsingDeclarations;
			return newOne;
		}

		void DecompileAssemblyInfo(DecompileAssemblyInfo info) {
			var builder = CreateAstBuilder(info.Context, langSettings.Settings, currentModule: info.Module);
			builder.AddAssembly(info.Module, true, info.Module.IsManifestModule, true);
			RunTransformsAndGenerateCode(builder, info.Output, info.Context, new AssemblyInfoTransform());
		}
	}
}
