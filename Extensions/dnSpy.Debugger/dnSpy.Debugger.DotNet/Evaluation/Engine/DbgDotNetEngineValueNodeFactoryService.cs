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
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.Engine.Evaluation.Internal;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	abstract class DbgDotNetEngineValueNodeFactoryService {
		public abstract DbgDotNetEngineValueNodeFactory Create(string languageGuid, DbgDotNetFormatter formatter);
	}

	[Export(typeof(DbgDotNetEngineValueNodeFactoryService))]
	sealed class DbgDotNetEngineValueNodeFactoryServiceImpl : DbgDotNetEngineValueNodeFactoryService {
		readonly IPredefinedEvaluationErrorMessagesHelper errorMessagesHelper;
		readonly Dictionary<Guid, Lazy<DbgDotNetValueNodeFactory, IDbgDotNetValueNodeFactoryMetadata>> toLazyFactory;
		readonly Dictionary<Lazy<DbgDotNetValueNodeFactory, IDbgDotNetValueNodeFactoryMetadata>, DbgDotNetEngineValueNodeFactory> toFactory;

		[ImportingConstructor]
		DbgDotNetEngineValueNodeFactoryServiceImpl(IPredefinedEvaluationErrorMessagesHelper errorMessagesHelper, [ImportMany] IEnumerable<Lazy<DbgDotNetValueNodeFactory, IDbgDotNetValueNodeFactoryMetadata>> factories) {
			this.errorMessagesHelper = errorMessagesHelper;
			toLazyFactory = new Dictionary<Guid, Lazy<DbgDotNetValueNodeFactory, IDbgDotNetValueNodeFactoryMetadata>>();
			toFactory = new Dictionary<Lazy<DbgDotNetValueNodeFactory, IDbgDotNetValueNodeFactoryMetadata>, DbgDotNetEngineValueNodeFactory>();
			foreach (var lz in factories.OrderBy(a => a.Metadata.Order)) {
				bool b = Guid.TryParse(lz.Metadata.LanguageGuid, out var languageGuid);
				Debug.Assert(b);
				if (!b)
					continue;
				if (!toLazyFactory.ContainsKey(languageGuid))
					toLazyFactory.Add(languageGuid, lz);
			}
		}

		public override DbgDotNetEngineValueNodeFactory Create(string languageGuid, DbgDotNetFormatter formatter) {
			if (languageGuid == null)
				throw new ArgumentNullException(nameof(languageGuid));
			if (formatter == null)
				throw new ArgumentNullException(nameof(formatter));

			bool b = Guid.TryParse(languageGuid, out var guid);
			Debug.Assert(b);
			if (!b)
				return null;

			if (TryGetFactory(guid, formatter, out var factory))
				return factory;
			if (TryGetFactory(LanguageConstants.DefaultLanguageGuid, formatter, out factory))
				return factory;

			Debug.Fail($"Default value node factory ({LanguageConstants.DefaultLanguageGuid.ToString()}) wasn't exported");
			var lz = toLazyFactory.Values.FirstOrDefault();
			if (lz != null)
				return GetFactory(formatter, lz);
			return null;
		}

		bool TryGetFactory(Guid guid, DbgDotNetFormatter formatter, out DbgDotNetEngineValueNodeFactory factory) {
			if (!toLazyFactory.TryGetValue(guid, out var lz)) {
				factory = null;
				return false;
			}
			factory = GetFactory(formatter, lz);
			return true;
		}

		DbgDotNetEngineValueNodeFactory GetFactory(DbgDotNetFormatter formatter, Lazy<DbgDotNetValueNodeFactory, IDbgDotNetValueNodeFactoryMetadata> lz) {
			lock (toFactory) {
				if (!toFactory.TryGetValue(lz, out var factory))
					toFactory.Add(lz, factory = new DbgDotNetEngineValueNodeFactoryImpl(formatter, lz.Value, errorMessagesHelper));
				return factory;
			}
		}
	}
}
