/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Resources;
using dnlib.PE;
using dnSpy.Contracts.Decompiler;
using dnSpy.Decompiler.Properties;

namespace dnSpy.Decompiler.MSBuild {
	sealed class Project {
		public ProjectModuleOptions Options { get; }
		public string DefaultNamespace { get; }
		public string AssemblyName { get; }
		public ModuleDef Module => Options.Module;
		public List<ProjectFile> Files { get; }
		public Guid Guid => Options.ProjectGuid;
		public Guid LanguageGuid { get; }
		public string Filename { get; }
		public string Directory { get; }
		public string? Platform { get; set; }
		public HashSet<Guid> ProjectTypeGuids { get; }
		public HashSet<string> ExtraAssemblyReferences { get; }
		public string? StartupObject { get; private set; }
		public bool AllowUnsafeBlocks { get; private set; }
		public string PropertiesFolder { get; }
		public ApplicationIcon? ApplicationIcon => applicationIcon;
		public ApplicationManifest? ApplicationManifest => applicationManifest;

		ApplicationIcon? applicationIcon;
		ApplicationManifest? applicationManifest;

		readonly SatelliteAssemblyFinder satelliteAssemblyFinder;
		readonly Func<TextWriter, IDecompilerOutput> createDecompilerOutput;

		public Project(ProjectModuleOptions options, string projDir, SatelliteAssemblyFinder satelliteAssemblyFinder, Func<TextWriter, IDecompilerOutput> createDecompilerOutput) {
			Options = options ?? throw new ArgumentNullException(nameof(options));
			Directory = projDir;
			this.satelliteAssemblyFinder = satelliteAssemblyFinder;
			this.createDecompilerOutput = createDecompilerOutput;
			Files = new List<ProjectFile>();
			DefaultNamespace = new DefaultNamespaceFinder(options.Module).Find();
			Filename = Path.Combine(projDir, Path.GetFileName(projDir) + options.Decompiler.ProjectFileExtension);
			AssemblyName = options.Module.Assembly is null ? string.Empty : options.Module.Assembly.Name.String;
			ProjectTypeGuids = new HashSet<Guid>();
			PropertiesFolder = CalculatePropertiesFolder();
			ExtraAssemblyReferences = new HashSet<string>();
			LanguageGuid = CalculateLanguageGuid(options.Decompiler);
		}

		static Guid CalculateLanguageGuid(IDecompiler decompiler) {
			if (decompiler.GenericGuid == DecompilerConstants.LANGUAGE_VISUALBASIC)
				return new Guid("F184B08F-C81C-45F6-A57F-5ABD9991F28F");

			Debug.Assert(decompiler.GenericGuid == DecompilerConstants.LANGUAGE_CSHARP);
			return new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");
		}

		string CalculatePropertiesFolder() {
			if (Options.Decompiler.GenericGuid == DecompilerConstants.LANGUAGE_VISUALBASIC)
				return "My Project";
			return "Properties";
		}

