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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	abstract class DbgAliasProvider {
		public abstract (DbgDotNetAlias[] aliases, DmdType[] typeReferences) GetAliases(DbgEvaluationInfo evalInfo);
	}

	[Export(typeof(DbgAliasProvider))]
	sealed class DbgAliasProviderImpl : DbgAliasProvider {
		readonly DbgObjectIdService objectIdService;

		[ImportingConstructor]
		DbgAliasProviderImpl(DbgObjectIdService objectIdService) => this.objectIdService = objectIdService;

		public override (DbgDotNetAlias[] aliases, DmdType[] typeReferences) GetAliases(DbgEvaluationInfo evalInfo) {
			var runtime = evalInfo.Runtime.GetDotNetRuntime();
			var objectIds = objectIdService.GetObjectIds(evalInfo.Runtime);
			var aliases = runtime.GetAliases(evalInfo);

			if (objectIds.Length == 0 && aliases.Length == 0)
				return (Array.Empty<DbgDotNetAlias>(), Array.Empty<DmdType>());

			var res = new DbgDotNetAlias[objectIds.Length + aliases.Length];
			var typeReferences = new DmdType[res.Length];

			var sb = ObjectCache.AllocStringBuilder();
			var output = new DbgStringBuilderTextWriter(sb);
			int w = 0;
			foreach (var alias in aliases) {
				output.Reset();
				DbgDotNetAliasKind dnAliasKind;
				string aliasName;
				switch (alias.Kind) {
				case DbgDotNetAliasInfoKind.Exception:
					dnAliasKind = DbgDotNetAliasKind.Exception;
					evalInfo.Context.Language.Formatter.FormatExceptionName(evalInfo.Context, output, alias.Id);
					aliasName = sb.ToString();
					break;
				case DbgDotNetAliasInfoKind.StowedException:
					dnAliasKind = DbgDotNetAliasKind.StowedException;
					evalInfo.Context.Language.Formatter.FormatStowedExceptionName(evalInfo.Context, output, alias.Id);
					aliasName = sb.ToString();
					break;
				case DbgDotNetAliasInfoKind.ReturnValue:
					dnAliasKind = DbgDotNetAliasKind.ReturnValue;
					evalInfo.Context.Language.Formatter.FormatReturnValueName(evalInfo.Context, output, alias.Id);
					aliasName = sb.ToString();
					break;
				default:
					throw new InvalidOperationException();
				}
				res[w] = new DbgDotNetAlias(dnAliasKind, alias.Type.AssemblyQualifiedName, aliasName, alias.CustomTypeInfoId, alias.CustomTypeInfo);
				typeReferences[w] = alias.Type;
				w++;
			}
			foreach (var objectId in objectIds) {
				output.Reset();
				var value = objectId.GetValue(evalInfo);
				var dnValue = (DbgDotNetValue)value.InternalValue;
				evalInfo.Context.Language.Formatter.FormatObjectIdName(evalInfo.Context, output, objectId.Id);
				res[w] = new DbgDotNetAlias(DbgDotNetAliasKind.ObjectId, dnValue.Type.AssemblyQualifiedName, sb.ToString(), Guid.Empty, null);
				typeReferences[w] = dnValue.Type;
				w++;
				value.Close();
			}
			if (w != res.Length || w != typeReferences.Length)
				throw new InvalidOperationException();
			ObjectCache.Free(ref sb);
			return (res, typeReferences);
		}
	}
}
