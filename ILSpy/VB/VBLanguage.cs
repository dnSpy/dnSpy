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

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.ILSpy.XmlDoc;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using CSharp = ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Visitors;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.VB
{
	/// <summary>
	/// Decompiler logic for VB.
	/// </summary>
	[Export(typeof(Language))]
	public class VBLanguage : Language
	{
		readonly Predicate<IAstTransform> transformAbortCondition = null;
		bool showAllMembers = false;
		
		public VBLanguage()
		{
		}
		
		public override string Name {
			get { return "VB"; }
		}
		
		public override string FileExtension {
			get { return ".vb"; }
		}
		
		public override string ProjectFileExtension {
			get { return ".vbproj"; }
		}
		
		public override void WriteCommentLine(ITextOutput output, string comment)
		{
			output.WriteLine("' " + comment, TextTokenType.Comment);
		}
		
		public override void WriteComment(ITextOutput output, string comment)
		{
			output.Write("' " + comment, TextTokenType.Comment);
		}
		
		public override void DecompileAssembly(LoadedAssembly assembly, ITextOutput output, DecompilationOptions options, DecompileAssemblyFlags flags = DecompileAssemblyFlags.AssemblyAndModule)
		{
			if (options.FullDecompilation && options.SaveAsProjectDirectory != null) {
				HashSet<string> directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				var files = WriteCodeFilesInProject(assembly.ModuleDefinition, options, directories).ToList();
				files.AddRange(WriteResourceFilesInProject(assembly, options, directories));
				WriteProjectFile(new TextOutputWriter(output), files, assembly, options);
			} else {
				bool decompileAsm = (flags & DecompileAssemblyFlags.Assembly) != 0;
				bool decompileMod = (flags & DecompileAssemblyFlags.Module) != 0;
				base.DecompileAssembly(assembly, output, options, flags);
				output.WriteLine();
				if (decompileMod || decompileAsm)
					PrintEntryPoint(assembly, output);
				if (decompileMod) {
					ModuleDef mainModule = assembly.ModuleDefinition;
					WriteCommentLine(output, "Architecture: " + CSharpLanguage.GetPlatformDisplayName(mainModule));
					if (!mainModule.IsILOnly) {
						WriteCommentLine(output, "This assembly contains unmanaged code.");
					}
					string runtimeName = ICSharpCode.ILSpy.CSharpLanguage.GetRuntimeDisplayName(mainModule);
					if (runtimeName != null) {
						WriteCommentLine(output, "Runtime: " + runtimeName);
					}
				}
				if (decompileMod || decompileAsm)
					output.WriteLine();
				
				// don't automatically load additional assemblies when an assembly node is selected in the tree view
				using (options.FullDecompilation ? null : LoadedAssembly.DisableAssemblyLoad()) {
					AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: assembly.ModuleDefinition);
					codeDomBuilder.AddAssembly(assembly.ModuleDefinition, !options.FullDecompilation, decompileAsm, decompileMod);
					RunTransformsAndGenerateCode(codeDomBuilder, output, options, assembly.ModuleDefinition);
				}
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
		void WriteProjectFile(TextWriter writer, IEnumerable<Tuple<string, string>> files, LoadedAssembly assembly, DecompilationOptions options)
		{
			var module = assembly.ModuleDefinition;
			const string ns = "http://schemas.microsoft.com/developer/msbuild/2003";
			string platformName = CSharpLanguage.GetPlatformName(module);
			Guid guid = App.CommandLineArguments.FixedGuid ?? Guid.NewGuid();
			using (XmlTextWriter w = new XmlTextWriter(writer)) {
				var asmRefs = CSharpLanguage.GetAssemblyRefs(options, assembly);

				w.Formatting = Formatting.Indented;
				w.WriteStartDocument();
				w.WriteStartElement("Project", ns);
				w.WriteAttributeString("ToolsVersion", "4.0");
				w.WriteAttributeString("DefaultTargets", "Build");

				w.WriteStartElement("PropertyGroup");
				w.WriteElementString("ProjectGuid", (options.ProjectGuid ?? guid).ToString("B").ToUpperInvariant());

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
					w.WriteElementString("AssemblyName", module.Assembly.Name);
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
					} else if (module.IsClr11) {
						w.WriteElementString("TargetFrameworkVersion", "v1.1");
					} else if (module.IsClr20) {
						w.WriteElementString("TargetFrameworkVersion", "v2.0");
						// TODO: Detect when .NET 3.0/3.5 is required
					} else {
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
				if (options.DontReferenceStdLib) {
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
				if (options.DontReferenceStdLib) {
					w.WriteStartElement("NoStdLib");
					w.WriteString("true");
					w.WriteEndElement();
				}
				w.WriteEndElement(); // </PropertyGroup> (Release)


				w.WriteStartElement("ItemGroup"); // References
				foreach (var r in asmRefs) {
					if (r.Name != "mscorlib") {
						var asm = assembly.LookupReferencedAssembly(r, module);
						if (asm != null && CSharpLanguage.ExistsInProject(options, asm.FileName))
							continue;
						w.WriteStartElement("Reference");
						w.WriteAttributeString("Include", r.Name);
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
					var asm = assembly.LookupReferencedAssembly(r, module);
					if (asm == null)
						continue;
					var otherProj = CSharpLanguage.FindOtherProject(options, asm.FileName);
					if (otherProj != null) {
						var relPath = CSharpLanguage.GetRelativePath(options.SaveAsProjectDirectory, otherProj.ProjectFileName);
						w.WriteStartElement("ProjectReference");
						w.WriteAttributeString("Include", relPath);
						w.WriteStartElement("Project");
						w.WriteString(otherProj.ProjectGuid.ToString("B").ToUpperInvariant());
						w.WriteEndElement();
						w.WriteStartElement("Name");
						w.WriteString(otherProj.AssemblySimpleName);
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
		bool IncludeTypeWhenDecompilingProject(TypeDef type, DecompilationOptions options)
		{
			if (type.IsGlobalModuleType || AstBuilder.MemberIsHidden(type, options.DecompilerSettings))
				return false;
			if (type.Namespace == "XamlGeneratedNamespace" && type.Name == "GeneratedInternalTypeHelper")
				return false;
			return true;
		}

		IEnumerable<Tuple<string, string>> WriteAssemblyInfo(ModuleDef module, DecompilationOptions options, HashSet<string> directories)
		{
			// don't automatically load additional assemblies when an assembly node is selected in the tree view
			using (LoadedAssembly.DisableAssemblyLoad())
			{
				AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: module);
				codeDomBuilder.AddAssembly(module, true, true, true);
				codeDomBuilder.RunTransformations(transformAbortCondition);

				string prop = "Properties";
				if (directories.Add("Properties"))
					Directory.CreateDirectory(Path.Combine(options.SaveAsProjectDirectory, prop));
				string assemblyInfo = Path.Combine(prop, "AssemblyInfo" + this.FileExtension);
				using (StreamWriter w = new StreamWriter(Path.Combine(options.SaveAsProjectDirectory, assemblyInfo)))
					codeDomBuilder.GenerateCode(new PlainTextOutput(w));
				return new Tuple<string, string>[] { Tuple.Create("Compile", assemblyInfo) };
			}
		}

		IEnumerable<Tuple<string, string>> WriteCodeFilesInProject(ModuleDef module, DecompilationOptions options, HashSet<string> directories)
		{
			var files = module.Types.Where(t => IncludeTypeWhenDecompilingProject(t, options)).GroupBy(
				delegate(TypeDef type) {
					string file = TextView.DecompilerTextView.CleanUpName(type.Name) + this.FileExtension;
					if (string.IsNullOrEmpty(type.Namespace)) {
						return file;
					} else {
						string dir = TextView.DecompilerTextView.CleanUpName(type.Namespace);
						if (directories.Add(dir))
							Directory.CreateDirectory(Path.Combine(options.SaveAsProjectDirectory, dir));
						return Path.Combine(dir, file);
					}
				}, StringComparer.OrdinalIgnoreCase).ToList();
			AstMethodBodyBuilder.ClearUnhandledOpcodes();
			Parallel.ForEach(
				files,
				new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
				delegate(IGrouping<string, TypeDef> file) {
					using (StreamWriter w = new StreamWriter(Path.Combine(options.SaveAsProjectDirectory, file.Key))) {
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
		IEnumerable<Tuple<string, string>> WriteResourceFilesInProject(LoadedAssembly assembly, DecompilationOptions options, HashSet<string> directories)
		{
			//AppDomain bamlDecompilerAppDomain = null;
			//try {
				foreach (EmbeddedResource r in assembly.ModuleDefinition.Resources.OfType<EmbeddedResource>()) {
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
								fileName = Path.Combine(((string)pair.Key).Split('/').Select(p => TextView.DecompilerTextView.CleanUpName(p)).ToArray());
								string dirName = Path.GetDirectoryName(fileName);
								if (!string.IsNullOrEmpty(dirName) && directories.Add(dirName)) {
									Directory.CreateDirectory(Path.Combine(options.SaveAsProjectDirectory, dirName));
								}
								Stream entryStream = (Stream)pair.Value;
								entryStream.Position = 0;
								if (fileName.EndsWith(".baml", StringComparison.OrdinalIgnoreCase)) {
									//MemoryStream ms = new MemoryStream();
									//entryStream.CopyTo(ms);
									// TODO implement extension point
//									var decompiler = Baml.BamlResourceEntryNode.CreateBamlDecompilerInAppDomain(ref bamlDecompilerAppDomain, assembly.FileName);
//									string xaml = null;
//									try {
//										xaml = decompiler.DecompileBaml(ms, assembly.FileName, new ConnectMethodDecompiler(assembly), new AssemblyResolver(assembly));
//									}
//									catch (XamlXmlWriterException) { } // ignore XAML writer exceptions
//									if (xaml != null) {
//										File.WriteAllText(Path.Combine(options.SaveAsProjectDirectory, Path.ChangeExtension(fileName, ".xaml")), xaml);
//										yield return Tuple.Create("Page", Path.ChangeExtension(fileName, ".xaml"));
//										continue;
//									}
								}
								using (FileStream fs = new FileStream(Path.Combine(options.SaveAsProjectDirectory, fileName), FileMode.Create, FileAccess.Write)) {
									entryStream.CopyTo(fs);
								}
								yield return Tuple.Create("Resource", fileName);
							}
							continue;
						}
					}
					fileName = GetFileNameForResource(r.Name, directories);
					using (FileStream fs = new FileStream(Path.Combine(options.SaveAsProjectDirectory, fileName), FileMode.Create, FileAccess.Write)) {
						s.CopyTo(fs);
					}
					yield return Tuple.Create("EmbeddedResource", fileName);
				}
			//}
			//finally {
			//    if (bamlDecompilerAppDomain != null)
			//        AppDomain.Unload(bamlDecompilerAppDomain);
			//}
		}

		string GetFileNameForResource(string fullName, HashSet<string> directories)
		{
			string[] splitName = fullName.Split('.');
			string fileName = TextView.DecompilerTextView.CleanUpName(fullName);
			for (int i = splitName.Length - 1; i > 0; i--) {
				string ns = string.Join(".", splitName, 0, i);
				if (directories.Contains(ns)) {
					string name = string.Join(".", splitName, i, splitName.Length - i);
					fileName = Path.Combine(ns, TextView.DecompilerTextView.CleanUpName(name));
					break;
				}
			}
			return fileName;
		}
		#endregion
		
		public override void DecompileMethod(MethodDef method, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(method.DeclaringType, includeNamespace: true));
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: method.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddMethod(method);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, method.Module);
		}
		
		public override void DecompileProperty(PropertyDef property, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(property.DeclaringType, includeNamespace: true));
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: property.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddProperty(property);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, property.Module);
		}
		
		public override void DecompileField(FieldDef field, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(field.DeclaringType, includeNamespace: true));
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: field.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddField(field);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, field.Module);
		}
		
		public override void DecompileEvent(EventDef ev, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(ev.DeclaringType, includeNamespace: true));
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: ev.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddEvent(ev);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, ev.Module);
		}
		
		public override void DecompileType(TypeDef type, ITextOutput output, DecompilationOptions options)
		{
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: type);
			codeDomBuilder.AddType(type);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, type.Module);
		}
		
		public override bool ShowMember(IMemberRef member)
		{
			return showAllMembers || !AstBuilder.MemberIsHidden(member, new DecompilationOptions().DecompilerSettings);
		}
		
		void RunTransformsAndGenerateCode(AstBuilder astBuilder, ITextOutput output, DecompilationOptions options, ModuleDef module)
		{
			astBuilder.RunTransformations(transformAbortCondition);
			if (options.DecompilerSettings.ShowXmlDocumentation) {
				try {
					AddXmlDocTransform.Run(astBuilder.SyntaxTree);
				} catch (XmlException ex) {
					string[] msg = (" Exception while reading XmlDoc: " + ex.ToString()).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					var insertionPoint = astBuilder.SyntaxTree.FirstChild;
					for (int i = 0; i < msg.Length; i++)
						astBuilder.SyntaxTree.InsertChildBefore(insertionPoint, new CSharp.Comment(msg[i], CSharp.CommentType.Documentation), CSharp.Roles.Comment);
				}
			}
			var csharpUnit = astBuilder.SyntaxTree;
			csharpUnit.AcceptVisitor(new NRefactory.CSharp.InsertParenthesesVisitor() { InsertParenthesesForReadability = true });
			var unit = csharpUnit.AcceptVisitor(new CSharpToVBConverterVisitor(new ILSpyEnvironmentProvider()), null);
			var outputFormatter = new VBTextOutputFormatter(output);
			var formattingPolicy = new VBFormattingOptions();
			unit.AcceptVisitor(new OutputVisitor(outputFormatter, formattingPolicy), null);
		}
		
		AstBuilder CreateAstBuilder(DecompilationOptions options, ModuleDef currentModule = null, TypeDef currentType = null, bool isSingleMember = false)
		{
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
		
		public override string FormatTypeName(TypeDef type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			
			return TypeToString(ConvertTypeOptions.DoNotUsePrimitiveTypeNames | ConvertTypeOptions.IncludeTypeParameterDefinitions, type);
		}
		
		public override string TypeToString(ITypeDefOrRef type, bool includeNamespace, IHasCustomAttribute typeAttributes = null)
		{
			ConvertTypeOptions options = ConvertTypeOptions.IncludeTypeParameterDefinitions;
			if (includeNamespace)
				options |= ConvertTypeOptions.IncludeNamespace;

			return TypeToString(options, type, typeAttributes);
		}
		
		string TypeToString(ConvertTypeOptions options, ITypeDefOrRef type, IHasCustomAttribute typeAttributes = null)
		{
			var envProvider = new ILSpyEnvironmentProvider();
			var converter = new CSharpToVBConverterVisitor(envProvider);
			var astType = AstBuilder.ConvertType(type, typeAttributes, options);
			StringWriter w = new StringWriter();

			if (type.TryGetByRefSig() != null) {
				w.Write("ByRef ");
				if (astType is NRefactory.CSharp.ComposedType && ((NRefactory.CSharp.ComposedType)astType).PointerRank > 0)
					((NRefactory.CSharp.ComposedType)astType).PointerRank--;
			}
			
			var vbAstType = astType.AcceptVisitor(converter, null);
			
			vbAstType.AcceptVisitor(new OutputVisitor(w, new VBFormattingOptions()), null);
			return w.ToString();
		}
	}
}
