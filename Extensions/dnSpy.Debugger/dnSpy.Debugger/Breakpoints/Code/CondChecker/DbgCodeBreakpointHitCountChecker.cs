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

using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;

namespace dnSpy.Debugger.Breakpoints.Code.CondChecker {
	abstract class DbgCodeBreakpointHitCountChecker {
		public abstract DbgCodeBreakpointCheckResult ShouldBreak(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread, DbgCodeBreakpointHitCount hitCount, int currentHitCount);
	}

	[Export(typeof(DbgCodeBreakpointHitCountChecker))]
	sealed class DbgCodeBreakpointHitCountCheckerImpl : DbgCodeBreakpointHitCountChecker {
		public override DbgCodeBreakpointCheckResult ShouldBreak(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread, DbgCodeBreakpointHitCount hitCount, int currentHitCount) {
			switch (hitCount.Kind) {
			case DbgCodeBreakpointHitCountKind.Equals:
				return new DbgCodeBreakpointCheckResult(currentHitCount == hitCount.Count);

			case DbgCodeBreakpointHitCountKind.MultipleOf:
				if (hitCount.Count <= 0)
					return new DbgCodeBreakpointCheckResult($"Invalid hit count value: {hitCount.Count}");
				return new DbgCodeBreakpointCheckResult((currentHitCount % hitCount.Count) == 0);

			case DbgCodeBreakpointHitCountKind.GreaterThanOrEquals:
				return new DbgCodeBreakpointCheckResult(currentHitCount >= hitCount.Count);

			default:
				Debug.Fail($"Unknown hit count kind: {hitCount.Kind}");
				return new DbgCodeBreakpointCheckResult($"Unknown hit count kind: {hitCount.Kind}");
			}
		}
	}
}
