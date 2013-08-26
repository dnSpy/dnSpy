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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Visitors;
using Mono.Cecil;

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
			output.WriteLine("' " + comment);
		}
		
		public override void DecompileAssembly(LoadedAssembly assembly, ITextOutput output, DecompilationOptions options)
		{
			if (options.FullDecompilation && options.SaveAsProjectDirectory != null) {
				HashSet<string> directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				var files = WriteCodeFilesInProject(assembly.ModuleDefinition, options, directories).ToList();
				files.AddRange(WriteResourceFilesInProject(assembly, options, directories));
				WriteProjectFile(new TextOutputWriter(output), files, assembly.ModuleDefinition);
			} else {
				base.DecompileAssembly(assembly, output, options);
				output.WriteLine();
				ModuleDefinition mainModule = assembly.ModuleDefinition;
				if (mainModule.EntryPoint != null) {
					output.Write("' Entry point: ");
					output.WriteReference(mainModule.EntryPoint.DeclaringType.FullName + "." + mainModule.EntryPoint.Name, mainModule.EntryPoint);
					output.WriteLine();
				}
				WriteCommentLine(output, "Architecture: " + CSharpLanguage.GetPlatformDisplayName(mainModule));
				if ((mainModule.Attributes & ModuleAttributes.ILOnly) == 0) {
					WriteCommentLine(output, "This assembly contains unmanaged code.");
				}
				switch (mainModule.Runtime) {
					case TargetRuntime.Net_1_0:
						WriteCommentLine(output, "Runtime: .NET 1.0");
						break;
					case TargetRuntime.Net_1_1:
						WriteCommentLine(output, "Runtime: .NET 1.1");
						break;
					case TargetRuntime.Net_2_0:
						WriteCommentLine(output, "Runtime: .NET 2.0");
						break;
					case TargetRuntime.Net_4_0:
						WriteCommentLine(output, "Runtime: .NET 4.0");
						break;
				}
				output.WriteLine();
				
				// don't automatically load additional assemblies when an assembly node is selected in the tree view
				using (options.FullDecompilation ? null : LoadedAssembly.DisableAssemblyLoad()) {
					AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: assembly.ModuleDefinition);
					codeDomBuilder.AddAssembly(assembly.ModuleDefinition, onlyAssemblyLevel: !options.FullDecompilation);
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
		void WriteProjectFile(TextWriter writer, IEnumerable<Tuple<string, string>> files, ModuleDefinition module)
		{
			const string ns = "http://schemas.microsoft.com/developer/msbuild/2003";
			string platformName = CSharpLanguage.GetPlatformName(module);
			using (XmlTextWriter w = new XmlTextWriter(writer)) {
				w.Formatting = Formatting.Indented;
				w.WriteStartDocument();
				w.WriteStartElement("Project", ns);
				w.WriteAttributeString("ToolsVersion", "4.0");
				w.WriteAttributeString("DefaultTargets", "Build");

				w.WriteStartElement("PropertyGroup");
				w.WriteElementString("ProjectGuid", Guid.NewGuid().ToString().ToUpperInvariant());

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

				w.WriteElementString("AssemblyName", module.Assembly.Name.Name);
				switch (module.Runtime) {
					case TargetRuntime.Net_1_0:
						w.WriteElementString("TargetFrameworkVersion", "v1.0");
						break;
					case TargetRuntime.Net_1_1:
						w.WriteElementString("TargetFrameworkVersion", "v1.1");
						break;
					case TargetRuntime.Net_2_0:
						w.WriteElementString("TargetFrameworkVersion", "v2.0");
						// TODO: Detect when .NET 3.0/3.5 is required
						break;
					default:
						w.WriteElementString("TargetFrameworkVersion", "v4.0");
						// TODO: Detect TargetFrameworkProfile
						break;
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
				w.WriteEndElement(); // </PropertyGroup> (Debug)

				w.WriteStartElement("PropertyGroup"); // Release
				w.WriteAttributeString("Condition", " '$(Configuration)' == 'Release' ");
				w.WriteElementString("OutputPath", "bin\\Release\\");
				w.WriteElementString("DebugSymbols", "true");
				w.WriteElementString("DebugType", "pdbonly");
				w.WriteElementString("Optimize", "true");
				w.WriteEndElement(); // </PropertyGroup> (Release)


				w.WriteStartElement("ItemGroup"); // References
				foreach (AssemblyNameReference r in module.AssemblyReferences) {
					if (r.Name != "mscorlib") {
						w.WriteStartElement("Reference");
						w.WriteAttributeString("Include", r.Name);
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
		bool IncludeTypeWhenDecompilingProject(TypeDefinition type, DecompilationOptions options)
		{
			if (type.Name == "<Module>" || AstBuilder.MemberIsHidden(type, options.DecompilerSettings))
				return false;
			if (type.Namespace == "XamlGeneratedNamespace" && type.Name == "GeneratedInternalTypeHelper")
				return false;
			return true;
		}

		IEnumerable<Tuple<string, string>> WriteAssemblyInfo(ModuleDefinition module, DecompilationOptions options, HashSet<string> directories)
		{
			// don't automatically load additional assemblies when an assembly node is selected in the tree view
			using (LoadedAssembly.DisableAssemblyLoad())
			{
				AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: module);
				codeDomBuilder.AddAssembly(module, onlyAssemblyLevel: true);
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

		IEnumerable<Tuple<string, string>> WriteCodeFilesInProject(ModuleDefinition module, DecompilationOptions options, HashSet<string> directories)
		{
			var files = module.Types.Where(t => IncludeTypeWhenDecompilingProject(t, options)).GroupBy(
				delegate(TypeDefinition type) {
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
				delegate(IGrouping<string, TypeDefinition> file) {
					using (StreamWriter w = new StreamWriter(Path.Combine(options.SaveAsProjectDirectory, file.Key))) {
						AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: module);
						foreach (TypeDefinition type in file) {
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
		
		public override void DecompileMethod(MethodDefinition method, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(method.DeclaringType, includeNamespace: true));
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: method.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddMethod(method);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, method.Module);
		}
		
		public override void DecompileProperty(PropertyDefinition property, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(property.DeclaringType, includeNamespace: true));
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: property.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddProperty(property);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, property.Module);
		}
		
		public override void DecompileField(FieldDefinition field, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(field.DeclaringType, includeNamespace: true));
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: field.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddField(field);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, field.Module);
		}
		
		public override void DecompileEvent(EventDefinition ev, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, TypeToString(ev.DeclaringType, includeNamespace: true));
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: ev.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddEvent(ev);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, ev.Module);
		}
		
		public override void DecompileType(TypeDefinition type, ITextOutput output, DecompilationOptions options)
		{
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: type);
			codeDomBuilder.AddType(type);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, type.Module);
		}
		
		public override bool ShowMember(MemberReference member)
		{
			return showAllMembers || !AstBuilder.MemberIsHidden(member, new DecompilationOptions().DecompilerSettings);
		}
		
		void RunTransformsAndGenerateCode(AstBuilder astBuilder, ITextOutput output, DecompilationOptions options, ModuleDefinition module)
		{
			astBuilder.RunTransformations(transformAbortCondition);
			if (options.DecompilerSettings.ShowXmlDocumentation)
				AddXmlDocTransform.Run(astBuilder.SyntaxTree);
			var csharpUnit = astBuilder.SyntaxTree;
			csharpUnit.AcceptVisitor(new NRefactory.CSharp.InsertParenthesesVisitor() { InsertParenthesesForReadability = true });
			var unit = csharpUnit.AcceptVisitor(new CSharpToVBConverterVisitor(new ILSpyEnvironmentProvider()), null);
			var outputFormatter = new VBTextOutputFormatter(output);
			var formattingPolicy = new VBFormattingOptions();
			unit.AcceptVisitor(new OutputVisitor(outputFormatter, formattingPolicy), null);
		}
		
		AstBuilder CreateAstBuilder(DecompilationOptions options, ModuleDefinition currentModule = null, TypeDefinition currentType = null, bool isSingleMember = false)
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
				});
		}
		
		public override string FormatTypeName(TypeDefinition type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			
			return TypeToString(ConvertTypeOptions.DoNotUsePrimitiveTypeNames | ConvertTypeOptions.IncludeTypeParameterDefinitions, type);
		}
		
		public override string TypeToString(TypeReference type, bool includeNamespace, ICustomAttributeProvider typeAttributes = null)
		{
			ConvertTypeOptions options = ConvertTypeOptions.IncludeTypeParameterDefinitions;
			if (includeNamespace)
				options |= ConvertTypeOptions.IncludeNamespace;

			return TypeToString(options, type, typeAttributes);
		}
		
		string TypeToString(ConvertTypeOptions options, TypeReference type, ICustomAttributeProvider typeAttributes = null)
		{
			var envProvider = new ILSpyEnvironmentProvider();
			var converter = new CSharpToVBConverterVisitor(envProvider);
			var astType = AstBuilder.ConvertType(type, typeAttributes, options);
			StringWriter w = new StringWriter();

			if (type.IsByReference) {
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
