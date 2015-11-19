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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Xml;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Languages;
using dnSpy.Decompiler;
using dnSpy.Languages.CSharp;
using dnSpy.Languages.XmlDoc;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.Highlighting;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Visitors;

namespace dnSpy.Languages.VB {
	/// <summary>
	/// Decompiler logic for VB.
	/// </summary>
	[Export(typeof(ILanguage))]
	sealed class VBLanguage : Language {
		readonly Predicate<IAstTransform> transformAbortCondition = null;
		bool showAllMembers = false;

		public override double OrderUI {
			get { return LanguageConstants.VB_ORDERUI; }
		}

		public VBLanguage() {
		}

		public override string NameUI {
			get { return "VB"; }
		}

		public override string FileExtension {
			get { return ".vb"; }
		}

		public override string ProjectFileExtension {
			get { return ".vbproj"; }
		}

		public override void WriteCommentBegin(ITextOutput output, bool addSpace) {
			if (addSpace)
				output.WriteLine("' ", TextTokenType.Comment);
			else
				output.WriteLine("'", TextTokenType.Comment);
		}

		public override void WriteCommentEnd(ITextOutput output, bool addSpace) {
		}

		public override void DecompileAssembly(IDnSpyFile file, ITextOutput output, DecompilationOptions options, DecompileAssemblyFlags flags = DecompileAssemblyFlags.AssemblyAndModule) {
			if (options.ProjectOptions.Directory != null) {
				HashSet<string> directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				var files = WriteCodeFilesInProject(file.ModuleDef, options, directories).ToList();
				files.AddRange(WriteResourceFilesInProject(file, options, directories));
				WriteProjectFile(new TextOutputWriter(output), files, file, options);
			}
			else {
				bool decompileAsm = (flags & DecompileAssemblyFlags.Assembly) != 0;
				bool decompileMod = (flags & DecompileAssemblyFlags.Module) != 0;
				base.DecompileAssembly(file, output, options, flags);
				output.WriteLine();
				ModuleDef mainModule = file.ModuleDef;
				if (decompileMod && mainModule.Types.Count > 0) {
					output.Write("' Global type: ", TextTokenType.Comment);
					output.WriteReference(IdentifierEscaper.Escape(mainModule.GlobalType.FullName), mainModule.GlobalType, TextTokenType.Comment);
					output.WriteLine();
				}
				if (decompileMod || decompileAsm)
					PrintEntryPoint(file, output);
				if (decompileMod) {
					this.WriteCommentLine(output, "Architecture: " + CSharpLanguage.GetPlatformDisplayName(mainModule));
					if (!mainModule.IsILOnly) {
						this.WriteCommentLine(output, "This assembly contains unmanaged code.");
					}
					string runtimeName = CSharpLanguage.GetRuntimeDisplayName(mainModule);
					if (runtimeName != null) {
						this.WriteCommentLine(output, "Runtime: " + runtimeName);
					}
				}
				if (decompileMod || decompileAsm)
					output.WriteLine();

				AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: file.ModuleDef);
				codeDomBuilder.AddAssembly(file.ModuleDef, true, decompileAsm, decompileMod);
				RunTransformsAndGenerateCode(codeDomBuilder, output, options, file.ModuleDef);
			}
		}

		static readonly string[] projectImports = new[] {
			"System.Diagnostics",
			"Microsoft.VisualBasic",
			"System",
			"System.Collections",
			"System.Collections.Generic"
		};

