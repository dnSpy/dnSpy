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
using dnlib.DotNet;

namespace dnSpy.Languages {
	public struct TargetFrameworkInfo {
		/// <summary>
		/// true if <see cref="Framework"/> is .NET Framework
		/// </summary>
		public bool IsDotNetFramework {
			get { return Framework == ".NETFramework"; }
		}

		/// <summary>
		/// Framework, eg. ".NETFramework". This is stored in a <c>TargetFrameworkIdentifier</c> tag
		/// in the project file.
		/// </summary>
		public string Framework { get; }

		/// <summary>
		/// Version, eg. "4.5". This is stored in a <c>TargetFrameworkVersion</c> tag
		/// in the project file.
		/// </summary>
		public string Version { get; }

		/// <summary>
		/// Profile eg. "Client" or null. This is stored in a <c>TargetFrameworkProfile</c> tag
		/// in the project file.
		/// </summary>
		public string Profile { get; }

		/// <summary>
		/// true if the info is from <see cref="T:System.Runtime.Versioning.TargetFrameworkAttribute"/>
		/// </summary>
		public bool FromAttribute { get; }

		TargetFrameworkInfo(string framework, string version, string profile, bool fromAttribute) {
			if (framework == null)
				throw new ArgumentNullException(nameof(framework));
			if (version == null)
				throw new ArgumentNullException(nameof(version));
			Framework = framework;
			Version = version;
			Profile = profile;
			FromAttribute = fromAttribute;
		}

		public static TargetFrameworkInfo Create(ModuleDef module) {
			var asm = module.Assembly;
			if (asm != null && module.IsManifestModule) {
				var info = TryGetTargetFrameworkInfoInternal(asm);
				if (info != null)
					return info.Value;
			}

			const string framework = ".NETFramework";

			if (module.IsClr10)
				return new TargetFrameworkInfo(framework, "1.0", null, false);
			if (module.IsClr11)
				return new TargetFrameworkInfo(framework, "1.1", null, false);
			if (module.IsClr20)
				return new TargetFrameworkInfo(framework, GetDotNetVersion2035(module), null, false);
			if (module.IsClr40)
				return new TargetFrameworkInfo(framework, "4.0", null, false);

			return new TargetFrameworkInfo(framework, "4.0", null, false);
		}

		static TargetFrameworkInfo? TryGetTargetFrameworkInfoInternal(AssemblyDef asm) {
			var ca = asm.CustomAttributes.Find("System.Runtime.Versioning.TargetFrameworkAttribute");
			if (ca == null)
				return null;

			if (ca.ConstructorArguments.Count != 1)
				return null;
			var arg = ca.ConstructorArguments[0];
			if (arg.Type.GetElementType() != ElementType.String)
				return null;
			var s = arg.Value as UTF8String;
			if (UTF8String.IsNullOrEmpty(s))
				return null;

			return TryCreateFromAttributeString(s);
		}

		static TargetFrameworkInfo? TryCreateFromAttributeString(string attrString) {
			// See corclr/src/mscorlib/src/System/Runtime/Versioning/BinaryCompatibility.cs
			var values = attrString.Split(new char[] { ',' });
			if (values.Length < 2 || values.Length > 3)
				return null;
			var framework = values[0].Trim();
			if (framework.Length == 0)
				return null;

			string versionStr = null;
			string profile = null;
			for (int i = 1; i < values.Length; i++) {
				var kvp = values[i].Split('=');
				if (kvp.Length != 2)
					return null;

				var key = kvp[0].Trim();
				var value = kvp[1].Trim();

				if (key.Equals("Version", StringComparison.OrdinalIgnoreCase)) {
					if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
						value = value.Substring(1);
					versionStr = value;
					Version version = null;
					if (!System.Version.TryParse(value, out version))
						return null;
				}
				else if (key.Equals("Profile", StringComparison.OrdinalIgnoreCase)) {
					if (!string.IsNullOrEmpty(value))
						profile = value;
				}
			}
			if (versionStr == null || versionStr.Length == 0)
				return null;

			return new TargetFrameworkInfo(framework, versionStr, profile, true);
		}

		static HashSet<string> dotNet30Asms = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"ComSvcConfig, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"infocard, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"Microsoft.Transactions.Bridge, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"Microsoft.Transactions.Bridge.Dtc, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"PresentationBuildTasks, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"PresentationCFFRasterizer, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"PresentationCore, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"PresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"PresentationFramework.Aero, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"PresentationFramework.Classic, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"PresentationFramework.Luna, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"PresentationFramework.Royale, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"PresentationUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"ReachFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"ServiceModelReg, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"SMSvcHost, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"System.IdentityModel, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.IdentityModel.Selectors, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.IO.Log, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"System.Printing, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"System.Runtime.Serialization, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.ServiceModel, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.ServiceModel.Install, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.ServiceModel.WasHosting, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.Speech, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"System.Workflow.Activities, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"System.Workflow.ComponentModel, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"System.Workflow.Runtime, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"UIAutomationClient, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"UIAutomationClientsideProviders, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"UIAutomationProvider, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"UIAutomationTypes, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"WindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"WindowsFormsIntegration, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"WsatConfig, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
		};

