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
using System.IO;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Languages;

namespace dnSpy.Languages.MSBuild {
	public sealed class ProjectModuleOptions {
		/// <summary>
		/// Module
		/// </summary>
		public ModuleDef Module { get; }

		/// <summary>
		/// Language to use
		/// </summary>
		public ILanguage Language { get; }

		/// <summary>
		/// Decompilation context
		/// </summary>
		public DecompilationContext DecompilationContext { get; }

		/// <summary>
		/// true to not reference mscorlib
		/// </summary>
		public bool DontReferenceStdLib { get; set; }

		/// <summary>
		/// Project version or null to use <see cref="ProjectCreatorOptions.ProjectVersion"/>
		/// </summary>
		public ProjectVersion? ProjectVersion { get; set; }

		/// <summary>
		/// Project guid, initialized to a random guid in the constructor
		/// </summary>
		public Guid ProjectGuid { get; set; }

		/// <summary>
		/// true to extract files from resources found in the modules. Default value is true.
		/// </summary>
		public bool UnpackResources { get; set; }

		/// <summary>
		/// true to generate .resx files from resources. Only used if <see cref="UnpackResources"/>
		/// is true. Default value is true.
		/// </summary>
		public bool CreateResX { get; set; }

		/// <summary>
		/// true to decompile baml files to xaml files. Only used if <see cref="UnpackResources"/>
		/// is true and <see cref="DecompileBaml"/> isn't null. Default value is true.
		/// </summary>
		public bool DecompileXaml { get; set; }

		/// <summary>
		/// Decompiles baml data to a <see cref="Stream"/>
		/// </summary>
		public Func<ModuleDef, byte[], CancellationToken, Stream, IList<string>> DecompileBaml;

		public ProjectModuleOptions(ModuleDef module, ILanguage language, DecompilationContext decompilationContext) {
			if (language == null || decompilationContext == null || module == null)
				throw new ArgumentNullException();
			this.Module = module;
			this.Language = language;
			this.DecompilationContext = decompilationContext;
			this.ProjectGuid = Guid.NewGuid();
			this.UnpackResources = true;
			this.CreateResX = true;
			this.DecompileXaml = true;
		}
	}
}
