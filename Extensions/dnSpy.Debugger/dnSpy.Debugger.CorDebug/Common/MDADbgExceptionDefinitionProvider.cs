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

using System.Collections.Generic;
using dnSpy.Contracts.Debugger.Exceptions;

namespace dnSpy.Debugger.CorDebug.Common {
	[ExportDbgExceptionDefinitionProvider]
	sealed class MDADbgExceptionDefinitionProvider : DbgExceptionDefinitionProvider {
		// See ExceptionInfo.cs on how to regenerate this array
		static readonly ExceptionInfo[] exceptionInfos = new ExceptionInfo[] {
			new ExceptionInfo("AsynchronousThreadAbort", DbgExceptionDefinitionFlags.None),
			new ExceptionInfo("BindingFailure", DbgExceptionDefinitionFlags.None),
			new ExceptionInfo("CallbackOnCollectedDelegate", DbgExceptionDefinitionFlags.StopFirstChance),
			new ExceptionInfo("ContextSwitchDeadlock", DbgExceptionDefinitionFlags.StopFirstChance),
			new ExceptionInfo("DangerousThreadingAPI", DbgExceptionDefinitionFlags.None),
			new ExceptionInfo("DateTimeInvalidLocalFormat", DbgExceptionDefinitionFlags.StopFirstChance),
			new ExceptionInfo("DisconnectedContext", DbgExceptionDefinitionFlags.StopFirstChance),
			new ExceptionInfo("DllMainReturnsFalse", DbgExceptionDefinitionFlags.None),
			new ExceptionInfo("ExceptionSwallowedOnCallFromCom", DbgExceptionDefinitionFlags.None),
			new ExceptionInfo("FailedQI", DbgExceptionDefinitionFlags.None),
			new ExceptionInfo("FatalExecutionEngineError", DbgExceptionDefinitionFlags.StopFirstChance),
			new ExceptionInfo("InvalidApartmentStateChange", DbgExceptionDefinitionFlags.None),
			new ExceptionInfo("InvalidFunctionPointerInDelegate", DbgExceptionDefinitionFlags.StopFirstChance),
			new ExceptionInfo("InvalidMemberDeclaration", DbgExceptionDefinitionFlags.StopFirstChance),
			new ExceptionInfo("InvalidOverlappedToPinvoke", DbgExceptionDefinitionFlags.None),
			new ExceptionInfo("InvalidVariant", DbgExceptionDefinitionFlags.StopFirstChance),
			new ExceptionInfo("LoaderLock", DbgExceptionDefinitionFlags.StopFirstChance),
			new ExceptionInfo("LoadFromContext", DbgExceptionDefinitionFlags.None),
			new ExceptionInfo("MarshalCleanupError", DbgExceptionDefinitionFlags.None),
			new ExceptionInfo("NonComVisibleBaseClass", DbgExceptionDefinitionFlags.StopFirstChance),
			new ExceptionInfo("NotMarshalable", DbgExceptionDefinitionFlags.None),
			new ExceptionInfo("PInvokeStackImbalance", DbgExceptionDefinitionFlags.StopFirstChance),
			new ExceptionInfo("RaceOnRCWCleanup", DbgExceptionDefinitionFlags.StopFirstChance),
			new ExceptionInfo("Reentrancy", DbgExceptionDefinitionFlags.StopFirstChance),
			new ExceptionInfo("ReleaseHandleFailed", DbgExceptionDefinitionFlags.None),
			new ExceptionInfo("ReportAvOnComRelease", DbgExceptionDefinitionFlags.None),
			new ExceptionInfo("StreamWriterBufferedDataLost", DbgExceptionDefinitionFlags.None),
		};

		const string groupName = PredefinedExceptionGroups.MDA;
		const string groupDisplayName = "Managed Debugging Assistants";

		public override IEnumerable<DbgExceptionGroupDefinition> CreateGroups() {
			yield return new DbgExceptionGroupDefinition(groupName, groupDisplayName);
		}

		public override IEnumerable<DbgExceptionDefinition> Create() {
			foreach (var info in exceptionInfos)
				yield return new DbgExceptionDefinition(new DbgExceptionId(groupName, info.Name), info.Flags);
		}
	}
}