		public void CreateProjectFiles(DecompileContext ctx) {
			var filenameCreator = new FilenameCreator(Directory, DefaultNamespace);
			var resourceNameCreator = new ResourceNameCreator(Options.Module, filenameCreator);

			AllowUnsafeBlocks = DotNetUtils.IsUnsafe(Options.Module);
			InitializeSplashScreen();
			if (Options.Decompiler.CanDecompile(DecompilationType.AssemblyInfo)) {
				var filename = filenameCreator.CreateFromRelativePath(Path.Combine(PropertiesFolder, "AssemblyInfo"), Options.Decompiler.FileExtension);
				Files.Add(new AssemblyInfoProjectFile(Options.Module, filename, Options.DecompilationContext, Options.Decompiler, createDecompilerOutput));
			}

			var ep = Options.Module.EntryPoint;
			if (!(ep is null) && !(ep.DeclaringType is null))
				StartupObject = ep.DeclaringType.ReflectionFullName;

			applicationManifest = ApplicationManifest.TryCreate(Options.Module.Win32Resources, filenameCreator);
			if (!(ApplicationManifest is null))
				Files.Add(new ApplicationManifestProjectFile(ApplicationManifest.Filename));

			foreach (var rsrc in Options.Module.Resources) {
				ctx.CancellationToken.ThrowIfCancellationRequested();
				switch (rsrc.ResourceType) {
				case ResourceType.Embedded:
					foreach (var file in CreateEmbeddedResourceFiles(Options.Module, resourceNameCreator, (EmbeddedResource)rsrc)) {
						Files.Add(file);
						Files.AddRange(CreateSatelliteFiles(rsrc.Name, filenameCreator, file));
					}
					break;

				case ResourceType.AssemblyLinked:
					//TODO: What should be created here?
					break;

				case ResourceType.Linked:
					//TODO: What should be created here?
					break;

				default:
					break;
				}
			}
			InitializeXaml();
			InitializeResX();
			foreach (var type in Options.Module.Types) {
				ctx.CancellationToken.ThrowIfCancellationRequested();
				if (!DecompileType(type))
					continue;
				Files.Add(CreateTypeProjectFile(type, filenameCreator));
			}
			CreateEmptyAppXamlFile();

			var existingAppConfig = Options.Module.Location + ".config";
			if (File.Exists(existingAppConfig))
				Files.Add(new AppConfigProjectFile(filenameCreator.CreateName("app.config"), existingAppConfig));

			applicationIcon = ApplicationIcon.TryCreate(Options.Module.Win32Resources, Path.GetFileName(Directory), filenameCreator);

			var dirs = new HashSet<string>(Files.Select(a => GetDirectoryName(a.Filename)).OfType<string>(), StringComparer.OrdinalIgnoreCase);
			int errors = 0;
			foreach (var dir in dirs) {
				ctx.CancellationToken.ThrowIfCancellationRequested();
				try {
					System.IO.Directory.CreateDirectory(dir);
				}
				catch (Exception ex) {
					if (errors++ < 20)
						ctx.Logger.Error(string.Format(dnSpy_Decompiler_Resources.MSBuild_CouldNotCreateDirectory2, dir, ex.Message));
				}
			}
		}

		static string? GetDirectoryName(string s) {
			try {
				return Path.GetDirectoryName(s);
			}
			catch (ArgumentException) {
			}
			catch (PathTooLongException) {
			}
			return null;
		}

		void InitializeSplashScreen() {
			var ep = Options.Module.EntryPoint;
			if (ep is null || ep.Body is null)
				return;
			var instrs = ep.Body.Instructions;
			for (int i = 0; i + 1 < instrs.Count; i++) {
				var newobj = instrs[i + 1];
				if (newobj.OpCode.Code != Code.Newobj)
					continue;
				var s = instrs[i].Operand as string;
				if (s is null)
					continue;
				var ctor = newobj.Operand as IMethod;
				if (ctor is null || ctor.MethodSig is null)
					continue;
				if (ctor.FullName != "System.Void System.Windows.SplashScreen::.ctor(System.String)" &&
					ctor.FullName != "System.Void System.Windows.SplashScreen::.ctor(System.Reflection.Assembly,System.String)")
					continue;
				splashScreenImageName = s;
				break;
			}
		}
		string? splashScreenImageName;

