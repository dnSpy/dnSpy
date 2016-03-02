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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Resources;
using dnlib.PE;
using dnSpy.Contracts.Languages;
using dnSpy.Languages.Properties;

namespace dnSpy.Languages.MSBuild {
	sealed class Project {
		public ProjectModuleOptions Options {
			get { return options; }
		}
		readonly ProjectModuleOptions options;

		public string DefaultNamespace {
			get { return defaultNamespace; }
		}
		readonly string defaultNamespace;

		public string AssemblyName {
			get { return assemblyName; }
		}
		readonly string assemblyName;

		public ModuleDef Module {
			get { return options.Module; }
		}

		public IList<ProjectFile> Files {
			get { return files; }
		}
		readonly List<ProjectFile> files;

		public Guid Guid {
			get { return options.ProjectGuid; }
		}

		public Guid LanguageGuid {
			get { return languageGuid; }
		}
		readonly Guid languageGuid;

		public string Filename {
			get { return filename; }
		}
		readonly string filename;

		public string Directory {
			get { return projDir; }
		}
		readonly string projDir;

		public string Platform { get; set; }

		public HashSet<Guid> ProjectTypeGuids {
			get { return projectTypeGuids; }
		}
		readonly HashSet<Guid> projectTypeGuids;

		public ApplicationManifest ApplicationManifest {
			get { return applicationManifest; }
		}
		ApplicationManifest applicationManifest;

		public ApplicationIcon ApplicationIcon {
			get { return applicationIcon; }
		}
		ApplicationIcon applicationIcon;

		public HashSet<string> ExtraAssemblyReferences {
			get { return extraAssemblyReferences; }
		}
		readonly HashSet<string> extraAssemblyReferences;

		public string StartupObject { get; private set; }
		public bool AllowUnsafeBlocks { get; private set; }
		public string PropertiesFolder { get; private set; }

		readonly SatelliteAssemblyFinder satelliteAssemblyFinder;

		public Project(ProjectModuleOptions options, string projDir, SatelliteAssemblyFinder satelliteAssemblyFinder) {
			if (options == null)
				throw new ArgumentNullException();
			this.options = options;
			this.projDir = projDir;
			this.satelliteAssemblyFinder = satelliteAssemblyFinder;
			this.files = new List<ProjectFile>();
			this.defaultNamespace = new DefaultNamespaceFinder(options.Module).Find();
			this.filename = Path.Combine(projDir, Path.GetFileName(projDir) + options.Language.ProjectFileExtension);
			this.assemblyName = options.Module.Assembly == null ? string.Empty : options.Module.Assembly.Name.String;
			this.projectTypeGuids = new HashSet<Guid>();
			this.PropertiesFolder = CalculatePropertiesFolder();
			this.extraAssemblyReferences = new HashSet<string>();
			this.languageGuid = CalculateLanguageGuid(options.Language);
		}

		static Guid CalculateLanguageGuid(ILanguage language) {
			if (language.GenericGuid == LanguageConstants.LANGUAGE_VB)
				return new Guid("F184B08F-C81C-45F6-A57F-5ABD9991F28F");

			Debug.Assert(language.GenericGuid == LanguageConstants.LANGUAGE_CSHARP);
			return new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");
		}

		string CalculatePropertiesFolder() {
			if (options.Language.GenericGuid == LanguageConstants.LANGUAGE_VB)
				return "My Project";
			return "Properties";
		}

