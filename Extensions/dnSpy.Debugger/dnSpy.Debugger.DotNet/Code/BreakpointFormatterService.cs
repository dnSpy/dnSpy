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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger.DotNet.Code {
	abstract class BreakpointFormatterService {
		public abstract DbgBreakpointLocationFormatterImpl Create(DbgDotNetCodeLocation location);
	}

	[Export(typeof(BreakpointFormatterService))]
	sealed class BreakpointFormatterServiceImpl : BreakpointFormatterService {
		readonly DbgDotNetDecompilerService dbgDotNetDecompilerService;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly Lazy<DbgMetadataService> dbgMetadataService;

		internal IDecompiler MethodDecompiler => dbgDotNetDecompilerService.Decompiler;

		[ImportingConstructor]
		BreakpointFormatterServiceImpl(DbgDotNetDecompilerService dbgDotNetDecompilerService, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, Lazy<DbgMetadataService> dbgMetadataService) {
			this.dbgDotNetDecompilerService = dbgDotNetDecompilerService;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			this.dbgMetadataService = dbgMetadataService;
			dbgDotNetDecompilerService.DecompilerChanged += DbgDotNetDecompilerService_DecompilerChanged;
		}

		void DbgDotNetDecompilerService_DecompilerChanged(object sender, EventArgs e) {
			foreach (var bp in dbgCodeBreakpointsService.Value.Breakpoints) {
				if (bp.IsHidden)
					continue;
				var formatter = (bp.Location as DbgDotNetCodeLocationImpl)?.Formatter;
				formatter?.RefreshName();
			}
		}

		public override DbgBreakpointLocationFormatterImpl Create(DbgDotNetCodeLocation location) =>
			new DbgBreakpointLocationFormatterImpl(this, (DbgDotNetCodeLocationImpl)location);

		internal TDef GetDefinition<TDef>(ModuleId module, uint token) where TDef : class {
			var md = dbgMetadataService.Value.TryGetMetadata(module, DbgLoadModuleOptions.AutoLoaded);
			return md?.ResolveToken(token) as TDef;
		}
	}
}
