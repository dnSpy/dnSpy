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
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace dnSpy.Roslyn.EditorFeatures.Host {
	[Export(typeof(IWaitIndicator))]
	sealed class WaitIndicator : IWaitIndicator {
		public IWaitContext StartWait(string title, string message, bool allowCancel, bool showProgress) =>
			new WaitContext(message, allowCancel);

		public WaitIndicatorResult Wait(string title, string message, bool allowCancel, bool showProgress, Action<IWaitContext> action) {
			using (var context = StartWait(title, message, allowCancel, showProgress)) {
				try {
					action(context);
				}
				catch (OperationCanceledException) {
					return WaitIndicatorResult.Canceled;
				}
				catch (AggregateException e) when (e.InnerExceptions.Any(a => a is OperationCanceledException)) {
					return WaitIndicatorResult.Canceled;
				}
			}
			return WaitIndicatorResult.Completed;
		}

		sealed class WaitContext : IWaitContext {
			public CancellationToken CancellationToken { get; }
			public bool AllowCancel { get; set; }
			public string Message { get; set; }
			public IProgressTracker ProgressTracker { get; }

			public WaitContext(string message, bool allowCancel) {
				CancellationToken = CancellationToken.None;
				Message = message;
				AllowCancel = allowCancel;
				ProgressTracker = new ProgressTracker();
			}

			public void Dispose() { }
		}
	}
}