		public void CreateProjectFiles(DecompileContext ctx) {
			var filenameCreator = new FilenameCreator(projDir, defaultNamespace);
			var resourceNameCreator = new ResourceNameCreator(options.Module, filenameCreator);

			AllowUnsafeBlocks = DotNetUtils.IsUnsafe(options.Module);
			InitializeSplashScreen();
			if (options.Language.CanDecompile(DecompilationType.AssemblyInfo)) {
				var filename = filenameCreator.CreateFromRelativePath(Path.Combine(PropertiesFolder, "AssemblyInfo"), options.Language.FileExtension);
				files.Add(new AssemblyInfoProjectFile(options.Module, filename, options.DecompilationContext, options.Language));
			}

			var ep = options.Module.EntryPoint;
			if (ep != null && ep.DeclaringType != null)
				StartupObject = ep.DeclaringType.ReflectionFullName;

			applicationManifest = ApplicationManifest.TryCreate(options.Module.Win32Resources, filenameCreator);
			if (applicationManifest != null)
				files.Add(new ApplicationManifestProjectFile(applicationManifest.Filename));

			foreach (var rsrc in options.Module.Resources) {
				ctx.CancellationToken.ThrowIfCancellationRequested();
				switch (rsrc.ResourceType) {
				case ResourceType.Embedded:
					foreach (var file in CreateEmbeddedResourceFiles(options.Module, resourceNameCreator, (EmbeddedResource)rsrc)) {
						files.Add(file);
						files.AddRange(CreateSatelliteFiles(rsrc.Name, filenameCreator, file));
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
			foreach (var type in options.Module.Types) {
				ctx.CancellationToken.ThrowIfCancellationRequested();
				if (!DecompileType(type))
					continue;
				files.Add(CreateTypeProjectFile(type, filenameCreator));
			}
			CreateEmptyAppXamlFile();

			var existingAppConfig = options.Module.Location + ".config";
			if (File.Exists(existingAppConfig))
				files.Add(new AppConfigProjectFile(filenameCreator.CreateName("App.config"), existingAppConfig));

			applicationIcon = ApplicationIcon.TryCreate(options.Module.Win32Resources, Path.GetFileName(Directory), filenameCreator);

			var dirs = new HashSet<string>(files.Select(a => GetDirectoryName(a.Filename)).Where(a => a != null), StringComparer.OrdinalIgnoreCase);
			int errors = 0;
			foreach (var dir in dirs) {
				ctx.CancellationToken.ThrowIfCancellationRequested();
				try {
					System.IO.Directory.CreateDirectory(dir);
				}
				catch (Exception ex) {
					if (errors++ < 20)
						ctx.Logger.Error(string.Format(Languages_Resources.MSBuild_CouldNotCreateDirectory2, dir, ex.Message));
				}
			}
		}

		static string GetDirectoryName(string s) {
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
			var ep = options.Module.EntryPoint;
			if (ep == null || ep.Body == null)
				return;
			var instrs = ep.Body.Instructions;
			for (int i = 0; i + 1 < instrs.Count; i++) {
				var newobj = instrs[i + 1];
				if (newobj.OpCode.Code != Code.Newobj)
					continue;
				var s = instrs[i].Operand as string;
				if (s == null)
					continue;
				var ctor = newobj.Operand as IMethod;
				if (ctor == null || ctor.MethodSig == null)
					continue;
				if (ctor.FullName != "System.Void System.Windows.SplashScreen::.ctor(System.String)" &&
					ctor.FullName != "System.Void System.Windows.SplashScreen::.ctor(System.Reflection.Assembly,System.String)")
					continue;
				splashScreenImageName = s;
				break;
			}
		}
		string splashScreenImageName;

		ProjectFile CreateTypeProjectFile(TypeDef type, FilenameCreator filenameCreator) {
			var bamlFile = TryGetBamlFile(type);
			if (bamlFile != null) {
				var filename = filenameCreator.Create(GetTypeExtension(type), type.FullName);
				TypeProjectFile newFile;
				var isAppType = DotNetUtils.IsSystemWindowsApplication(type);
				if (!options.Language.CanDecompile(DecompilationType.PartialType))
					newFile = new TypeProjectFile(type, filename, options.DecompilationContext, options.Language);
				else
					newFile = new XamlTypeProjectFile(type, filename, options.DecompilationContext, options.Language);
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
			if (resxFile != null) {
				if (DotNetUtils.IsWinForm(type)) {
					var filename = filenameCreator.CreateFromNamespaceName(GetTypeExtension(type), type.ReflectionNamespace, Path.GetFileNameWithoutExtension(resxFile.Filename));
					var newFile = new WinFormsProjectFile(type, filename, options.DecompilationContext, options.Language);
					resxFile.DependentUpon = newFile;
					var dname = filenameCreator.CreateFromNamespaceName(GetTypeExtension(type), type.ReflectionNamespace, Path.GetFileNameWithoutExtension(resxFile.Filename) + DESIGNER);
					var winFormsDesignerFile = new WinFormsDesignerProjectFile(newFile, dname);
					winFormsDesignerFile.DependentUpon = newFile;
					files.Add(winFormsDesignerFile);
					return newFile;
				}
				else {
					var filename = filenameCreator.CreateFromNamespaceName(GetTypeExtension(type), type.ReflectionNamespace, Path.GetFileNameWithoutExtension(resxFile.Filename) + DESIGNER);
					var newFile = new TypeProjectFile(type, filename, options.DecompilationContext, options.Language);
					newFile.DependentUpon = resxFile;
					newFile.AutoGen = true;
					newFile.DesignTime = true;
					resxFile.Generator = type.IsPublic ? "PublicResXFileCodeGenerator" : "ResXFileCodeGenerator";
					resxFile.LastGenOutput = newFile;
					return newFile;
				}
			}

			var bt = type.BaseType;
			if (bt != null && bt.FullName == "System.Configuration.ApplicationSettingsBase") {
				var designerFilename = filenameCreator.Create(DESIGNER + GetTypeExtension(type), type.FullName);
				var settingsFilename = filenameCreator.Create(".settings", type.FullName);
				ProjectFile designerTypeFile;
				if (options.Language.CanDecompile(DecompilationType.PartialType)) {
					var typeFilename = filenameCreator.Create(GetTypeExtension(type), type.FullName);
					var settingsTypeFile = new SettingsTypeProjectFile(type, typeFilename, options.DecompilationContext, options.Language);
					designerTypeFile = new SettingsDesignerTypeProjectFile(settingsTypeFile, designerFilename);
					files.Add(settingsTypeFile);
				}
				else
					designerTypeFile = new TypeProjectFile(type, designerFilename, options.DecompilationContext, options.Language);
				var settingsFile = new SettingsProjectFile(type, settingsFilename);
				designerTypeFile.DependentUpon = settingsFile;
				designerTypeFile.AutoGen = true;
				designerTypeFile.DesignTimeSharedInput = true;
				settingsFile.Generator = type.IsPublic ? "PublicSettingsSingleFileGenerator" : "SettingsSingleFileGenerator";
				settingsFile.LastGenOutput = designerTypeFile;
				files.Add(settingsFile);
				return designerTypeFile;
			}

			var newFilename = filenameCreator.Create(GetTypeExtension(type), type.FullName);
			return new TypeProjectFile(type, newFilename, options.DecompilationContext, options.Language);
		}

		void CreateEmptyAppXamlFile() {
			if (!hasXamlClasses || appTypeProjFile != null)
				return;
			if ((options.Module.Characteristics & Characteristics.Dll) != 0)
				return;

			var file = files.OfType<TypeProjectFile>().Where(a => DotNetUtils.IsSystemWindowsApplication(a.Type)).FirstOrDefault();
			Debug.Assert(file != null);
			if (file == null)
				return;
			Debug.Assert(file.DependentUpon == null);
			if (file.DependentUpon != null)
				return;

			files.Remove(file);

			var filename = file.Filename;
			var name = Path.GetFileNameWithoutExtension(file.Filename);
			filename = Path.Combine(Path.GetDirectoryName(filename), name + ".xaml");

			var newFile = new XamlTypeProjectFile(file.Type, filename + options.Language.FileExtension, options.DecompilationContext, options.Language);
			files.Add(newFile);
			var bamlFile = new AppBamlResourceProjectFile(filename, file.Type, options.Language);
			newFile.DependentUpon = bamlFile;
			files.Add(bamlFile);
		}
		TypeProjectFile appTypeProjFile;

		void InitializeXaml() {
			typeFullNameToBamlFile = new Dictionary<string, BamlResourceProjectFile>(StringComparer.OrdinalIgnoreCase);
			foreach (var xamlFile in files.OfType<BamlResourceProjectFile>()) {
				hasXamlClasses = true;
				if (!string.IsNullOrEmpty(xamlFile.TypeFullName) && !xamlFile.IsSatelliteFile)
					typeFullNameToBamlFile[xamlFile.TypeFullName] = xamlFile;
			}
			if (hasXamlClasses) {
				extraAssemblyReferences.Add("WindowsBase");
				extraAssemblyReferences.Add("PresentationCore");
				extraAssemblyReferences.Add("PresentationFramework");
				if (!options.Module.IsClr1x && !options.Module.IsClr20)
					extraAssemblyReferences.Add("System.Xaml");
			}

			if (hasXamlClasses || ReferencesWPFClasses()) {
				projectTypeGuids.Add(new Guid("60DC8134-EBA5-43B8-BCC9-BB4BC16C2548"));
				if (options.Language.GenericGuid == LanguageConstants.LANGUAGE_VB)
					projectTypeGuids.Add(new Guid("F184B08F-C81C-45F6-A57F-5ABD9991F28F"));
				else if (options.Language.GenericGuid == LanguageConstants.LANGUAGE_CSHARP)
					projectTypeGuids.Add(new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC"));
			}
		}
		Dictionary<string, BamlResourceProjectFile> typeFullNameToBamlFile;
		bool hasXamlClasses;

		bool ReferencesWPFClasses() {
			foreach (var asmRef in options.Module.GetAssemblyRefs()) {
				switch (asmRef.Name) {
				case "WindowsBase":
				case "PresentationCore":
				case "PresentationFramework":
					return true;
				}
			}
			return false;
		}

		BamlResourceProjectFile TryGetBamlFile(TypeDef type) {
			BamlResourceProjectFile bamlFile;
			typeFullNameToBamlFile.TryGetValue(type.FullName, out bamlFile);
			return bamlFile;
		}

		void InitializeResX() {
			typeFullNameToResXFile = new Dictionary<string, ResXProjectFile>(StringComparer.Ordinal);
			foreach (var resxFile in files.OfType<ResXProjectFile>()) {
				if (!string.IsNullOrEmpty(resxFile.TypeFullName) && !resxFile.IsSatelliteFile)
					typeFullNameToResXFile[resxFile.TypeFullName] = resxFile;
			}
		}
		Dictionary<string, ResXProjectFile> typeFullNameToResXFile;

		ResXProjectFile TryGetResXFile(TypeDef type) {
			ResXProjectFile resxFile;
			typeFullNameToResXFile.TryGetValue(type.FullName, out resxFile);
			return resxFile;
		}

		string GetTypeExtension(TypeDef type) {
			BamlResourceProjectFile bamlFile;
			if (typeFullNameToBamlFile.TryGetValue(type.FullName, out bamlFile))
				return ".xaml" + options.Language.FileExtension;
			return options.Language.FileExtension;
		}

		IEnumerable<ProjectFile> CreateEmbeddedResourceFiles(ModuleDef module, ResourceNameCreator resourceNameCreator, EmbeddedResource er) {
			if (!options.UnpackResources) {
				yield return CreateRawEmbeddedResourceProjectFile(module, resourceNameCreator, er);
				yield break;
			}

			er.Data.Position = 0;
			if (ResourceReader.CouldBeResourcesFile(er.Data)) {
				var files = TryCreateResourceFiles(module, resourceNameCreator, er);
				if (files != null) {
					foreach (var file in files)
						yield return file;
					yield break;
				}
			}

			yield return CreateRawEmbeddedResourceProjectFile(module, resourceNameCreator, er);
		}

		List<ProjectFile> TryCreateResourceFiles(ModuleDef module, ResourceNameCreator resourceNameCreator, EmbeddedResource er) {
			ResourceElementSet set;
			try {
				er.Data.Position = 0;
				set = ResourceReader.Read(module, er.Data);
			}
			catch {
				return null;
			}
			if (IsXamlResource(module, er.Name, set))
				return CreateXamlResourceFiles(module, resourceNameCreator, set).ToList();
			if (options.CreateResX) {
				string typeFullName;
				string filename = resourceNameCreator.GetResxFilename(er.Name, out typeFullName);
				return new List<ProjectFile>() { CreateResXFile(module, er, set, filename, typeFullName, false) };
			}

			return null;
		}

		bool IsXamlResource(ModuleDef module, string name, ResourceElementSet set) {
			var asm = module.Assembly;
			if (asm == null || !module.IsManifestModule)
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
			bool decompileBaml = options.DecompileXaml && options.DecompileBaml != null;
			foreach (var e in set.ResourceElements) {
				Debug.Assert(e.ResourceData.Code == ResourceTypeCode.ByteArray || e.ResourceData.Code == ResourceTypeCode.Stream);
				var data = (byte[])((BuiltInResourceData)e.ResourceData).Data;

				if (decompileBaml && e.Name.EndsWith(".baml", StringComparison.OrdinalIgnoreCase)) {
					string typeFullName;
					var filename = resourceNameCreator.GetBamlResourceName(e.Name, out typeFullName);
					yield return new BamlResourceProjectFile(filename, data, typeFullName, (bamlData, stream) => options.DecompileBaml(module, bamlData, options.DecompilationContext.CancellationToken, stream));
				}
				else if (StringComparer.InvariantCultureIgnoreCase.Equals(splashScreenImageName, e.Name)) {
					var filename = resourceNameCreator.GetXamlResourceFilename(e.Name);
					yield return new SplashScreenProjectFile(filename, data, e.Name);
				}
				else {
					var filename = resourceNameCreator.GetXamlResourceFilename(e.Name);
					yield return new ResourceProjectFile(filename, data, e.Name);
				}
			}
		}

		ResXProjectFile CreateResXFile(ModuleDef module, EmbeddedResource er, ResourceElementSet set, string filename, string typeFullName, bool isSatellite) {
			Debug.Assert(options.CreateResX);
			if (!options.CreateResX)
				throw new InvalidOperationException();

			return new ResXProjectFile(module, filename, typeFullName, er) {
				IsSatelliteFile = isSatellite,
			};
		}

		RawEmbeddedResourceProjectFile CreateRawEmbeddedResourceProjectFile(ModuleDef module, ResourceNameCreator resourceNameCreator, EmbeddedResource er) {
			return new RawEmbeddedResourceProjectFile(resourceNameCreator.GetResourceFilename(er.Name), er);
		}

		bool DecompileType(TypeDef type) {
			if (!options.Language.ShowMember(type))
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
			foreach (var satMod in satelliteAssemblyFinder.GetSatelliteAssemblies(options.Module)) {
				var satFile = TryCreateSatelliteFile(satMod, rsrcName, filenameCreator, nonSatFile);
				if (satFile != null)
					yield return satFile;
			}
		}

		ProjectFile TryCreateSatelliteFile(ModuleDef module, string rsrcName, FilenameCreator filenameCreator, ProjectFile nonSatFile) {
			if (!options.CreateResX)
				return null;
			var asm = module.Assembly;
			Debug.Assert(asm != null && !UTF8String.IsNullOrEmpty(asm.Culture));
			if (asm == null || UTF8String.IsNullOrEmpty(asm.Culture))
				return null;
			var name = FileUtils.RemoveExtension(rsrcName);
			var ext = FileUtils.GetExtension(rsrcName);
			var locName = name + "." + asm.Culture + ext;
			var er = module.Resources.OfType<EmbeddedResource>().FirstOrDefault(a => StringComparer.Ordinal.Equals(a.Name, locName));
			var set = TryCreateResourceElementSet(module, er);
			if (set == null)
				return null;

			var dir = Path.GetDirectoryName(nonSatFile.Filename).Substring(projDir.Length + 1);
			name = Path.GetFileNameWithoutExtension(nonSatFile.Filename);
			ext = Path.GetExtension(nonSatFile.Filename);
			var filename = filenameCreator.CreateFromRelativePath(Path.Combine(dir, name) + "." + asm.Culture, ext);
			return CreateResXFile(module, er, set, filename, string.Empty, true);
		}

		static ResourceElementSet TryCreateResourceElementSet(ModuleDef module, EmbeddedResource er) {
			if (er == null)
				return null;
			er.Data.Position = 0;
			if (!ResourceReader.CouldBeResourcesFile(er.Data))
				return null;
			try {
				er.Data.Position = 0;
				return ResourceReader.Read(module, er.Data);
			}
			catch {
				return null;
			}
		}

		public IEnumerable<IJob> GetJobs() {
			if (applicationIcon != null)
				yield return applicationIcon;
			if (applicationManifest != null)
				yield return applicationManifest;
			foreach (var f in files)
				yield return f;
		}

		public void OnWrite() {
			string asmName = options.Module.Assembly != null && options.Module.IsManifestModule ? options.Module.Assembly.Name : null;
			foreach (var bamlFile in files.OfType<BamlResourceProjectFile>()) {
				foreach (var asmRef in bamlFile.AssemblyReferences) {
					if (asmName != null && !StringComparer.Ordinal.Equals(asmName, asmRef.Name))
						extraAssemblyReferences.Add(asmRef.Name);
				}
			}
		}
	}
}
