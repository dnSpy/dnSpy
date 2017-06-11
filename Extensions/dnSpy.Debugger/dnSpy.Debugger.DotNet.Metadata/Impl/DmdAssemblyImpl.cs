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
using System.IO;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdAssemblyImpl : DmdAssembly {
		internal sealed override void YouCantDeriveFromThisClass() => throw new InvalidOperationException();
		public override DmdAppDomain AppDomain => appDomain;
		public override string Location { get; }
		public override string ImageRuntimeVersion => metadataReader.ImageRuntimeVersion;
		public override DmdMethodInfo EntryPoint => metadataReader.EntryPoint;
		public override bool GlobalAssemblyCache => throw new NotImplementedException();//TODO:

		internal string ApproximateSimpleName {
			get {
				if (approximateSimpleName == null)
					approximateSimpleName = CalculateApproximateSimpleName();
				return approximateSimpleName;
			}
		}
		string approximateSimpleName;
		string CalculateApproximateSimpleName() {
			if (IsInMemory || IsDynamic)
				return string.Empty;
			try {
				return Path.GetFileNameWithoutExtension(Location);
			}
			catch (ArgumentException) {
			}
			return string.Empty;
		}

		public override DmdModule ManifestModule {
			get {
				lock (LockObject)
					return modules.Count == 0 ? null : modules[0];
			}
		}

		readonly DmdAppDomainImpl appDomain;
		readonly List<DmdModuleImpl> modules;
		readonly DmdMetadataReader metadataReader;

		public DmdAssemblyImpl(DmdAppDomainImpl appDomain, DmdMetadataReader metadataReader, string location) {
			modules = new List<DmdModuleImpl>();
			this.appDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
			this.metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
			Location = location ?? throw new ArgumentNullException(nameof(location));
		}

		internal void Add(DmdModuleImpl module) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			lock (LockObject) {
				Debug.Assert(!modules.Contains(module));
				modules.Add(module);
			}
		}

		internal void Remove(DmdModuleImpl module) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			lock (LockObject) {
				bool b = modules.Remove(module);
				Debug.Assert(b);
			}
		}

		public override DmdModule[] GetModules() => GetLoadedModules();
		public override DmdModule[] GetLoadedModules() {
			lock (LockObject)
				return modules.ToArray();
		}

		public override DmdModule GetModule(string name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			lock (LockObject) {
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

		public override DmdType[] GetExportedTypes() {
			var list = new List<DmdType>();
			foreach (var type in metadataReader.GetTypes()) {
				if (type.IsVisible)
					list.Add(type);
			}
			foreach (var type in metadataReader.GetExportedTypes()) {
				if (IsNotTypeForwarder(type))
					list.Add(type);
			}
			return list.ToArray();
		}

		static bool IsNotTypeForwarder(DmdType type) {
			var nonNested = DmdTypeUtilities.GetNonNestedType(type);
			if ((object)nonNested == null)
				return false;
			return nonNested.TypeScope.Kind == DmdTypeScopeKind.ModuleRef;
		}

		public override DmdAssemblyName[] GetReferencedAssemblies() => metadataReader.GetReferencedAssemblies();
		internal DmdType GetType(DmdType typeRef) => appDomain.TryLookup(this, typeRef);
		public override DmdType GetType(string name, bool throwOnError, bool ignoreCase) => throw new NotImplementedException();//TODO:
		public override IList<DmdCustomAttributeData> GetCustomAttributesData() => throw new NotImplementedException();//TODO:
	}
}
