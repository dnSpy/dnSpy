/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdAssemblyImpl : DmdAssembly {
		public override DmdAppDomain AppDomain { get; }
		public override string Location { get; }
		public override string ImageRuntimeVersion => metadataReader.ImageRuntimeVersion;
		public override DmdMethodInfo EntryPoint => metadataReader.EntryPoint;
		public override string FullName => throw new NotImplementedException();//TODO:
		public override bool GlobalAssemblyCache => throw new NotImplementedException();//TODO:

		public override DmdModule ManifestModule {
			get {
				lock (lockObj)
					return modules.Count == 0 ? null : modules[0];
			}
		}

		readonly object lockObj;
		readonly List<DmdModuleImpl> modules;
		readonly DmdMetadataReader metadataReader;

		public DmdAssemblyImpl(DmdAppDomainImpl appDomain, DmdMetadataReader metadataReader, string location) {
			lockObj = new object();
			modules = new List<DmdModuleImpl>();
			AppDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
			this.metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
			Location = location ?? throw new ArgumentNullException(nameof(location));
		}

		internal void Add(DmdModuleImpl module) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			lock (lockObj) {
				Debug.Assert(!modules.Contains(module));
				modules.Add(module);
			}
		}

		internal void Remove(DmdModuleImpl module) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			lock (lockObj) {
				bool b = modules.Remove(module);
				Debug.Assert(b);
			}
		}

		public override DmdModule[] GetModules() => GetLoadedModules();
		public override DmdModule[] GetLoadedModules() {
			lock (lockObj)
				return modules.ToArray();
		}

		public override DmdModule GetModule(string name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			lock (lockObj) {
				foreach (var module in modules) {
					// This is case insensitive, see AssemblyNative::GetModule(pAssembly,wszFileName,retModule) in coreclr/src/vm/assemblynative.cpp
					if (StringComparer.OrdinalIgnoreCase.Equals(module.ScopeName, name))
						return module;
				}
			}
			return null;
		}

		public override DmdAssemblyName GetName() => (asmName ?? (asmName = metadataReader.GetName())).Clone();
		DmdAssemblyName asmName;

		public override DmdType[] GetExportedTypes() => metadataReader.GetExportedTypes();
		public override DmdAssemblyName[] GetReferencedAssemblies() => metadataReader.GetReferencedAssemblies();
		public override DmdType GetType(string name, bool throwOnError, bool ignoreCase) => throw new NotImplementedException();//TODO:
		public override IList<DmdCustomAttributeData> GetCustomAttributesData() => throw new NotImplementedException();//TODO:
		public override bool IsDefined(string attributeTypeFullName, bool inherit) => throw new NotImplementedException();//TODO:
		public override bool IsDefined(DmdType attributeType, bool inherit) => throw new NotImplementedException();//TODO:
	}
}
