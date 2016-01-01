/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

namespace dndbg.Engine {
	public interface IBreakpointCondition {
		/// <summary>
		/// Called whenever the breakpoint is enabled and hit. Returns true if the breakpoint should
		/// be triggered and the debugger should stop the process. This method is called from the
		/// dndbg thread.
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		bool Hit(BreakpointConditionContext context);
	}

	public sealed class AlwaysBreakpointCondition : IBreakpointCondition {
		public static readonly AlwaysBreakpointCondition Instance = new AlwaysBreakpointCondition();

		public bool Hit(BreakpointConditionContext context) {
			return true;
		}
	}

	public sealed class NeverBreakpointCondition : IBreakpointCondition {
		public static readonly NeverBreakpointCondition Instance = new NeverBreakpointCondition();

		public bool Hit(BreakpointConditionContext context) {
			return false;
		}
	}

	public sealed class OrBreakpointCondition : IBreakpointCondition {
		public List<IBreakpointCondition> BreakpointConditions {
			get { return hitBps; }
		}
		readonly List<IBreakpointCondition> hitBps = new List<IBreakpointCondition>();

		public bool Hit(BreakpointConditionContext context) {
			foreach (var hitBp in hitBps) {
				if (hitBp.Hit(context))
					return true;
			}
			return false;
		}
	}

	public sealed class AndBreakpointCondition : IBreakpointCondition {
		public List<IBreakpointCondition> BreakpointConditions {
			get { return hitBps; }
		}
		readonly List<IBreakpointCondition> hitBps = new List<IBreakpointCondition>();

		public bool Hit(BreakpointConditionContext context) {
			foreach (var hitBp in hitBps) {
				if (!hitBp.Hit(context))
					return false;
			}
			return true;
		}
	}

	public sealed class CountEqualBreakpointCondition : IBreakpointCondition {
		readonly int count;
		int hits;

		public CountEqualBreakpointCondition(int count) {
			this.count = count;
		}

		public bool Hit(BreakpointConditionContext context) {
			return ++hits == count;
		}
	}

	public sealed class CountGreaterThanOrEqualBreakpointCondition : IBreakpointCondition {
		readonly int count;
		int hits;

		public CountGreaterThanOrEqualBreakpointCondition(int count) {
			this.count = count;
		}

		public bool Hit(BreakpointConditionContext context) {
			return ++hits >= count;
		}
	}

	public sealed class DelegateBreakpointCondition : IBreakpointCondition {
		readonly Predicate<BreakpointConditionContext> cond;

		public DelegateBreakpointCondition(Predicate<BreakpointConditionContext> cond) {
			if (cond == null)
				throw new ArgumentNullException();
			this.cond = cond;
		}

		public bool Hit(BreakpointConditionContext context) {
			return cond(context);
		}
	}
}
