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
using System.Threading.Tasks;
using dnSpy.Contracts.Files.Tabs;

namespace dnSpy.Files.Tabs {
	sealed class AsyncShowResult : IAsyncShowResult {
		public Exception Exception { get; set; }
		public bool IsCanceled { get; set; }
		public bool CanShowOutput { get; set; }

		public AsyncShowResult() {
			this.CanShowOutput = true;
		}

		public AsyncShowResult(Task t, bool canShowOutput) {
			this.Exception = t.Exception;
			this.IsCanceled = t.IsCanceled;
			this.CanShowOutput = canShowOutput;
		}
	}
}
