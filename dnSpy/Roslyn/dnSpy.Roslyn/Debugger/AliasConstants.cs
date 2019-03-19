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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;

namespace dnSpy.Roslyn.Debugger {
	static class AliasConstants {
		// These strings are hard coded in the expression compiler
		public const string ReturnValueName = "$ReturnValue";
		public const string ExceptionName = "$exception";
		public const string StowedExceptionName = "$stowedexception";
		public const string ObjectIdName = "$";

		public static bool TryGetAliasInfo(string aliasName, bool isCaseSensitive, out DbgDotNetParsedAlias aliasInfo) {
			if (aliasName != null) {
				var comparison = isCaseSensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
				if (aliasName.Equals(ReturnValueName, comparison)) {
					aliasInfo = new DbgDotNetParsedAlias(DbgDotNetAliasKind.ReturnValue, DbgDotNetRuntimeConstants.LastReturnValueId);
					return true;
				}
				if (aliasName.Equals(ExceptionName, comparison)) {
					aliasInfo = new DbgDotNetParsedAlias(DbgDotNetAliasKind.Exception, DbgDotNetRuntimeConstants.ExceptionId);
					return true;
				}
				if (aliasName.Equals(StowedExceptionName, comparison)) {
					aliasInfo = new DbgDotNetParsedAlias(DbgDotNetAliasKind.StowedException, DbgDotNetRuntimeConstants.StowedExceptionId);
					return true;
				}
				if (TryGetId(aliasName, ReturnValueName, comparison, out uint id) && id != DbgDotNetRuntimeConstants.LastReturnValueId) {
					aliasInfo = new DbgDotNetParsedAlias(DbgDotNetAliasKind.ReturnValue, id);
					return true;
				}
				if (TryGetId(aliasName, ObjectIdName, comparison, out id)) {
					aliasInfo = new DbgDotNetParsedAlias(DbgDotNetAliasKind.ObjectId, id);
					return true;
				}
			}

			aliasInfo = default;
			return false;
		}

		static bool TryGetId(string aliasName, string prefix, StringComparison comparison, out uint id) {
			if (aliasName.StartsWith(prefix, comparison)) {
				if (uint.TryParse(aliasName.Substring(prefix.Length), out id) && aliasName.Equals(prefix + id.ToString(), comparison))
					return true;
			}

			id = 0;
			return false;
		}
	}
}