		static HashSet<string> dotNet35Asms = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"AddInProcess, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"AddInProcess32, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"AddInUtil, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"DataSvcUtil, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"EdmGen, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"Microsoft.Build.Conversion.v3.5, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"Microsoft.Build.Engine, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"Microsoft.Build.Framework, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"Microsoft.Build.Tasks.v3.5, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"Microsoft.Build.Utilities.v3.5, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"Microsoft.Data.Entity.Build.Tasks, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"Microsoft.VisualC.STLCLR, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"MSBuild, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"Sentinel.v3.5Client, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"System.AddIn, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.AddIn.Contract, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"System.ComponentModel.DataAnnotations, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.Data.DataSetExtensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.Data.Entity, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.Data.Entity.Design, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.Data.Linq, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.Data.Services, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.Data.Services.Client, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.Data.Services.Design, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.DirectoryServices.AccountManagement, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.Management.Instrumentation, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.Net, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			"System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"System.Web.Abstractions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"System.Web.DynamicData, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"System.Web.DynamicData.Design, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"System.Web.Entity, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.Web.Entity.Design, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.Web.Extensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"System.Web.Extensions.Design, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"System.Web.Routing, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"System.Windows.Presentation, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
			"System.WorkflowServices, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
			"System.Xml.Linq, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
		};

		// If input module is V35, use it, else check all its assembly references for version and
		// use highest version found. It doesn't try to check the asm refs of the asm refs, this
		// should be enough.
		static string GetDotNetVersion2035(ModuleDef module) {
			Dnr2035Version ver = Dnr2035Version.V20;

			foreach (var m in GetModules(module)) {
				ver = Max(ver, GetDotNetVersion2035Internal(m));
				if (ver == Dnr2035Version.V35)
					return ToString(ver);
			}

			return ToString(ver);
		}

		static IEnumerable<ModuleDef> GetModules(ModuleDef module) {
			yield return module;
			foreach (var asmRef in module.GetAssemblyRefs()) {
				var asm = module.Context.AssemblyResolver.Resolve(asmRef, module);
				if (asm != null)
					yield return asm.ManifestModule;
			}
		}

		enum Dnr2035Version {
			V20,
			V30,
			V35,
		}

		static Dnr2035Version Max(Dnr2035Version a, Dnr2035Version b) {
			return a > b ? a : b;
		}

		static string ToString(Dnr2035Version v) {
			switch (v) {
			case Dnr2035Version.V20: return "2.0";
			case Dnr2035Version.V30: return "3.0";
			case Dnr2035Version.V35: return "3.5";
			default: throw new InvalidOperationException();
			}
		}

		static Dnr2035Version GetDotNetVersion2035Internal(ModuleDef module) {
			var ver = Dnr2035Version.V20;
			foreach (var r in module.GetAssemblyRefs()) {
				if (dotNet35Asms.Contains(r.FullName))
					return Dnr2035Version.V35;
				if (dotNet30Asms.Contains(r.FullName))
					ver = Dnr2035Version.V30;
			}
			var asm = module.Assembly;
			if (asm != null && module.IsManifestModule) {
				if (dotNet35Asms.Contains(asm.FullName))
					return Dnr2035Version.V35;
				if (dotNet30Asms.Contains(asm.FullName))
					ver = Dnr2035Version.V30;
			}
			return ver;
		}

		string GetDisplayName() {
			if (Framework == null)
				return null;
			var name = GetFrameworkDisplayName();
			if (name == null)
				return null;

			if (!string.IsNullOrEmpty(Profile))
				name = name + " (" + Profile + ")";
			return name;
		}

		string GetFrameworkDisplayName() {
			switch (Framework) {
			case ".NETFramework":
				string v = Version;
				if (v == "4.0")
					v = "4";
				return ".NET Framework " + v;

			case ".NETPortable":
				return ".NET Portable " + Version;

			case ".NETCore":
				return ".NET Core " + Version;

			case ".NETCoreApp":
				return ".NET Core App " + Version;

			case "DNXCore":
				return "DNX Core " + Version;

			case "WindowsPhone":
				return "Windows Phone " + Version;

			case "WindowsPhoneApp":
				return "Windows Phone App " + Version;

			case "Silverlight":
				return "Silverlight " + Version;

			case "MonoAndroid":
				return "Mono Android " + Version;

			default:
				Debug.Fail("Unknown target framework: " + Framework);
				if (Framework.Length > 20)
					return null;
				return Framework + " " + Version;
			}
		}

		public override string ToString() {
			return GetDisplayName();
		}
	}
}