		ProjectFile CreateTypeProjectFile(TypeDef type, FilenameCreator filenameCreator) {
			var bamlFile = TryGetBamlFile(type);
			if (!(bamlFile is null)) {
				var filename = filenameCreator.Create(GetTypeExtension(type), type.FullName);
				TypeProjectFile newFile;
				var isAppType = DotNetUtils.IsSystemWindowsApplication(type);
				if (!Options.Decompiler.CanDecompile(DecompilationType.PartialType))
					newFile = new TypeProjectFile(type, filename, Options.DecompilationContext, Options.Decompiler, createDecompilerOutput);
				else
					newFile = new XamlTypeProjectFile(type, filename, Options.DecompilationContext, Options.Decompiler, createDecompilerOutput);
				newFile.DependentUpon = bamlFile;
				if (isAppType && DotNetUtils.IsStartUpClass(type)) {
					bamlFile.IsAppDef = true;
					StartupObject = null;
				}
				if (isAppType)
					appTypeProjFile = newFile;
				return newFile;
			}

			const string DESIGNER = ".Designer";
			var resxFile = TryGetResXFile(type);
			if (DotNetUtils.IsWinForm(type)) {
				var fname = !(resxFile is null) ? Path.GetFileNameWithoutExtension(resxFile.Filename) : type.Name.String;
				var filename = filenameCreator.CreateFromNamespaceName(GetTypeExtension(type), type.ReflectionNamespace, fname);
				var dname = filenameCreator.CreateFromNamespaceName(GetTypeExtension(type), type.ReflectionNamespace, fname + DESIGNER);

				var newFile = new WinFormsProjectFile(type, filename, Options.DecompilationContext, Options.Decompiler, createDecompilerOutput);
				if (!(resxFile is null))
					resxFile.DependentUpon = newFile;
				var winFormsDesignerFile = new WinFormsDesignerProjectFile(newFile, dname, createDecompilerOutput);
				winFormsDesignerFile.DependentUpon = newFile;
				Files.Add(winFormsDesignerFile);
				return newFile;
			}
			else if (!(resxFile is null)) {
				var filename = filenameCreator.CreateFromNamespaceName(GetTypeExtension(type), type.ReflectionNamespace, Path.GetFileNameWithoutExtension(resxFile.Filename) + DESIGNER);
				var newFile = new TypeProjectFile(type, filename, Options.DecompilationContext, Options.Decompiler, createDecompilerOutput);
				newFile.DependentUpon = resxFile;
				newFile.AutoGen = true;
				newFile.DesignTime = true;
				resxFile.Generator = type.IsPublic ? "PublicResXFileCodeGenerator" : "ResXFileCodeGenerator";
				resxFile.LastGenOutput = newFile;
				return newFile;
			}

			var bt = type.BaseType;
			if (!(bt is null) && bt.FullName == "System.Configuration.ApplicationSettingsBase") {
				var designerFilename = filenameCreator.Create(DESIGNER + GetTypeExtension(type), type.FullName);
				var settingsFilename = filenameCreator.Create(".settings", type.FullName);
				ProjectFile designerTypeFile;
				if (Options.Decompiler.CanDecompile(DecompilationType.PartialType)) {
					var typeFilename = filenameCreator.Create(GetTypeExtension(type), type.FullName);
					var settingsTypeFile = new SettingsTypeProjectFile(type, typeFilename, Options.DecompilationContext, Options.Decompiler, createDecompilerOutput);
					designerTypeFile = new SettingsDesignerTypeProjectFile(settingsTypeFile, designerFilename, createDecompilerOutput);
					Files.Add(settingsTypeFile);
				}
				else
					designerTypeFile = new TypeProjectFile(type, designerFilename, Options.DecompilationContext, Options.Decompiler, createDecompilerOutput);
				var settingsFile = new SettingsProjectFile(type, settingsFilename);
				designerTypeFile.DependentUpon = settingsFile;
				designerTypeFile.AutoGen = true;
				designerTypeFile.DesignTimeSharedInput = true;
				settingsFile.Generator = type.IsPublic ? "PublicSettingsSingleFileGenerator" : "SettingsSingleFileGenerator";
				settingsFile.LastGenOutput = designerTypeFile;
				Files.Add(settingsFile);
				return designerTypeFile;
			}

			var newFilename = filenameCreator.Create(GetTypeExtension(type), type.FullName);
			return new TypeProjectFile(type, newFilename, Options.DecompilationContext, Options.Decompiler, createDecompilerOutput);
		}

