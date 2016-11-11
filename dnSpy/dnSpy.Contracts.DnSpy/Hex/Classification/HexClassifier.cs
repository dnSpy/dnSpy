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
using System.Threading;

namespace dnSpy.Contracts.Hex.Classification {
	/// <summary>
	/// Hex viewer classifier
	/// </summary>
	public abstract class HexClassifier : IDisposable {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexClassifier() { }

		/// <summary>
		/// Raised when classification spans have changed
		/// </summary>
		public abstract event EventHandler<HexClassificationChangedEventArgs> ClassificationChanged;

		/// <summary>
		/// Classifies text
		/// </summary>
		/// <param name="result">Updated with classifications</param>
		/// <param name="context">Context</param>
		public abstract void GetClassificationSpans(List<HexClassificationSpan> result, HexClassificationContext context);

		/// <summary>
		/// Classifies text synchronously
		/// </summary>
		/// <param name="result">Updated with classifications</param>
		/// <param name="context">Context</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public virtual void GetClassificationSpans(List<HexClassificationSpan> result, HexClassificationContext context, CancellationToken cancellationToken) =>
			GetClassificationSpans(result, context);

		/// <summary>
		/// Disposes this instance
		/// </summary>
		public void Dispose() => DisposeCore();

		/// <summary>
		/// Disposes this instance
		/// </summary>
		protected virtual void DisposeCore() { }
	}
}
