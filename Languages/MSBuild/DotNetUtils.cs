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
using System.Linq;
using System.Text.RegularExpressions;
using dnlib.DotNet;

namespace dnSpy.Languages.MSBuild {
	static class DotNetUtils {
		/// <summary>
		/// Gets the .NET Framework version as a string, eg. "v1.1", "v4.0", "v4.5.1"
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="profile">Updated with profile, eg. "Client" or null if none</param>
		/// <returns></returns>
		public static string GetDotNetVersion(this ModuleDef module, out string profile) {
			profile = null;
			var asm = module.Assembly;
			if (asm != null && module.IsManifestModule) {
				var asmNetVer = GetDotNetVersion(asm, out profile);
				if (asmNetVer != null)
					return asmNetVer;
			}

			if (module.IsClr10)
				return "v1.0";
			if (module.IsClr11)
				return "v1.1";
			if (module.IsClr20)
				return GetDotNetVersion2035(module);
			if (module.IsClr40)
				return "v4.0";

			return "v4.0";
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
			case Dnr2035Version.V20: return "v2.0";
			case Dnr2035Version.V30: return "v3.0";
			case Dnr2035Version.V35: return "v3.5";
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

		static string GetDotNetVersion(AssemblyDef asm, out string profile) {
			profile = null;
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
			var values = s.String.Split(new char[] { ',' });
			bool isDNF = values.Any(a => StringComparer.OrdinalIgnoreCase.Equals(a, ".NETFramework"));
			const string VERSION_PREFIX = "Version=v";
			var verString = values.FirstOrDefault(a => a.StartsWith(VERSION_PREFIX, StringComparison.OrdinalIgnoreCase));
			if (isDNF && verString != null) {
				var ver = verString.Substring(VERSION_PREFIX.Length);
				var match = new Regex(@"^\d+(?:\.\d+(?:\.\d+(?:\.\d+)?)?)?$").Match(ver);
				// "65535.65535.65535.65535"
				if (match.Success && ver.Length <= 5 * 4 + 3) {
					const string PROFILE_PREFIX = "Profile=";
					var pv = values.FirstOrDefault(a => a.StartsWith(PROFILE_PREFIX, StringComparison.Ordinal));
					if (pv != null)
						profile = pv.Substring(PROFILE_PREFIX.Length).Trim();
					return "v" + ver;
				}
			}

			return null;
		}

		static bool IsType(TypeDef type, string typeFullName) {
			while (type != null) {
				var bt = type.BaseType;
				if (bt == null)
					break;
				if (bt.FullName == typeFullName)
					return true;
				type = bt.ResolveTypeDef();
			}
			return false;
		}

		public static bool IsWinForm(TypeDef type) {
			return IsType(type, "System.Windows.Forms.Form");
		}

		public static bool IsSystemWindowsApplication(TypeDef type) {
			return IsType(type, "System.Windows.Application");
		}

		public static bool IsStartUpClass(TypeDef type) {
			return type.Module.EntryPoint != null &&
				type.Module.EntryPoint.DeclaringType == type;
		}

		public static bool IsUnsafe(ModuleDef module) {
			return module.CustomAttributes.IsDefined("System.Security.UnverifiableCodeAttribute");
		}

		public static IEnumerable<FieldDef> GetFields(MethodDef method) {
			return GetDefs(method).OfType<FieldDef>();
		}

		public static IEnumerable<IMemberDef> GetDefs(MethodDef method) {
			var body = method.Body;
			if (body != null) {
				foreach (var instr in body.Instructions) {
					var def = instr.Operand as IMemberDef;
					if (def != null && def.DeclaringType == method.DeclaringType)
						yield return def;
				}
			}
		}

		public static IEnumerable<IMemberDef> GetDefs(PropertyDef prop) {
			foreach (var g in prop.GetMethods) {
				foreach (var d in GetDefs(g))
					yield return d;
			}
		}

		public static IEnumerable<IMemberDef> GetMethodsAndSelf(PropertyDef p) {
			yield return p;
			foreach (var m in p.GetMethods)
				yield return m;
			foreach (var m in p.SetMethods)
				yield return m;
			foreach (var m in p.OtherMethods)
				yield return m;
		}
	}
}
