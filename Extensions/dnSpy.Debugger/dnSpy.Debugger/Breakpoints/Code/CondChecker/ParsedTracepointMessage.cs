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
using System.Diagnostics;

namespace dnSpy.Debugger.Breakpoints.Code.CondChecker {
	sealed class ParsedTracepointMessage {
		public TracepointMessagePart[] Parts { get; }
		public int MaxFrames { get; }
		public bool Evaluates { get; }
		public ParsedTracepointMessage(TracepointMessagePart[] parts) {
			Parts = parts ?? throw new ArgumentNullException(nameof(parts));
			var info = CalculateInfo(parts);
			MaxFrames = info.maxFrames;
			Evaluates = info.evaluates;
		}

		static (int maxFrames, bool evaluates) CalculateInfo(TracepointMessagePart[] parts) {
			int maxFrames = 0;
			bool evaluates = false;
			foreach (var part in parts) {
				switch (part.Kind) {
				case TracepointMessageKind.WriteText:
				case TracepointMessageKind.WriteAppDomainId:
				case TracepointMessageKind.WriteBreakpointAddress:
				case TracepointMessageKind.WriteManagedId:
				case TracepointMessageKind.WriteProcessId:
				case TracepointMessageKind.WriteProcessName:
				case TracepointMessageKind.WriteThreadId:
				case TracepointMessageKind.WriteThreadName:
					break;

				case TracepointMessageKind.WriteEvaluatedExpression:
					evaluates = true;
					break;

				case TracepointMessageKind.WriteAddress:
				case TracepointMessageKind.WriteCaller:
				case TracepointMessageKind.WriteCallerModule:
				case TracepointMessageKind.WriteCallerOffset:
				case TracepointMessageKind.WriteCallerToken:
				case TracepointMessageKind.WriteCallStack:
				case TracepointMessageKind.WriteFunction:
					maxFrames = Math.Max(maxFrames, part.Number);
					break;

				default: throw new InvalidOperationException();
				}
			}
			return (maxFrames, evaluates);
		}
	}

	struct TracepointMessagePart {
		public TracepointMessageKind Kind => (TracepointMessageKind)(val & 0xFF);
		public int Number => (int)(val >> 8);
		public string String { get; }
		uint val;

		public TracepointMessagePart(TracepointMessageKind kind, string @string) {
			val = (uint)kind;
			String = @string;
		}

		public TracepointMessagePart(TracepointMessageKind kind, int number) {
			Debug.Assert((int)kind <= 0xFF);
			Debug.Assert(0 <= number && number <= 0x00FFFFFF);
			val = (uint)kind | ((uint)number << 8);
			String = null;
		}
	}

	enum TracepointMessageKind {
		WriteText,
		WriteEvaluatedExpression,
		WriteAddress,
		WriteAppDomainId,
		WriteBreakpointAddress,
		WriteCaller,
		WriteCallerModule,
		WriteCallerOffset,
		WriteCallerToken,
		WriteCallStack,
		WriteFunction,
		WriteManagedId,
		WriteProcessId,
		WriteProcessName,
		WriteThreadId,
		WriteThreadName,
	}
}
