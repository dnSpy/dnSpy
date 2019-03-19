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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Debugger.DotNet.Code {
	[Export(typeof(DbgDotNetDecompilerService))]
	sealed class DbgDotNetDecompilerServiceImpl : DbgDotNetDecompilerService {
		readonly IDecompilerService decompilerService;
		readonly Lazy<DbgDotNetDecompilerGuidProvider, IDbgDotNetDecompilerGuidProviderMetadata>[] dbgDotNetDecompilerGuidProviders;

		public override event EventHandler<EventArgs> DecompilerChanged;

		public override IDecompiler Decompiler => decompiler;
		IDecompiler decompiler;

		[ImportingConstructor]
		DbgDotNetDecompilerServiceImpl(IDecompilerService decompilerService, DbgLanguageService dbgLanguageService, [ImportMany] IEnumerable<Lazy<DbgDotNetDecompilerGuidProvider, IDbgDotNetDecompilerGuidProviderMetadata>> dbgDotNetDecompilerGuidProviders) {
			this.decompilerService = decompilerService;
			this.dbgDotNetDecompilerGuidProviders = dbgDotNetDecompilerGuidProviders.OrderBy(a => a.Metadata.Order).ToArray();
			dbgLanguageService.LanguageChanged += DbgLanguageService_LanguageChanged;
			SetDecompiler(dbgLanguageService.GetCurrentLanguage(PredefinedDbgRuntimeKindGuids.DotNet_Guid));
		}

		void DbgLanguageService_LanguageChanged(object sender, DbgLanguageChangedEventArgs e) {
			if (e.RuntimeKindGuid == PredefinedDbgRuntimeKindGuids.DotNet_Guid)
				SetDecompiler(e.Language);
		}

		Guid GetDecompilerGuid(DbgLanguage language) {
			foreach (var lz in dbgDotNetDecompilerGuidProviders) {
				var guid = lz.Value.GetDecompilerGuid(language);
				if (guid != null)
					return guid.Value;
			}
			return DecompilerConstants.LANGUAGE_CSHARP;
		}

		void SetDecompiler(DbgLanguage language) => SetDecompiler(decompilerService.FindOrDefault(GetDecompilerGuid(language)));

		void SetDecompiler(IDecompiler newDecompiler) {
			if (newDecompiler == null)
				throw new ArgumentNullException(nameof(newDecompiler));
			if (decompiler == newDecompiler)
				return;
			decompiler = newDecompiler;
			DecompilerChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
