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
using System.Collections.Generic;
using System.Diagnostics;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdRuntimeImpl : DmdRuntime {
		readonly object lockObj;
		readonly List<DmdAppDomainImpl> appDomains;

		internal DmdEvaluator Evaluator { get; }

		public DmdRuntimeImpl(DmdEvaluator evaluator) {
			lockObj = new object();
			appDomains = new List<DmdAppDomainImpl>();
			Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
		}

		internal void Add(DmdAppDomainImpl appDomain) {
			if (appDomain == null)
				throw new ArgumentNullException(nameof(appDomain));
			lock (lockObj) {
				Debug.Assert(!appDomains.Contains(appDomain));
				appDomains.Add(appDomain);
			}
		}

		internal void Remove(DmdAppDomainImpl appDomain) {
			if (appDomain == null)
				throw new ArgumentNullException(nameof(appDomain));
			lock (lockObj) {
				bool b = appDomains.Remove(appDomain);
				Debug.Assert(b);
			}
		}

		public override DmdAppDomain[] GetAppDomains() {
			lock (lockObj)
				return appDomains.ToArray();
		}

		public override DmdAppDomain GetAppDomain(int id) {
			lock (lockObj) {
				foreach (var appDomain in appDomains) {
					if (appDomain.Id == id)
						return appDomain;
				}
			}
			return null;
		}
	}
}
