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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Engine;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation.Internal;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Resources;
using dnSpy.Debugger.DotNet.Code;
using dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	abstract class DbgDotNetLanguageService2 : DbgDotNetLanguageService {
		public abstract IEnumerable<DbgEngineLanguage> CreateLanguages();
	}

	[Export(typeof(DbgDotNetLanguageService))]
	[Export(typeof(DbgDotNetLanguageService2))]
	sealed class DbgDotNetLanguageServiceImpl : DbgDotNetLanguageService2 {
		readonly Lazy<DbgMethodDebugInfoProvider> dbgMethodDebugInfoProvider;
		readonly Lazy<DbgModuleReferenceProvider> dbgModuleReferenceProvider;
		readonly Lazy<DbgDotNetEngineValueNodeFactoryService> dbgDotNetEngineValueNodeFactoryService;
		readonly Lazy<DbgDotNetILInterpreter> dnILInterpreter;
		readonly Lazy<DbgAliasProvider> dbgAliasProvider;
		readonly Lazy<DbgDotNetExpressionCompiler, IDbgDotNetExpressionCompilerMetadata>[] dbgDotNetExpressionCompilers;
		readonly IDecompilerService decompilerService;
		readonly IPredefinedEvaluationErrorMessagesHelper predefinedEvaluationErrorMessagesHelper;
		readonly Dictionary<Guid, Lazy<DbgDotNetFormatter, IDbgDotNetFormatterMetadata>> formattersDict;

		static readonly Guid csharpDecompilerGuid = new Guid(PredefinedDecompilerGuids.CSharp);
		static readonly Guid visualBasicDecompilerGuid = new Guid(PredefinedDecompilerGuids.VisualBasic);

		[ImportingConstructor]
		DbgDotNetLanguageServiceImpl(Lazy<DbgMethodDebugInfoProvider> dbgMethodDebugInfoProvider, Lazy<DbgModuleReferenceProvider> dbgModuleReferenceProvider, Lazy<DbgDotNetEngineValueNodeFactoryService> dbgDotNetEngineValueNodeFactoryService, Lazy<DbgDotNetILInterpreter> dnILInterpreter, Lazy<DbgAliasProvider> dbgAliasProvider, [ImportMany] IEnumerable<Lazy<DbgDotNetExpressionCompiler, IDbgDotNetExpressionCompilerMetadata>> dbgDotNetExpressionCompilers, IDecompilerService decompilerService, IPredefinedEvaluationErrorMessagesHelper predefinedEvaluationErrorMessagesHelper, [ImportMany] IEnumerable<Lazy<DbgDotNetFormatter, IDbgDotNetFormatterMetadata>> dbgDotNetFormatters) {
			this.dbgMethodDebugInfoProvider = dbgMethodDebugInfoProvider;
			this.dbgModuleReferenceProvider = dbgModuleReferenceProvider;
			this.dbgDotNetEngineValueNodeFactoryService = dbgDotNetEngineValueNodeFactoryService;
			this.dnILInterpreter = dnILInterpreter;
			this.dbgAliasProvider = dbgAliasProvider;
			this.decompilerService = decompilerService;
			this.predefinedEvaluationErrorMessagesHelper = predefinedEvaluationErrorMessagesHelper;
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

		public override IEnumerable<DbgEngineLanguage> CreateLanguages() {
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

				var valueNodeFactory = dbgDotNetEngineValueNodeFactoryService.Value.Create(lz.Metadata.LanguageGuid, formatter.Value);
				if (valueNodeFactory == null)
					continue;

				var languageDisplayName = ResourceHelper.GetString(lz.Value, lz.Metadata.LanguageDisplayName);
				yield return new DbgEngineLanguageImpl(dbgModuleReferenceProvider.Value, lz.Metadata.LanguageName, languageDisplayName, lz.Value, dbgMethodDebugInfoProvider.Value, decompiler, formatter.Value, valueNodeFactory, dnILInterpreter.Value, dbgAliasProvider.Value, predefinedEvaluationErrorMessagesHelper);
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
			if (formattersDict.TryGetValue(LanguageConstants.DefaultLanguageGuid, out formatter))
				return true;

			Debug.Fail($"Default formatter ({LanguageConstants.DefaultLanguageGuid.ToString()}) wasn't exported");
			formatter = formattersDict.Values.FirstOrDefault();
			return formatter != null;
		}

		public override DbgEngineObjectIdFactory GetEngineObjectIdFactory(Guid runtimeGuid) =>
			new DbgEngineObjectIdFactoryImpl(runtimeGuid);
	}
}
