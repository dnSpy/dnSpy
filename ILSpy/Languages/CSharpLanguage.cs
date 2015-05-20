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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.ILSpy.Options;
using ICSharpCode.ILSpy.XmlDoc;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Decompiler logic for C#.
	/// </summary>
	[Export(typeof(Language))]
	public class CSharpLanguage : Language
	{
		string name = "C#";
		bool showAllMembers = false;
		Predicate<IAstTransform> transformAbortCondition = null;

		public CSharpLanguage()
		{
		}

		#if DEBUG
		internal static IEnumerable<CSharpLanguage> GetDebugLanguages()
		{
			DecompilerContext context = new DecompilerContext(new ModuleDefUser("dummy"));
			string lastTransformName = "no transforms";
			foreach (Type _transformType in TransformationPipeline.CreatePipeline(context).Select(v => v.GetType()).Distinct()) {
				Type transformType = _transformType; // copy for lambda
				yield return new CSharpLanguage {
					transformAbortCondition = v => transformType.IsInstanceOfType(v),
					name = "C# - " + lastTransformName,
					showAllMembers = true
				};
				lastTransformName = "after " + transformType.Name;
			}
			yield return new CSharpLanguage {
				name = "C# - " + lastTransformName,
				showAllMembers = true
			};
		}
		#endif

		public override string Name
		{
			get { return name; }
		}

		public override string FileExtension
		{
			get { return ".cs"; }
		}

		public override string ProjectFileExtension
		{
			get { return ".csproj"; }
		}

		public override void DecompileMethod(MethodDef method, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLineDeclaringType(output, method);
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: method.DeclaringType, isSingleMember: true);
			if (method.IsConstructor && !method.IsStatic && !DnlibExtensions.IsValueType(method.DeclaringType)) {
				// also fields and other ctors so that the field initializers can be shown as such
				AddFieldsAndCtors(codeDomBuilder, method.DeclaringType, method.IsStatic);
				RunTransformsAndGenerateCode(codeDomBuilder, output, options, new SelectCtorTransform(method));
			} else {
				codeDomBuilder.AddMethod(method);
				RunTransformsAndGenerateCode(codeDomBuilder, output, options);
			}
		}
		
		class SelectCtorTransform : IAstTransform
		{
			readonly MethodDef ctorDef;
			
			public SelectCtorTransform(MethodDef ctorDef)
			{
				this.ctorDef = ctorDef;
			}
			
			public void Run(AstNode compilationUnit)
			{
				ConstructorDeclaration ctorDecl = null;
				foreach (var node in compilationUnit.Children) {
					ConstructorDeclaration ctor = node as ConstructorDeclaration;
					if (ctor != null) {
						if (ctor.Annotation<MethodDef>() == ctorDef) {
							ctorDecl = ctor;
						} else {
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

		public override void DecompileProperty(PropertyDef property, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLineDeclaringType(output, property);
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: property.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddProperty(property);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options);
		}

		public override void DecompileField(FieldDef field, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLineDeclaringType(output, field);
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: field.DeclaringType, isSingleMember: true);
			if (field.IsLiteral) {
				codeDomBuilder.AddField(field);
			} else {
				// also decompile ctors so that the field initializer can be shown
				AddFieldsAndCtors(codeDomBuilder, field.DeclaringType, field.IsStatic);
			}
			RunTransformsAndGenerateCode(codeDomBuilder, output, options, new SelectFieldTransform(field));
		}
		
		/// <summary>
		/// Removes all top-level members except for the specified fields.
		/// </summary>
		sealed class SelectFieldTransform : IAstTransform
		{
			readonly FieldDef field;
			
			public SelectFieldTransform(FieldDef field)
			{
				this.field = field;
			}
			
			public void Run(AstNode compilationUnit)
			{
				foreach (var child in compilationUnit.Children) {
					if (child is EntityDeclaration) {
						if (child.Annotation<FieldDef>() != field)
							child.Remove();
					}
				}
			}
		}
		
		void AddFieldsAndCtors(AstBuilder codeDomBuilder, TypeDef declaringType, bool isStatic)
		{
			foreach (var field in declaringType.Fields) {
				if (field.IsStatic == isStatic)
					codeDomBuilder.AddField(field);
			}
			foreach (var ctor in declaringType.Methods) {
				if (ctor.IsConstructor && ctor.IsStatic == isStatic)
					codeDomBuilder.AddMethod(ctor);
			}
		}

		public override void DecompileEvent(EventDef ev, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLineDeclaringType(output, ev);
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: ev.DeclaringType, isSingleMember: true);
			codeDomBuilder.AddEvent(ev);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options);
		}

		public override void DecompileType(TypeDef type, ITextOutput output, DecompilationOptions options)
		{
			AstBuilder codeDomBuilder = CreateAstBuilder(options, currentType: type);
			codeDomBuilder.AddType(type);
			RunTransformsAndGenerateCode(codeDomBuilder, output, options);
		}
		
		void RunTransformsAndGenerateCode(AstBuilder astBuilder, ITextOutput output, DecompilationOptions options, IAstTransform additionalTransform = null)
		{
			astBuilder.RunTransformations(transformAbortCondition);
			if (additionalTransform != null) {
				additionalTransform.Run(astBuilder.SyntaxTree);
			}
			if (options.DecompilerSettings.ShowXmlDocumentation) {
				try {
					AddXmlDocTransform.Run(astBuilder.SyntaxTree);
				} catch (XmlException ex) {
					string[] msg = (" Exception while reading XmlDoc: " + ex.ToString()).Split(new[]{'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
					var insertionPoint = astBuilder.SyntaxTree.FirstChild;
					for (int i = 0; i < msg.Length; i++)
						astBuilder.SyntaxTree.InsertChildBefore(insertionPoint, new Comment(msg[i], CommentType.Documentation), Roles.Comment);
				}
			}
			astBuilder.GenerateCode(output);
		}

		public static string GetPlatformDisplayName(ModuleDef module)
		{
			switch (module.Machine) {
				case dnlib.PE.Machine.I386:
					if (module.Is32BitPreferred)
						return "AnyCPU (32-bit preferred)";
					else if (module.Is32BitRequired)
						return "x86";
					else
						return "AnyCPU (64-bit preferred)";
				case dnlib.PE.Machine.AMD64:
					return "x64";
				case dnlib.PE.Machine.IA64:
					return "Itanium";
				default:
					return module.Machine.ToString();
			}
		}
		
		public static string GetPlatformName(ModuleDef module)
		{
			switch (module.Machine) {
				case dnlib.PE.Machine.I386:
					if (module.Is32BitPreferred)
						return "AnyCPU";
					else if (module.Is32BitRequired)
						return "x86";
					else
						return "AnyCPU";
				case dnlib.PE.Machine.AMD64:
					return "x64";
				case dnlib.PE.Machine.IA64:
					return "Itanium";
				default:
					return module.Machine.ToString();
			}
		}

		public static string GetRuntimeDisplayName(ModuleDef module)
		{
			if (module.IsClr10)
				return ".NET 1.0";
			if (module.IsClr11)
				return ".NET 1.1";
			if (module.IsClr20)
				return ".NET 2.0";
			if (module.IsClr40)
				return ".NET 4.0";
			return null;
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
				ModuleDef mainModule = assembly.ModuleDefinition;
				if (decompileMod && mainModule.Types.Count > 0) {
					output.Write("// Global type: ", TextTokenType.Comment);
					output.WriteReference(mainModule.GlobalType.FullName, mainModule.GlobalType, TextTokenType.Comment);
					output.WriteLine();
				}
				if (decompileMod || decompileAsm)
					PrintEntryPoint(assembly, output);
				if (decompileMod) {
					output.WriteLine("// Architecture: " + GetPlatformDisplayName(mainModule), TextTokenType.Comment);
					if (!mainModule.IsILOnly) {
						output.WriteLine("// This assembly contains unmanaged code.", TextTokenType.Comment);
					}
					string runtimeName = GetRuntimeDisplayName(mainModule);
					if (runtimeName != null) {
						output.WriteLine("// Runtime: " + runtimeName, TextTokenType.Comment);
					}
				}
				if (decompileMod || decompileAsm)
					output.WriteLine();
				
				// don't automatically load additional assemblies when an assembly node is selected in the tree view
				using (options.FullDecompilation ? null : LoadedAssembly.DisableAssemblyLoad()) {
					AstBuilder codeDomBuilder = CreateAstBuilder(options, currentModule: assembly.ModuleDefinition);
					codeDomBuilder.AddAssembly(assembly.ModuleDefinition, !options.FullDecompilation, decompileAsm, decompileMod);
					codeDomBuilder.RunTransformations(transformAbortCondition);
					codeDomBuilder.GenerateCode(output);
				}
			}
		}

		#region WriteProjectFile
		void WriteProjectFile(TextWriter writer, IEnumerable<Tuple<string, string>> files, LoadedAssembly assembly, DecompilationOptions options)
		{
			var module = assembly.ModuleDefinition;
			const string ns = "http://schemas.microsoft.com/developer/msbuild/2003";
			string platformName = GetPlatformName(module);
			Guid guid = (App.CommandLineArguments == null ? null : App.CommandLineArguments.FixedGuid) ?? Guid.NewGuid();
			using (XmlTextWriter w = new XmlTextWriter(writer)) {
				var asmRefs = GetAssemblyRefs(options, assembly);

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
						if (asm != null && ExistsInProject(options, asm.FileName))
							continue;
						w.WriteStartElement("Reference");
						w.WriteAttributeString("Include", r.Name);
						var hintPath = GetHintPath(options, asm);
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
					var otherProj = FindOtherProject(options, asm.FileName);
					if (otherProj != null) {
						var relPath = GetRelativePath(options.SaveAsProjectDirectory, otherProj.ProjectFileName);
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

				w.WriteStartElement("Import");
				w.WriteAttributeString("Project", "$(MSBuildToolsPath)\\Microsoft.CSharp.targets");
				w.WriteEndElement();

				w.WriteEndDocument();
			}
		}

		internal static List<IAssembly> GetAssemblyRefs(DecompilationOptions options, LoadedAssembly assembly)
		{
			return new RealAssemblyReferencesFinder(options, assembly).Find();
		}

		class RealAssemblyReferencesFinder
		{
			readonly DecompilationOptions options;
			readonly LoadedAssembly assembly;
			readonly List<IAssembly> allReferences = new List<IAssembly>();
			readonly HashSet<IAssembly> checkedAsms = new HashSet<IAssembly>(AssemblyNameComparer.CompareAll);

			public RealAssemblyReferencesFinder(DecompilationOptions options, LoadedAssembly assembly)
			{
				this.options = options;
				this.assembly = assembly;
			}

			bool ShouldFindRealAsms()
			{
				return options.ProjectFiles != null && options.DontReferenceStdLib;
			}

			public List<IAssembly> Find()
			{
				if (!ShouldFindRealAsms()) {
					var mod = assembly.ModuleDefinition as ModuleDefMD;
					if (mod != null)
						allReferences.AddRange(mod.GetAssemblyRefs());
				}
				else {
					Find(assembly.ModuleDefinition.CorLibTypes.Object.TypeRef);
					var mod = assembly.ModuleDefinition as ModuleDefMD;
					if (mod != null) {
						// Some types might've been moved to assembly A and some other types to
						// assembly B. Therefore we must check every type reference and we can't
						// just loop over all asm refs.
						foreach (var tr in mod.GetTypeRefs())
							Find(tr);
						for (uint rid = 1; ; rid++) {
							var et = mod.ResolveExportedType(rid);
							if (et == null)
								break;
							Find(et);
						}
					}
				}
				return allReferences;
			}

			void Find(ExportedType et)
			{
				if (et == null)
					return;
				// The type might've been moved, so always resolve it instead of using DefinitionAssembly
				var td = et.Resolve(assembly.ModuleDefinition);
				if (td == null)
					Find(et.DefinitionAssembly);
				else
					Find(td.DefinitionAssembly ?? et.DefinitionAssembly);
			}

			void Find(TypeRef typeRef)
			{
				if (typeRef == null)
					return;
				// The type might've been moved, so always resolve it instead of using DefinitionAssembly
				var td = typeRef.Resolve(assembly.ModuleDefinition);
				if (td == null)
					Find(typeRef.DefinitionAssembly);
				else
					Find(td.DefinitionAssembly ?? typeRef.DefinitionAssembly);
			}

			void Find(IAssembly asmRef)
			{
				if (asmRef == null)
					return;
				if (checkedAsms.Contains(asmRef))
					return;
				checkedAsms.Add(asmRef);
				var asm = assembly.LookupReferencedAssembly(asmRef, assembly.ModuleDefinition);
				if (asm == null)
					allReferences.Add(asmRef);
				else
					AddKnown(asm);
			}

			void AddKnown(LoadedAssembly asm)
			{
				if (asm.FileName.Equals(assembly.FileName, StringComparison.OrdinalIgnoreCase))
					return;
				if (asm.ModuleDefinition.Assembly != null)
					allReferences.Add(asm.ModuleDefinition.Assembly);
			}
		}

		internal static string GetHintPath(DecompilationOptions options, LoadedAssembly asmRef)
		{
			if (asmRef == null || options.ProjectFiles == null || options.SaveAsProjectDirectory == null)
				return null;
			if (asmRef.IsGAC)
				return null;
			if (ExistsInProject(options, asmRef.FileName))
				return null;

			return GetRelativePath(options.SaveAsProjectDirectory, asmRef.FileName);
		}

		// ("C:\dir1\dir2\dir3", "d:\Dir1\Dir2\Dir3\file.dll") = "d:\Dir1\Dir2\Dir3\file.dll"
		// ("C:\dir1\dir2\dir3", "c:\Dir1\dirA\dirB\file.dll") = "..\..\dirA\dirB\file.dll"
		// ("C:\dir1\dir2\dir3", "c:\Dir1\Dir2\Dir3\Dir4\Dir5\file.dll") = "Dir4\Dir5\file.dll"
		internal static string GetRelativePath(string sourceDir, string destFile)
		{
			sourceDir = Path.GetFullPath(sourceDir);
			destFile = Path.GetFullPath(destFile);
			if (!Path.GetPathRoot(sourceDir).Equals(Path.GetPathRoot(destFile), StringComparison.OrdinalIgnoreCase))
				return destFile;
			var sourceDirs = GetPathNames(sourceDir);
			var destDirs = GetPathNames(Path.GetDirectoryName(destFile));

			var hintPath = string.Empty;
			int i;
			for (i = 0; i < sourceDirs.Count && i < destDirs.Count; i++) {
				if (!sourceDirs[i].Equals(destDirs[i], StringComparison.OrdinalIgnoreCase))
					break;
			}
			for (int j = i; j < sourceDirs.Count; j++)
				hintPath = Path.Combine(hintPath, "..");
			for (; i < destDirs.Count; i++)
				hintPath = Path.Combine(hintPath, destDirs[i]);
			hintPath = Path.Combine(hintPath, Path.GetFileName(destFile));

			return hintPath;
		}

		static List<string> GetPathNames(string path)
		{
			var list = new List<string>();
			var root = Path.GetPathRoot(path);
			while (path != root) {
				list.Add(Path.GetFileName(path));
				path = Path.GetDirectoryName(path);
			}
			list.Add(root);
			list.Reverse();
			return list;
		}

		internal static bool ExistsInProject(DecompilationOptions options, string fileName)
		{
			return FindOtherProject(options, fileName) != null;
		}

		internal static ProjectInfo FindOtherProject(DecompilationOptions options, string fileName)
		{
			if (options.ProjectFiles == null)
				return null;
			return options.ProjectFiles.FirstOrDefault(f => Path.GetFullPath(f.AssemblyFileName).Equals(Path.GetFullPath(fileName)));
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
						codeDomBuilder.RunTransformations(transformAbortCondition);
						codeDomBuilder.GenerateCode(new PlainTextOutput(w));
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
//									MemoryStream ms = new MemoryStream();
//									entryStream.CopyTo(ms);
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

		AstBuilder CreateAstBuilder(DecompilationOptions options, ModuleDef currentModule = null, TypeDef currentType = null, bool isSingleMember = false)
		{
			if (currentModule == null)
				currentModule = currentType.Module;
			DecompilerSettings settings = options.DecompilerSettings;
			if (isSingleMember) {
				settings = settings.Clone();
				settings.UsingDeclarations = false;
			}
			return new AstBuilder(
				new DecompilerContext(currentModule) {
					CancellationToken = options.CancellationToken,
					CurrentType = currentType,
					Settings = settings
				}) {
				DontShowCreateMethodBodyExceptions = options.DontShowCreateMethodBodyExceptions,
			};
		}

		public override void TypeToString(ITextOutput output, ITypeDefOrRef type, bool includeNamespace, IHasCustomAttribute typeAttributes = null)
		{
			ConvertTypeOptions options = ConvertTypeOptions.IncludeTypeParameterDefinitions;
			if (includeNamespace)
				options |= ConvertTypeOptions.IncludeNamespace;

			TypeToString(output, options, type, typeAttributes);
		}

		void TypeToString(ITextOutput output, ConvertTypeOptions options, ITypeDefOrRef type, IHasCustomAttribute typeAttributes = null)
		{
			AstType astType = AstBuilder.ConvertType(type, typeAttributes, options);

			if (type.TryGetByRefSig() != null) {
				ParamDef pd = typeAttributes as ParamDef;
				if (pd != null && (!pd.IsIn && pd.IsOut)) {
					output.Write("out", TextTokenType.Keyword);
					output.WriteSpace();
				}
				else {
					output.Write("ref", TextTokenType.Keyword);
					output.WriteSpace();
				}

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

		public override string FormatPropertyName(PropertyDef property, bool? isIndexer)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			if (!isIndexer.HasValue) {
				isIndexer = property.IsIndexer();
			}
			if (isIndexer.Value) {
				var buffer = new System.Text.StringBuilder();
				var accessor = property.GetMethod ?? property.SetMethod;
				if (accessor != null && accessor.HasOverrides) {
					var methDecl = accessor.Overrides.First().MethodDeclaration;
					var declaringType = methDecl == null ? null : methDecl.DeclaringType;
					buffer.Append(TypeToString(declaringType, includeNamespace: true));
					buffer.Append(@".");
				}
				buffer.Append(@"this[");
				bool addSeparator = false;
				foreach (var p in property.PropertySig.GetParameters()) {
					if (addSeparator)
						buffer.Append(@", ");
					else
						addSeparator = true;
					buffer.Append(TypeToString(p.ToTypeDefOrRef(), includeNamespace: true));
				}
				buffer.Append(@"]");
				return buffer.ToString();
			} else
				return property.Name;
		}
		
		public override string FormatTypeName(TypeDef type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			var writer = new StringWriter();
			var output = new PlainTextOutput(writer);
			TypeToString(output, ConvertTypeOptions.DoNotUsePrimitiveTypeNames | ConvertTypeOptions.IncludeTypeParameterDefinitions, type);
			return writer.ToString();
		}

		public override bool ShowMember(IMemberRef member)
		{
			return showAllMembers || !AstBuilder.MemberIsHidden(member, new DecompilationOptions().DecompilerSettings);
		}

		public override IMemberRef GetOriginalCodeLocation(IMemberRef member)
		{
			if (showAllMembers || !DecompilerSettingsPanel.CurrentDecompilerSettings.AnonymousMethods)
				return member;
			else
				return TreeNodes.Analyzer.Helpers.GetOriginalCodeLocation(member);
		}

		public override void WriteTooltip(ITextOutput output, IMemberRef member)
		{
			MethodDef md = member as MethodDef;
			PropertyDef pd = member as PropertyDef;
			EventDef ed = member as EventDef;
			FieldDef fd = member as FieldDef;
			if (md != null || pd != null || ed != null || fd != null) {
				AstBuilder b = new AstBuilder(new DecompilerContext(member.Module) { Settings = new DecompilerSettings { UsingDeclarations = false, FullyQualifyAmbiguousTypeNames = false } });
				b.DecompileMethodBodies = false;
				if (md != null)
					b.AddMethod(md);
				else if (pd != null)
					b.AddProperty(pd);
				else if (ed != null)
					b.AddEvent(ed);
				else
					b.AddField(fd);
				b.RunTransformations();
				foreach (var attribute in b.SyntaxTree.Descendants.OfType<AttributeSection>())
					attribute.Remove();

				b.GenerateCode(output);
				return;
			}

			base.WriteTooltip(output, member);
		}
	}
}
