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
using System.Text;
using System.Xml;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Decompiler.MSBuild {
	sealed class ProjectWriter {
		readonly Project project;
		readonly ProjectVersion projectVersion;
		readonly IList<Project> allProjects;
		readonly IList<string> userGACPaths;

		public ProjectWriter(Project project, ProjectVersion projectVersion, IList<Project> allProjects, IList<string> userGACPaths) {
			this.project = project;
			this.projectVersion = projectVersion;
			this.allProjects = allProjects;
			this.userGACPaths = userGACPaths;
		}

		public void Write() {
			project.OnWrite();
			var settings = new XmlWriterSettings {
				Encoding = Encoding.UTF8,
				Indent = true,
			};
			if (projectVersion == ProjectVersion.VS2005)
				settings.OmitXmlDeclaration = true;
			using (var writer = XmlWriter.Create(project.Filename, settings)) {
				project.Platform = GetPlatformString();

				writer.WriteStartDocument();
				writer.WriteStartElement("Project", "http://schemas.microsoft.com/developer/msbuild/2003");
				var toolsVersion = GetToolsVersion();
				if (toolsVersion is not null)
					writer.WriteAttributeString("ToolsVersion", toolsVersion);
				if (projectVersion <= ProjectVersion.VS2015)
					writer.WriteAttributeString("DefaultTargets", "Build");
				if (projectVersion >= ProjectVersion.VS2012) {
					writer.WriteStartElement("Import");
					writer.WriteAttributeString("Project", @"$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props");
					writer.WriteAttributeString("Condition", @"Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')");
					writer.WriteEndElement();
				}

				// Default property group
				writer.WriteStartElement("PropertyGroup");

				writer.WriteStartElement("Configuration");
				writer.WriteAttributeString("Condition", " '$(Configuration)' == '' ");
				writer.WriteString("Debug");
				writer.WriteEndElement();

				writer.WriteStartElement("Platform");
				writer.WriteAttributeString("Condition", " '$(Platform)' == '' ");
				writer.WriteString(project.Platform);
				writer.WriteEndElement();

				writer.WriteElementString("ProjectGuid", project.Guid.ToString("B").ToUpperInvariant());
				writer.WriteElementString("OutputType", GetOutputType());
				var appDesignFolder = GetAppDesignerFolder();
				if (appDesignFolder is not null)
					writer.WriteElementString("AppDesignerFolder", appDesignFolder);
				writer.WriteElementString("RootNamespace", GetRootNamespace());
				var asmName = GetAssemblyName();
				if (!string.IsNullOrEmpty(asmName))
					writer.WriteElementString("AssemblyName", GetAssemblyName());
				var fwkInfo = TargetFrameworkInfo.Create(project.Module);
				if (projectVersion > ProjectVersion.VS2005 || !fwkInfo.IsDotNetFramework || fwkInfo.Version != "2.0")
					writer.WriteElementString("TargetFrameworkVersion", "v" + fwkInfo.Version);
				if (!string.IsNullOrEmpty(fwkInfo.Profile))
					writer.WriteElementString("TargetFrameworkProfile", fwkInfo.Profile);
				if (!fwkInfo.IsDotNetFramework)
					writer.WriteElementString("TargetFrameworkIdentifier", fwkInfo.Framework);
				writer.WriteElementString("FileAlignment", GetFileAlignment());
				if (project.ProjectTypeGuids.Count != 0) {
					var text = string.Join(";", project.ProjectTypeGuids.Select(a => a.ToString("B").ToUpperInvariant()).ToArray());
					writer.WriteElementString("ProjectTypeGuids", text);
				}
				//TODO: VB includes a "MyType"
				if (project.ApplicationManifest is not null)
					writer.WriteElementString("ApplicationManifest", GetRelativePath(project.ApplicationManifest.Filename));
				if (project.ApplicationIcon is not null)
					writer.WriteElementString("ApplicationIcon", GetRelativePath(project.ApplicationIcon.Filename));
				if (project.StartupObject is not null)
					writer.WriteElementString("StartupObject", project.StartupObject);
				writer.WriteEndElement();

				// Debug property group
				var noWarnList = GetNoWarnList();
				writer.WriteStartElement("PropertyGroup");
				writer.WriteAttributeString("Condition", $" '$(Configuration)|$(Platform)' == 'Debug|{project.Platform}' ");
				writer.WriteElementString("PlatformTarget", project.Platform);
				writer.WriteElementString("DebugSymbols", "true");
				writer.WriteElementString("DebugType", "full");
				writer.WriteElementString("Optimize", "false");
				writer.WriteElementString("OutputPath", @"bin\Debug\");
				writer.WriteElementString("DefineConstants", "DEBUG;TRACE");
				writer.WriteElementString("ErrorReport", "prompt");
				writer.WriteElementString("WarningLevel", "4");
				if (project.Options.DontReferenceStdLib)
					writer.WriteElementString("NoStdLib", "true");
				if (project.AllowUnsafeBlocks)
					writer.WriteElementString("AllowUnsafeBlocks", "true");
				if (noWarnList is not null)
					writer.WriteElementString("NoWarn", noWarnList);
				writer.WriteEndElement();

				// Release property group
				writer.WriteStartElement("PropertyGroup");
				writer.WriteAttributeString("Condition", $" '$(Configuration)|$(Platform)' == 'Release|{project.Platform}' ");
				writer.WriteElementString("PlatformTarget", project.Platform);
				writer.WriteElementString("DebugType", "pdbonly");
				writer.WriteElementString("Optimize", "true");
				writer.WriteElementString("OutputPath", @"bin\Release\");
				writer.WriteElementString("DefineConstants", "TRACE");
				writer.WriteElementString("ErrorReport", "prompt");
				writer.WriteElementString("WarningLevel", "4");
				if (project.Options.DontReferenceStdLib)
					writer.WriteElementString("NoStdLib", "true");
				if (project.AllowUnsafeBlocks)
					writer.WriteElementString("AllowUnsafeBlocks", "true");
				if (noWarnList is not null)
					writer.WriteElementString("NoWarn", noWarnList);
				writer.WriteEndElement();

				// GAC references
				var gacRefs = project.Module.GetAssemblyRefs().Where(a => a.Name != "mscorlib").OrderBy(a => a.Name.String, StringComparer.OrdinalIgnoreCase).ToArray();
				if (gacRefs.Length > 0 || project.ExtraAssemblyReferences.Count > 0) {
					writer.WriteStartElement("ItemGroup");
					var hash = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					foreach (var r in gacRefs) {
						var asm = project.Module.Context.AssemblyResolver.Resolve(r, project.Module);
						if (asm is not null && ExistsInProject(asm.ManifestModule.Location))
							continue;
						hash.Add(r.Name);
						writer.WriteStartElement("Reference");
						writer.WriteAttributeString("Include", IdentifierEscaper.Escape(r.Name));
						var hintPath = GetHintPath(asm);
						if (hintPath is not null)
							writer.WriteElementString("HintPath", hintPath);
						writer.WriteEndElement();
					}
					foreach (var r in project.ExtraAssemblyReferences) {
						if (hash.Contains(r) || AssemblyExistsInProject(r))
							continue;
						hash.Add(r);
						writer.WriteStartElement("Reference");
						writer.WriteAttributeString("Include", IdentifierEscaper.Escape(r));
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
				}

				writer.WriteStartElement("ItemGroup");
				writer.WriteStartElement("AppDesigner");
				writer.WriteAttributeString("Include", project.PropertiesFolder + "\\");
				writer.WriteEndElement();
				writer.WriteEndElement();

				Write(writer, BuildAction.Compile);
				Write(writer, BuildAction.EmbeddedResource);

				// Project references
				var projRefs = project.Module.GetAssemblyRefs().
					Select(a => project.Module.Context.AssemblyResolver.Resolve(a, project.Module)).
					Select(a => a is null ? null : FindOtherProject(a.ManifestModule.Location)).
					OfType<Project>().OrderBy(a => a.Filename, StringComparer.OrdinalIgnoreCase).ToArray();
				if (projRefs.Length > 0) {
					writer.WriteStartElement("ItemGroup");
					foreach (var otherProj in projRefs) {
						writer.WriteStartElement("ProjectReference");
						writer.WriteAttributeString("Include", GetRelativePath(otherProj.Filename));
						writer.WriteStartElement("Project");
						var guidString = otherProj.Guid.ToString("B");
						if (projectVersion < ProjectVersion.VS2012)
							guidString = guidString.ToUpperInvariant();
						writer.WriteString(guidString);
						writer.WriteEndElement();
						writer.WriteStartElement("Name");
						writer.WriteString(IdentifierEscaper.Escape(otherProj.Module.Assembly is null ? string.Empty : otherProj.Module.Assembly.Name.String));
						writer.WriteEndElement();
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
				}

				Write(writer, BuildAction.None);
				Write(writer, BuildAction.ApplicationDefinition);
				Write(writer, BuildAction.Page);
				Write(writer, BuildAction.Resource);
				Write(writer, BuildAction.SplashScreen);

				writer.WriteStartElement("Import");
				writer.WriteAttributeString("Project", GetLanguageTargets());
				writer.WriteEndElement();

				writer.WriteEndElement();
				writer.WriteEndDocument();
			}
		}

		static string GetRelativePath(string sourceDir, string destFile) {
			var s = FilenameUtils.GetRelativePath(sourceDir, destFile);
			if (Path.DirectorySeparatorChar != '\\')
				s = s.Replace(Path.DirectorySeparatorChar, '\\');
			if (Path.AltDirectorySeparatorChar != '\\')
				s = s.Replace(Path.AltDirectorySeparatorChar, '\\');
			return s;
		}

		string GetRelativePath(string filename) => GetRelativePath(project.Directory, filename);

		void Write(XmlWriter writer, BuildAction buildAction) {
			var files = project.Files.Where(a => a.BuildAction == buildAction).OrderBy(a => a.Filename, StringComparer.OrdinalIgnoreCase).ToArray();
			if (files.Length == 0)
				return;
			writer.WriteStartElement("ItemGroup");
			foreach (var file in files) {
				if (file.BuildAction == BuildAction.DontIncludeInProjectFile)
					continue;
				writer.WriteStartElement(ToString(buildAction));
				writer.WriteAttributeString("Include", GetRelativePath(file.Filename));
				if (file.DependentUpon is not null)
					writer.WriteElementString("DependentUpon", GetRelativePath(Path.GetDirectoryName(file.Filename)!, file.DependentUpon.Filename));
				if (file.SubType is not null)
					writer.WriteElementString("SubType", file.SubType);
				if (file.Generator is not null)
					writer.WriteElementString("Generator", file.Generator);
				if (file.LastGenOutput is not null)
					writer.WriteElementString("LastGenOutput", GetRelativePath(Path.GetDirectoryName(file.Filename)!, file.LastGenOutput.Filename));
				if (file.AutoGen)
					writer.WriteElementString("AutoGen", "True");
				if (file.DesignTime)
					writer.WriteElementString("DesignTime", "True");
				if (file.DesignTimeSharedInput)
					writer.WriteElementString("DesignTimeSharedInput", "True");
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}

		static string ToString(BuildAction buildAction) {
			switch (buildAction) {
			case BuildAction.None:					return "None";
			case BuildAction.Compile:				return "Compile";
			case BuildAction.EmbeddedResource:		return "EmbeddedResource";
			case BuildAction.ApplicationDefinition:	return "ApplicationDefinition";
			case BuildAction.Page:					return "Page";
			case BuildAction.Resource:				return "Resource";
			case BuildAction.SplashScreen:			return "SplashScreen";
			default: throw new InvalidOperationException();
			}
		}

		string? GetToolsVersion() {
			switch (projectVersion) {
			case ProjectVersion.VS2005: return null;
			case ProjectVersion.VS2008: return "3.5";
			case ProjectVersion.VS2010: return "4.0";
			case ProjectVersion.VS2012: return "4.0";
			case ProjectVersion.VS2013: return "12.0";
			case ProjectVersion.VS2015: return "14.0";
			case ProjectVersion.VS2017: return "15.0";
			case ProjectVersion.VS2019: return "15.0";
			default: throw new InvalidOperationException();
			}
		}

		string GetPlatformString() {
			var machine = project.Module.Machine;
			if (machine.IsI386()) {
				int c = (project.Module.Is32BitRequired ? 2 : 0) + (project.Module.Is32BitPreferred ? 1 : 0);
				switch (c) {
				case 0: // no special meaning, MachineType and ILONLY flag determine image requirements
					if (!project.Module.IsILOnly)
						return "x86";
					return "AnyCPU";
				case 1: // illegal, reserved for future use
					break;
				case 2: // image is x86-specific
					return "x86";
				case 3: // image is project.Platform neutral and prefers to be loaded 32-bit when possible
					return "AnyCPU";
				}
				return "AnyCPU";
			}
			else if (machine.IsAMD64())
				return "x64";
			else if (machine == Machine.IA64)
				return "Itanium";
			else if (machine.IsARMNT())
				return "ARM";
			else if (machine.IsARM64())
				return "ARM64";
			else {
				Debug.Fail("Unknown machine");
				return machine.ToString();
			}
		}

		string GetOutputType() {
			if (project.Module.IsWinMD)
				return "WinMDObj";
			switch (project.Module.Kind) {
			case ModuleKind.Console:	return "Exe";
			case ModuleKind.Windows:	return "WinExe";
			case ModuleKind.Dll:		return "Library";
			case ModuleKind.NetModule:	return "Module";

			default:
				Debug.Fail("Unknown module kind: " + project.Module.Kind);
				return "Library";
			}
		}

		string? GetAppDesignerFolder() {
			if (project.Options.Decompiler.GenericGuid == DecompilerConstants.LANGUAGE_VISUALBASIC)
				return null;
			if (projectVersion >= ProjectVersion.VS2017)
				return null;
			return project.PropertiesFolder;
		}

		string? GetNoWarnList() {
			if (project.Options.Decompiler.GenericGuid == DecompilerConstants.LANGUAGE_VISUALBASIC)
				return "41999,42016,42017,42018,42019,42020,42021,42022,42032,42036,42314";
			return null;
		}

		string GetRootNamespace() {
			if (!string.IsNullOrEmpty(project.DefaultNamespace))
				return project.DefaultNamespace;
			return GetAssemblyName();
		}

		string GetAssemblyName() => project.AssemblyName;

		string GetFileAlignment() {
			if (project.Module is ModuleDefMD mod)
				return mod.Metadata.PEImage.ImageNTHeaders.OptionalHeader.FileAlignment.ToString();
			return "512";
		}

		string GetLanguageTargets() {
			if (project.Options.Decompiler.GenericGuid == DecompilerConstants.LANGUAGE_CSHARP)
				return @"$(MSBuildToolsPath)\Microsoft.CSharp.targets";
			if (project.Options.Decompiler.GenericGuid == DecompilerConstants.LANGUAGE_VISUALBASIC)
				return @"$(MSBuildToolsPath)\Microsoft.VisualBasic.targets";
			return @"$(MSBuildToolsPath)\Microsoft.CSharp.targets";
		}

		string? GetHintPath(AssemblyDef? asm) {
			if (asm is null)
				return null;
			if (IsGacPath(asm.ManifestModule.Location))
				return null;
			if (ExistsInProject(asm.ManifestModule.Location))
				return null;

			return GetRelativePath(asm.ManifestModule.Location);
		}

		bool IsGacPath(string file) => GacInfo.IsGacPath(file) || IsUserGacPath(file);

		bool IsUserGacPath(string file) {
			file = file.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			foreach (var dir in userGACPaths) {
				if (file.StartsWith(dir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		bool ExistsInProject(string filename) => FindOtherProject(filename) is not null;
		bool AssemblyExistsInProject(string asmSimpleName) =>
			allProjects.Any(a => StringComparer.OrdinalIgnoreCase.Equals(a.AssemblyName, asmSimpleName));
		Project? FindOtherProject(string filename) =>
			allProjects.FirstOrDefault(f => StringComparer.OrdinalIgnoreCase.Equals(Path.GetFullPath(f.Module.Location), Path.GetFullPath(filename)));
	}
}
