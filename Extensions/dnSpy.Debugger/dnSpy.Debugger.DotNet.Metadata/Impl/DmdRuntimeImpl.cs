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
using System.Diagnostics;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdRuntimeImpl : DmdRuntime {
		sealed private protected override void YouCantDeriveFromThisClass() => throw new InvalidOperationException();
		public override int PointerSize { get; }
		public override DmdImageFileMachine Machine { get; }

		readonly object appDomainsLockObj;
		readonly List<DmdAppDomainImpl> appDomains;

		internal DmdEvaluator Evaluator { get; }

		public DmdRuntimeImpl(DmdEvaluator evaluator, DmdImageFileMachine machine) {
			appDomainsLockObj = new object();
			appDomains = new List<DmdAppDomainImpl>();
			Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
			PointerSize = CalculatePointerSize(machine);
			Machine = machine;
		}

		static int CalculatePointerSize(DmdImageFileMachine machine) {
			switch (machine) {
			case DmdImageFileMachine.IA64:
			case DmdImageFileMachine.AMD64:
			case DmdImageFileMachine.ARM64:
			case DmdImageFileMachine.ALPHA64:
				return 8;
			default:
				return 4;
			}
		}

		public override DmdAppDomain CreateAppDomain(int id) {
			var appDomain = new DmdAppDomainImpl(this, id);
			Add(appDomain);
			return appDomain;
		}

		internal void Add(DmdAppDomainImpl appDomain) {
			if (appDomain is null)
				throw new ArgumentNullException(nameof(appDomain));
			lock (appDomainsLockObj) {
				Debug.Assert(!appDomains.Contains(appDomain));
				appDomains.Add(appDomain);
			}
		}

		public override void Remove(DmdAppDomain appDomain) {
			if (appDomain is null)
				throw new ArgumentNullException(nameof(appDomain));
			var appDomainImpl = appDomain as DmdAppDomainImpl;
			if (appDomainImpl is null)
				throw new ArgumentException();
			if (appDomainImpl.Runtime != this)
				throw new ArgumentException();
			lock (appDomainsLockObj) {
				bool b = appDomains.Remove(appDomainImpl);
				Debug.Assert(b);
			}
		}

		public override DmdAppDomain[] GetAppDomains() {
			lock (appDomainsLockObj)
				return appDomains.ToArray();
		}

		public override DmdAppDomain? GetAppDomain(int id) {
			lock (appDomainsLockObj) {
				foreach (var appDomain in appDomains) {
					if (appDomain.Id == id)
						return appDomain;
				}
			}
			return null;
		}
	}
}