		#region WriteProjectFile
		void WriteProjectFile(TextWriter writer, IEnumerable<Tuple<string, string>> files, IDnSpyFile assembly, DecompilationOptions options) {
			var module = assembly.ModuleDef;
			const string ns = "http://schemas.microsoft.com/developer/msbuild/2003";
			string platformName = CSharpLanguage.GetPlatformName(module);
			var guid = Guid.NewGuid();
			using (XmlTextWriter w = new XmlTextWriter(writer)) {
				var asmRefs = CSharpLanguage.GetAssemblyRefs(options, assembly);

				w.Formatting = Formatting.Indented;
				w.WriteStartDocument();
				w.WriteStartElement("Project", ns);
				w.WriteAttributeString("ToolsVersion", "4.0");
				w.WriteAttributeString("DefaultTargets", "Build");

				w.WriteStartElement("PropertyGroup");
				w.WriteElementString("ProjectGuid", (options.ProjectOptions.ProjectGuid ?? guid).ToString("B").ToUpperInvariant());

				w.WriteStartElement("Configuration");
				w.WriteAttributeString("Condition", " '$(Configuration)' == '' ");
				w.WriteValue("Debug");
				w.WriteEndElement(); // </Configuration>

				w.WriteStartElement("Platform");
				w.WriteAttributeString("Condition", " '$(Platform)' == '' ");
				w.WriteValue(platformName);
				w.WriteEndElement(); // </Platform>

				switch (module.Kind) {
				case ModuleKind.Windows:
					w.WriteElementString("OutputType", "WinExe");
					break;
				case ModuleKind.Console:
					w.WriteElementString("OutputType", "Exe");
					break;
				default:
					w.WriteElementString("OutputType", "Library");
					break;
				}

				if (module.Assembly != null)
					w.WriteElementString("AssemblyName", IdentifierEscaper.Escape(module.Assembly.Name));
				bool useTargetFrameworkAttribute = false;
				var targetFrameworkAttribute = module.Assembly == null ? null : module.Assembly.CustomAttributes.FirstOrDefault(a => a.TypeFullName == "System.Runtime.Versioning.TargetFrameworkAttribute");
				if (targetFrameworkAttribute != null && targetFrameworkAttribute.ConstructorArguments.Any()) {
					string frameworkName = (targetFrameworkAttribute.ConstructorArguments[0].Value as UTF8String) ?? string.Empty;
					string[] frameworkParts = frameworkName.Split(',');
					string frameworkVersion = frameworkParts.FirstOrDefault(a => a.StartsWith("Version="));
					if (frameworkVersion != null) {
						w.WriteElementString("TargetFrameworkVersion", frameworkVersion.Substring("Version=".Length));
						useTargetFrameworkAttribute = true;
					}
					string frameworkProfile = frameworkParts.FirstOrDefault(a => a.StartsWith("Profile="));
					if (frameworkProfile != null)
						w.WriteElementString("TargetFrameworkProfile", frameworkProfile.Substring("Profile=".Length));
				}
				if (!useTargetFrameworkAttribute) {
					if (module.IsClr10) {
						w.WriteElementString("TargetFrameworkVersion", "v1.0");
					}
					else if (module.IsClr11) {
						w.WriteElementString("TargetFrameworkVersion", "v1.1");
					}
					else if (module.IsClr20) {
						w.WriteElementString("TargetFrameworkVersion", "v2.0");
						// TODO: Detect when .NET 3.0/3.5 is required
					}
					else {
						w.WriteElementString("TargetFrameworkVersion", "v4.0");
					}
				}
				w.WriteElementString("WarningLevel", "4");

				w.WriteEndElement(); // </PropertyGroup>

				w.WriteStartElement("PropertyGroup"); // platform-specific
				w.WriteAttributeString("Condition", " '$(Platform)' == '" + platformName + "' ");
				w.WriteElementString("PlatformTarget", platformName);
				w.WriteEndElement(); // </PropertyGroup> (platform-specific)

				w.WriteStartElement("PropertyGroup"); // Debug
				w.WriteAttributeString("Condition", " '$(Configuration)' == 'Debug' ");
				w.WriteElementString("OutputPath", "bin\\Debug\\");
				w.WriteElementString("DebugSymbols", "true");
				w.WriteElementString("DebugType", "full");
				w.WriteElementString("Optimize", "false");
				if (options.ProjectOptions.DontReferenceStdLib) {
					w.WriteStartElement("NoStdLib");
					w.WriteString("true");
					w.WriteEndElement();
				}
				w.WriteEndElement(); // </PropertyGroup> (Debug)

				w.WriteStartElement("PropertyGroup"); // Release
				w.WriteAttributeString("Condition", " '$(Configuration)' == 'Release' ");
				w.WriteElementString("OutputPath", "bin\\Release\\");
				w.WriteElementString("DebugSymbols", "true");
				w.WriteElementString("DebugType", "pdbonly");
				w.WriteElementString("Optimize", "true");
				if (options.ProjectOptions.DontReferenceStdLib) {
					w.WriteStartElement("NoStdLib");
					w.WriteString("true");
					w.WriteEndElement();
				}
				w.WriteEndElement(); // </PropertyGroup> (Release)


				w.WriteStartElement("ItemGroup"); // References
				foreach (var r in asmRefs) {
					if (r.Name != "mscorlib") {
						var asm = options.ProjectOptions.Resolve(r, module);
						if (asm != null && CSharpLanguage.ExistsInProject(options, asm.ManifestModule.Location))
							continue;
						w.WriteStartElement("Reference");
						w.WriteAttributeString("Include", IdentifierEscaper.Escape(r.Name));
						var hintPath = CSharpLanguage.GetHintPath(options, asm);
						if (hintPath != null) {
							w.WriteStartElement("HintPath");
							w.WriteString(hintPath);
							w.WriteEndElement();
						}
						w.WriteEndElement();
					}
				}
				w.WriteEndElement(); // </ItemGroup> (References)

				foreach (IGrouping<string, string> gr in (from f in files group f.Item2 by f.Item1 into g orderby g.Key select g)) {
					w.WriteStartElement("ItemGroup");
					foreach (string file in gr.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)) {
						w.WriteStartElement(gr.Key);
						w.WriteAttributeString("Include", file);
						w.WriteEndElement();
					}
					w.WriteEndElement();
				}

				w.WriteStartElement("ItemGroup"); // ProjectReference
				foreach (var r in asmRefs) {
					var asm = options.ProjectOptions.AssemblyResolver.Resolve(r, module);
					if (asm == null)
						continue;
					var otherProj = CSharpLanguage.FindOtherProject(options, asm.ManifestModule.Location);
					if (otherProj != null) {
						var relPath = CSharpLanguage.GetRelativePath(options.ProjectOptions.Directory, otherProj.ProjectFileName);
						w.WriteStartElement("ProjectReference");
						w.WriteAttributeString("Include", relPath);
						w.WriteStartElement("Project");
						w.WriteString(otherProj.ProjectGuid.ToString("B").ToUpperInvariant());
						w.WriteEndElement();
						w.WriteStartElement("Name");
						w.WriteString(IdentifierEscaper.Escape(otherProj.AssemblySimpleName));
						w.WriteEndElement();
						w.WriteEndElement();
					}
				}
				w.WriteEndElement(); // </ItemGroup> (ProjectReference)

				w.WriteStartElement("ItemGroup"); // Imports
				foreach (var import in projectImports.OrderBy(x => x)) {
					w.WriteStartElement("Import");
					w.WriteAttributeString("Include", import);
					w.WriteEndElement();
				}
				w.WriteEndElement(); // </ItemGroup> (Imports)

				w.WriteStartElement("Import");
				w.WriteAttributeString("Project", "$(MSBuildToolsPath)\\Microsoft.VisualBasic.targets");
				w.WriteEndElement();

				w.WriteEndDocument();
			}
		}
		#endregion

		#region WriteCodeFilesInProject
		bool IncludeTypeWhenDecompilingProject(TypeDef type, DecompilationOptions options) {
			if (type.IsGlobalModuleType || AstBuilder.MemberIsHidden(type, options.DecompilerSettings))
				return false;
			if (type.Namespace == "XamlGeneratedNamespace" && type.Name == "GeneratedInternalTypeHelper")
				return false;
			return true;
		}

		IEnumerable<Tuple<string, string>> WriteAssemblyInfo(ModuleDef module, DecompilationOptions options, HashSet<string> directories) {
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: module);
			codeDomBuilder.AddAssembly(module, true, true, true);
			codeDomBuilder.RunTransformations(transformAbortCondition);

			string prop = "Properties";
			if (directories.Add("Properties"))
				Directory.CreateDirectory(Path.Combine(options.ProjectOptions.Directory, prop));
			string assemblyInfo = Path.Combine(prop, "AssemblyInfo" + this.FileExtension);
			using (StreamWriter w = new StreamWriter(Path.Combine(options.ProjectOptions.Directory, assemblyInfo)))
				codeDomBuilder.GenerateCode(new PlainTextOutput(w));
			return new Tuple<string, string>[] { Tuple.Create("Compile", assemblyInfo) };
		}

		IEnumerable<Tuple<string, string>> WriteCodeFilesInProject(ModuleDef module, DecompilationOptions options, HashSet<string> directories) {
			var files = module.Types.Where(t => IncludeTypeWhenDecompilingProject(t, options)).GroupBy(
				delegate (TypeDef type) {
					string file = CSharpLanguage.CleanUpName(type.Name) + this.FileExtension;
					if (string.IsNullOrEmpty(type.Namespace)) {
						return file;
					}
					else {
						string dir = CSharpLanguage.CleanUpName(type.Namespace);
						if (directories.Add(dir))
							Directory.CreateDirectory(Path.Combine(options.ProjectOptions.Directory, dir));
						return Path.Combine(dir, file);
					}
				}, StringComparer.OrdinalIgnoreCase).ToList();
			AstMethodBodyBuilder.ClearUnhandledOpcodes();
			Parallel.ForEach(
				files,
				new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
				delegate (IGrouping<string, TypeDef> file) {
					using (StreamWriter w = new StreamWriter(Path.Combine(options.ProjectOptions.Directory, file.Key))) {
						AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: module);
						foreach (TypeDef type in file) {
							codeDomBuilder.AddType(type);
						}
						RunTransformsAndGenerateCode(codeDomBuilder, new PlainTextOutput(w), options, module);
					}
				});
			AstMethodBodyBuilder.PrintNumberOfUnhandledOpcodes();
			return files.Select(f => Tuple.Create("Compile", f.Key)).Concat(WriteAssemblyInfo(module, options, directories));
		}
		#endregion

		#region WriteResourceFilesInProject
		IEnumerable<Tuple<string, string>> WriteResourceFilesInProject(IDnSpyFile assembly, DecompilationOptions options, HashSet<string> directories) {
			foreach (EmbeddedResource r in assembly.ModuleDef.Resources.OfType<EmbeddedResource>()) {
				string fileName;
				Stream s = r.GetResourceStream();
				s.Position = 0;
				if (r.Name.EndsWith(".g.resources", StringComparison.OrdinalIgnoreCase)) {
					IEnumerable<DictionaryEntry> rs = null;
					try {
						rs = new ResourceSet(s).Cast<DictionaryEntry>();
					}
					catch (ArgumentException) {
					}
					if (rs != null && rs.All(e => e.Value is Stream)) {
						foreach (var pair in rs) {
							fileName = Path.Combine(((string)pair.Key).Split('/').Select(p => CSharpLanguage.CleanUpName(p)).ToArray());
							string dirName = Path.GetDirectoryName(fileName);
							if (!string.IsNullOrEmpty(dirName) && directories.Add(dirName)) {
								Directory.CreateDirectory(Path.Combine(options.ProjectOptions.Directory, dirName));
							}
							Stream entryStream = (Stream)pair.Value;
							entryStream.Position = 0;
							using (FileStream fs = new FileStream(Path.Combine(options.ProjectOptions.Directory, fileName), FileMode.Create, FileAccess.Write)) {
								entryStream.CopyTo(fs);
							}
							yield return Tuple.Create("Resource", fileName);
						}
						continue;
					}
				}
				fileName = GetFileNameForResource(r.Name, directories);
				using (FileStream fs = new FileStream(Path.Combine(options.ProjectOptions.Directory, fileName), FileMode.Create, FileAccess.Write)) {
					s.CopyTo(fs);
				}
				yield return Tuple.Create("EmbeddedResource", fileName);
			}
		}

		string GetFileNameForResource(string fullName, HashSet<string> directories) {
			string[] splitName = fullName.Split('.');
			string fileName = CSharpLanguage.CleanUpName(fullName);
			for (int i = splitName.Length - 1; i > 0; i--) {
				string ns = string.Join(".", splitName, 0, i);
				if (directories.Contains(ns)) {
					string name = string.Join(".", splitName, i, splitName.Length - i);
					fileName = Path.Combine(ns, CSharpLanguage.CleanUpName(name));
					break;
				}
			}
			return fileName;
		}
		#endregion

		public override void Decompile(MethodDef method, ITextOutput output, DecompilationOptions options) {
			WriteCommentLineDeclaringType(output, method);
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: method.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddMethod(method);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, method.Module);
		}

		public override void Decompile(PropertyDef property, ITextOutput output, DecompilationOptions options) {
			WriteCommentLineDeclaringType(output, property);
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: property.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddProperty(property);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, property.Module);
		}

		public override void Decompile(FieldDef field, ITextOutput output, DecompilationOptions options) {
			WriteCommentLineDeclaringType(output, field);
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: field.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddField(field);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, field.Module);
		}

		public override void Decompile(EventDef ev, ITextOutput output, DecompilationOptions options) {
			WriteCommentLineDeclaringType(output, ev);
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: ev.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddEvent(ev);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, ev.Module);
		}

		public override void Decompile(TypeDef type, ITextOutput output, DecompilationOptions options) {
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: type);
			codeDomBuilder.AddType(type);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, type.Module);
		}

		public override bool ShowMember(IMemberRef member) {
			return showAllMembers || !AstBuilder.MemberIsHidden(member, new DecompilationOptions().DecompilerSettings);
		}

		void RunTransformsAndGenerateCode(AstBuilder astBuilder, ITextOutput output, DecompilationOptions options, ModuleDef module) {
			astBuilder.RunTransformations(transformAbortCondition);
			if (options.DecompilerSettings.ShowXmlDocumentation) {
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
			var csharpUnit = astBuilder.SyntaxTree;
			csharpUnit.AcceptVisitor(new ICSharpCode.NRefactory.CSharp.InsertParenthesesVisitor() { InsertParenthesesForReadability = true });
			var unit = csharpUnit.AcceptVisitor(new CSharpToVBConverterVisitor(new ILSpyEnvironmentProvider()), null);
			var outputFormatter = new VBTextOutputFormatter(output);
			var formattingPolicy = new VBFormattingOptions();
			unit.AcceptVisitor(new OutputVisitor(outputFormatter, formattingPolicy), null);
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

		public override void WriteToolTip(ISyntaxHighlightOutput output, dnlib.DotNet.IVariable variable, string name) {
			output.Write(variable is Local ? "(local variable)" : "(parameter)", TextTokenType.Text);
			output.WriteSpace();
			output.Write(IdentifierEscaper.Escape(GetName(variable, name)), variable is Local ? TextTokenType.Local : TextTokenType.Parameter);
			output.WriteSpace();
			output.Write("As", TextTokenType.Keyword);
			output.WriteSpace();
			WriteToolTip(output, variable.Type.ToTypeDefOrRef(), variable is Parameter ? ((Parameter)variable).ParamDef : null);
		}
	}
}
