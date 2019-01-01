// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace dnSpy.Roslyn.Debugger.ExpressionCompiler.VisualBasic {
	static class StringConstants {
		public const string StateMachineHoistedUserVariablePrefix = "$VB$ResumableLocal_";
	}

	static class GeneratedNames {
		/// <summary>
		/// Try to parse the local name and return <paramref name="variableName"/> and <paramref name="index"/> if successful.
		/// </summary>
		public static bool TryParseStateMachineHoistedUserVariableName(string proxyName, out string variableName, out int index) {
			variableName = null;
			index = 0;

			// All names should start with "$VB$ResumableLocal_"
			if (!proxyName.StartsWith(StringConstants.StateMachineHoistedUserVariablePrefix, StringComparison.Ordinal)) {
				return false;
			}

			var prefixLen = StringConstants.StateMachineHoistedUserVariablePrefix.Length;
			var separator = proxyName.LastIndexOf('$');
			if (separator <= prefixLen) {
				return false;
			}

			variableName = proxyName.Substring(prefixLen, separator - prefixLen);
			return int.TryParse(proxyName.Substring(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, out index);
		}
	}

	static class GeneratedNamesHelpers {
		public static bool TryGetHoistedLocalSlotIndex(string name, out int slotIndex) {
			if (GeneratedNames.TryParseStateMachineHoistedUserVariableName(name, out _, out slotIndex))
				return true;
			slotIndex = -1;
			return false;
		}
	}
}
