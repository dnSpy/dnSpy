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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Engine;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Resources;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	[Export(typeof(DbgDotNetLanguageService))]
	sealed class DbgDotNetLanguageServiceImpl : DbgDotNetLanguageService {
		readonly Lazy<DbgDotNetExpressionCompiler, IDbgDotNetExpressionCompilerMetadata>[] dbgDotNetExpressionCompilers;
		readonly IDecompilerService decompilerService;
		readonly Dictionary<Guid, Lazy<DbgDotNetFormatter, IDbgDotNetFormatterMetadata>> formattersDict;

		static readonly Guid csharpDecompilerGuid = new Guid(PredefinedDecompilerGuids.CSharp);
		static readonly Guid visualBasicDecompilerGuid = new Guid(PredefinedDecompilerGuids.VisualBasic);
		static readonly Guid defaultLanguageGuid = new Guid(DbgDotNetLanguageGuids.CSharp);

		[ImportingConstructor]
		DbgDotNetLanguageServiceImpl([ImportMany] IEnumerable<Lazy<DbgDotNetExpressionCompiler, IDbgDotNetExpressionCompilerMetadata>> dbgDotNetExpressionCompilers, IDecompilerService decompilerService, [ImportMany] IEnumerable<Lazy<DbgDotNetFormatter, IDbgDotNetFormatterMetadata>> dbgDotNetFormatters) {
			this.decompilerService = decompilerService;
			var eeList = new List<Lazy<DbgDotNetExpressionCompiler, IDbgDotNetExpressionCompilerMetadata>>();
			var langGuids = new HashSet<Guid>();
			foreach (var lz in dbgDotNetExpressionCompilers.OrderBy(a => a.Metadata.Order)) {
				if (!Guid.TryParse(lz.Metadata.LanguageGuid, out var languageGuid)) {
					Debug.Fail($"Couldn't parse language GUID: '{lz.Metadata.LanguageGuid}'");
					continue;
				}
				if (!Guid.TryParse(lz.Metadata.DecompilerGuid, out _)) {
					Debug.Fail($"Couldn't parse decompiler GUID: '{lz.Metadata.DecompilerGuid}'");
					continue;
				}
				if (langGuids.Add(languageGuid))
					eeList.Add(lz);
			}
			this.dbgDotNetExpressionCompilers = eeList.ToArray();

			formattersDict = new Dictionary<Guid, Lazy<DbgDotNetFormatter, IDbgDotNetFormatterMetadata>>();
			foreach (var lz in dbgDotNetFormatters.OrderBy(a => a.Metadata.Order)) {
				if (!Guid.TryParse(lz.Metadata.LanguageGuid, out var languageGuid)) {
					Debug.Fail($"Couldn't parse language GUID: '{lz.Metadata.LanguageGuid}'");
					continue;
				}
				if (!formattersDict.ContainsKey(languageGuid))
					formattersDict.Add(languageGuid, lz);
			}
		}

		public override IEnumerable<DbgEngineLanguage> CreateLanguages(Guid runtimeGuid) {
			foreach (var lz in dbgDotNetExpressionCompilers) {
				bool b = Guid.TryParse(lz.Metadata.DecompilerGuid, out var decompilerGuid);
				Debug.Assert(b);
				if (!b)
					continue;

				if (!TryGetFormatter(lz.Metadata.LanguageGuid, out var formatter))
					continue;

				if (decompilerGuid == csharpDecompilerGuid)
					decompilerGuid = DecompilerConstants.LANGUAGE_CSHARP;
				else if (decompilerGuid == visualBasicDecompilerGuid)
					decompilerGuid = DecompilerConstants.LANGUAGE_VISUALBASIC;
				var decompiler = decompilerService.Find(decompilerGuid);
				Debug.Assert(decompiler != null);
				if (decompiler == null)
					continue;

				var languageDisplayName = ResourceHelper.GetString(lz.Value, lz.Metadata.LanguageDisplayName);
				yield return new DbgEngineLanguageImpl(lz.Metadata.LanguageName, languageDisplayName, lz.Value, decompiler, formatter.Value);
			}
		}

		bool TryGetFormatter(string guidString, out Lazy<DbgDotNetFormatter, IDbgDotNetFormatterMetadata> formatter) {
			formatter = null;
			bool b = Guid.TryParse(guidString, out var languageGuid);
			Debug.Assert(b);
			if (!b)
				return false;

			if (formattersDict.TryGetValue(languageGuid, out formatter))
				return true;
			if (formattersDict.TryGetValue(defaultLanguageGuid, out formatter))
				return true;

			Debug.Fail($"Default formatter ({defaultLanguageGuid.ToString()}) wasn't exported");
			formatter = formattersDict.Values.FirstOrDefault();
			return formatter != null;
		}
	}
}