		void CreateEmptyAppXamlFile() {
			if (!hasXamlClasses || !(appTypeProjFile is null))
				return;
			if ((Options.Module.Characteristics & Characteristics.Dll) != 0)
				return;

			var file = Files.OfType<TypeProjectFile>().Where(a => DotNetUtils.IsSystemWindowsApplication(a.Type)).FirstOrDefault();
			Debug2.Assert(!(file is null));
			if (file is null)
				return;
			Debug2.Assert(file.DependentUpon is null);
			if (!(file.DependentUpon is null))
				return;

			Files.Remove(file);

			var filename = file.Filename;
			var name = Path.GetFileNameWithoutExtension(file.Filename);
			filename = Path.Combine(Path.GetDirectoryName(filename)!, name + ".xaml");

			var newFile = new XamlTypeProjectFile(file.Type, filename + Options.Decompiler.FileExtension, Options.DecompilationContext, Options.Decompiler, createDecompilerOutput);
			Files.Add(newFile);
			var bamlFile = new AppBamlResourceProjectFile(filename, file.Type, Options.Decompiler);
			newFile.DependentUpon = bamlFile;
			Files.Add(bamlFile);
		}
		TypeProjectFile? appTypeProjFile;

		void InitializeXaml() {
			typeFullNameToBamlFile = new Dictionary<string, BamlResourceProjectFile>(StringComparer.OrdinalIgnoreCase);
			foreach (var xamlFile in Files.OfType<BamlResourceProjectFile>()) {
				hasXamlClasses = true;
				if (!string.IsNullOrEmpty(xamlFile.TypeFullName) && !xamlFile.IsSatelliteFile)
					typeFullNameToBamlFile[xamlFile.TypeFullName] = xamlFile;
			}
			if (hasXamlClasses) {
				ExtraAssemblyReferences.Add("WindowsBase");
				ExtraAssemblyReferences.Add("PresentationCore");
				ExtraAssemblyReferences.Add("PresentationFramework");
				if (!Options.Module.IsClr1x && !Options.Module.IsClr20)
					ExtraAssemblyReferences.Add("System.Xaml");
			}

			if (hasXamlClasses || ReferencesWPFClasses()) {
				ProjectTypeGuids.Add(new Guid("60DC8134-EBA5-43B8-BCC9-BB4BC16C2548"));
				if (Options.Decompiler.GenericGuid == DecompilerConstants.LANGUAGE_VISUALBASIC)
					ProjectTypeGuids.Add(new Guid("F184B08F-C81C-45F6-A57F-5ABD9991F28F"));
				else if (Options.Decompiler.GenericGuid == DecompilerConstants.LANGUAGE_CSHARP)
					ProjectTypeGuids.Add(new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC"));
			}
		}
		Dictionary<string, BamlResourceProjectFile>? typeFullNameToBamlFile;
		bool hasXamlClasses;

		bool ReferencesWPFClasses() {
			foreach (var asmRef in Options.Module.GetAssemblyRefs()) {
				switch (asmRef.Name) {
				case "WindowsBase":
				case "PresentationCore":
				case "PresentationFramework":
					return true;
				}
			}
			return false;
		}

		BamlResourceProjectFile? TryGetBamlFile(TypeDef type) {
			Debug2.Assert(!(typeFullNameToBamlFile is null));
			typeFullNameToBamlFile.TryGetValue(type.FullName, out var bamlFile);
			return bamlFile;
		}

		void InitializeResX() {
			typeFullNameToResXFile = new Dictionary<string, ResXProjectFile>(StringComparer.Ordinal);
			foreach (var resxFile in Files.OfType<ResXProjectFile>()) {
				if (!string.IsNullOrEmpty(resxFile.TypeFullName) && !resxFile.IsSatelliteFile)
					typeFullNameToResXFile[resxFile.TypeFullName] = resxFile;
			}
		}
		Dictionary<string, ResXProjectFile>? typeFullNameToResXFile;

		ResXProjectFile? TryGetResXFile(TypeDef type) {
			Debug2.Assert(!(typeFullNameToResXFile is null));
			typeFullNameToResXFile.TryGetValue(type.FullName, out var resxFile);
			return resxFile;
		}

		string GetTypeExtension(TypeDef type) {
			Debug2.Assert(!(typeFullNameToBamlFile is null));
			if (typeFullNameToBamlFile.TryGetValue(type.FullName, out var bamlFile))
				return ".xaml" + Options.Decompiler.FileExtension;
			return Options.Decompiler.FileExtension;
		}

		IEnumerable<ProjectFile> CreateEmbeddedResourceFiles(ModuleDef module, ResourceNameCreator resourceNameCreator, EmbeddedResource er) {
			if (!Options.UnpackResources) {
				yield return CreateRawEmbeddedResourceProjectFile(module, resourceNameCreator, er);
				yield break;
			}

			if (ResourceReader.CouldBeResourcesFile(er.CreateReader())) {
				var files = TryCreateResourceFiles(module, resourceNameCreator, er);
				if (!(files is null)) {
					foreach (var file in files)
						yield return file;
					yield break;
				}
			}

			yield return CreateRawEmbeddedResourceProjectFile(module, resourceNameCreator, er);
		}

		List<ProjectFile>? TryCreateResourceFiles(ModuleDef module, ResourceNameCreator resourceNameCreator, EmbeddedResource er) {
			ResourceElementSet set;
			try {
				set = ResourceReader.Read(module, er.CreateReader());
			}
			catch {
				return null;
			}
			if (IsXamlResource(module, er.Name, set))
				return CreateXamlResourceFiles(module, resourceNameCreator, set).ToList();
			if (Options.CreateResX) {
				string filename = resourceNameCreator.GetResxFilename(er.Name, out string typeFullName);
				return new List<ProjectFile>() { CreateResXFile(module, er, set, filename, typeFullName, false) };
			}

			return null;
		}

		bool IsXamlResource(ModuleDef module, string name, ResourceElementSet set) {
			var asm = module.Assembly;
			if (asm is null || !module.IsManifestModule)
				return false;

			string culture = UTF8String.IsNullOrEmpty(asm.Culture) ? string.Empty : "." + asm.Culture;
			if (!StringComparer.OrdinalIgnoreCase.Equals(asm.Name + ".g" + culture + ".resources", name))
				return false;

			var elems = set.ResourceElements.ToArray();
			if (elems.Length == 0)
				return false;
			foreach (var e in elems) {
				if (!(e.ResourceData.Code == ResourceTypeCode.ByteArray || e.ResourceData.Code == ResourceTypeCode.Stream))
					return false;
			}
			return true;
		}

		IEnumerable<ProjectFile> CreateXamlResourceFiles(ModuleDef module, ResourceNameCreator resourceNameCreator, ResourceElementSet set) {
			bool decompileBaml = Options.DecompileXaml && !(Options.DecompileBaml is null);
			foreach (var e in set.ResourceElements) {
				Debug.Assert(e.ResourceData.Code == ResourceTypeCode.ByteArray || e.ResourceData.Code == ResourceTypeCode.Stream);
				var data = (byte[])((BuiltInResourceData)e.ResourceData).Data;

				var rsrcName = Uri.UnescapeDataString(e.Name);
				if (decompileBaml && rsrcName.EndsWith(".baml", StringComparison.OrdinalIgnoreCase)) {
					var filename = resourceNameCreator.GetBamlResourceName(rsrcName, out string typeFullName);
					yield return new BamlResourceProjectFile(filename, data, typeFullName, (bamlData, stream) => Options.DecompileBaml!(module, bamlData, Options.DecompilationContext.CancellationToken, stream));
				}
				else if (StringComparer.InvariantCultureIgnoreCase.Equals(splashScreenImageName, e.Name)) {
					var filename = resourceNameCreator.GetXamlResourceFilename(rsrcName);
					yield return new SplashScreenProjectFile(filename, data, e.Name);
				}
				else {
					var filename = resourceNameCreator.GetXamlResourceFilename(rsrcName);
					yield return new ResourceProjectFile(filename, data, e.Name);
				}
			}
		}

		ResXProjectFile CreateResXFile(ModuleDef module, EmbeddedResource er, ResourceElementSet set, string filename, string typeFullName, bool isSatellite) {
			Debug.Assert(Options.CreateResX);
			if (!Options.CreateResX)
				throw new InvalidOperationException();

			return new ResXProjectFile(module, filename, typeFullName, er) {
				IsSatelliteFile = isSatellite,
			};
		}

		RawEmbeddedResourceProjectFile CreateRawEmbeddedResourceProjectFile(ModuleDef module, ResourceNameCreator resourceNameCreator, EmbeddedResource er) => new RawEmbeddedResourceProjectFile(resourceNameCreator.GetResourceFilename(er.Name), er);

		bool DecompileType(TypeDef type) {
			if (!Options.Decompiler.ShowMember(type))
				return false;

			if (type.IsGlobalModuleType && type.Methods.Count == 0 && type.Fields.Count == 0 &&
				type.Properties.Count == 0 && type.Events.Count == 0 && type.NestedTypes.Count == 0) {
				return false;
			}

			if (type.Namespace == "XamlGeneratedNamespace" && type.Name == "GeneratedInternalTypeHelper")
				return false;

			return true;
		}

		IEnumerable<ProjectFile> CreateSatelliteFiles(string rsrcName, FilenameCreator filenameCreator, ProjectFile nonSatFile) {
			foreach (var satMod in satelliteAssemblyFinder.GetSatelliteAssemblies(Options.Module)) {
				var satFile = TryCreateSatelliteFile(satMod, rsrcName, filenameCreator, nonSatFile);
				if (!(satFile is null))
					yield return satFile;
			}
		}

		ProjectFile? TryCreateSatelliteFile(ModuleDef module, string rsrcName, FilenameCreator filenameCreator, ProjectFile nonSatFile) {
			if (!Options.CreateResX)
				return null;
			var asm = module.Assembly;
			Debug2.Assert(!(asm is null) && !UTF8String.IsNullOrEmpty(asm.Culture));
			if (asm is null || UTF8String.IsNullOrEmpty(asm.Culture))
				return null;
			var name = FileUtils.RemoveExtension(rsrcName);
			var ext = FileUtils.GetExtension(rsrcName);
			var locName = name + "." + asm.Culture + ext;
			var er = module.Resources.OfType<EmbeddedResource>().FirstOrDefault(a => StringComparer.Ordinal.Equals(a.Name, locName));
			var set = TryCreateResourceElementSet(module, er);
			if (set is null)
				return null;
			Debug2.Assert(!(er is null));

			var dirName = Path.GetDirectoryName(nonSatFile.Filename)!;
			var dir = Directory.Length + 1 > dirName.Length ? string.Empty : dirName.Substring(Directory.Length + 1);
			name = Path.GetFileNameWithoutExtension(nonSatFile.Filename);
			ext = Path.GetExtension(nonSatFile.Filename);
			var filename = filenameCreator.CreateFromRelativePath(Path.Combine(dir, name) + "." + asm.Culture, ext);
			return CreateResXFile(module, er, set, filename, string.Empty, true);
		}

		static ResourceElementSet? TryCreateResourceElementSet(ModuleDef module, EmbeddedResource? er) {
			if (er is null)
				return null;
			if (!ResourceReader.CouldBeResourcesFile(er.CreateReader()))
				return null;
			try {
				return ResourceReader.Read(module, er.CreateReader());
			}
			catch {
				return null;
			}
		}

		public IEnumerable<IJob> GetJobs() {
			if (!(ApplicationIcon is null))
				yield return ApplicationIcon;
			if (!(ApplicationManifest is null))
				yield return ApplicationManifest;
			foreach (var f in Files)
				yield return f;
		}

		public void OnWrite() {
			string asmName = !(Options.Module.Assembly is null) && Options.Module.IsManifestModule ? Options.Module.Assembly.Name : null;
			foreach (var bamlFile in Files.OfType<BamlResourceProjectFile>()) {
				foreach (var asmRef in bamlFile.AssemblyReferences) {
					if (!(asmName is null) && !StringComparer.Ordinal.Equals(asmName, asmRef.Name))
						ExtraAssemblyReferences.Add(asmRef.Name);
				}
			}
		}
	}
}
