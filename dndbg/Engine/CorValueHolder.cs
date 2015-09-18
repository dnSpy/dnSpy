/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

namespace dndbg.Engine {
	public interface ICorValueHolder {
		bool IsNeutered { get; }
		CorValue CorValue { get; }
	}

	public abstract class CorValueHolderBase : ICorValueHolder {
		public bool IsNeutered {
			get {
				if (value == null)
					return true;
				//TODO: If it's a local/arg and it's a struct, it's not neutered
				return HasDebuggeeExecuted;
			}
		}

		/// <summary>
		/// true if debuggee has executed code since <see cref="CorValue"/> was created
		/// </summary>
		protected abstract bool HasDebuggeeExecuted { get; }

		public CorValue CorValue {
			get {
				if (IsNeutered) {
					value = getCorValue();
					OnNewCorValue(value == null);
				}
				return value;
			}
		}
		CorValue value;

		readonly Func<CorValue> getCorValue;

		/// <summary>
		/// Called when a new <see cref="dndbg.Engine.CorValue"/> has been created
		/// </summary>
		/// <param name="failed">true if we failed to create a new <see cref="dndbg.Engine.CorValue"/></param>
		protected abstract void OnNewCorValue(bool failed);

		protected CorValueHolderBase(CorValue value, Func<CorValue> getCorValue) {
			this.value = value;
			this.getCorValue = getCorValue;
		}
	}

	public sealed class DummyCorValueHolder : CorValueHolderBase {
		protected override bool HasDebuggeeExecuted {
			get { return false; }
		}

		protected override void OnNewCorValue(bool failed) {
		}

		public DummyCorValueHolder(CorValue value)
			: base(value, () => value) {
		}
	}

	public sealed class CorValueHolder : CorValueHolderBase {
		uint continueCounter;

		uint CurrentContinueCounter {
			get {
				var dbg = getDebugger();
				if (dbg != null)
					return dbg.ContinueCounter;
				return 0;
			}
		}

		protected override bool HasDebuggeeExecuted {
			get { return CurrentContinueCounter != continueCounter; }
		}

		readonly Func<DnDebugger> getDebugger;

		public CorValueHolder(CorValue value, Func<CorValue> getCorValue, Func<DnDebugger> getDebugger)
			: base(value, getCorValue) {
			this.getDebugger = getDebugger;
			InitializeMessageCounter();
		}

		protected override void OnNewCorValue(bool failed) {
			InitializeMessageCounter();
		}

		void InitializeMessageCounter() {
			continueCounter = CurrentContinueCounter;
		}
	}
}
