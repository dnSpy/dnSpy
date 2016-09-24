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
using dnSpy.Text;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	sealed class QuickInfoSession : IQuickInfoSession2 {
		public PropertyCollection Properties { get; }
		public BulkObservableCollection<object> QuickInfoContent { get; }
		public event EventHandler ApplicableToSpanChanged;
		public bool TrackMouse { get; }
		public ITextView TextView { get; }
		public IIntellisensePresenter Presenter => quickInfoPresenter;
		public event EventHandler PresenterChanged;
		public event EventHandler Recalculated;
		public event EventHandler Dismissed;
		public bool IsDismissed { get; private set; }
		public bool HasInteractiveContent { get; set; }
		bool IsStarted { get; set; }

		public ITrackingSpan ApplicableToSpan {
			get { return applicableToSpan; }
			private set {
				if (!TrackingSpanHelpers.IsSameTrackingSpan(applicableToSpan, value)) {
					applicableToSpan = value;
					ApplicableToSpanChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}
		ITrackingSpan applicableToSpan;

		readonly Lazy<IQuickInfoSourceProvider, IOrderableContentTypeMetadata>[] quickInfoSourceProviders;
		readonly ITrackingPoint triggerPoint;
		readonly IIntellisensePresenterFactoryService intellisensePresenterFactoryService;
		IQuickInfoSource[] quickInfoSources;
		IIntellisensePresenter quickInfoPresenter;

		public QuickInfoSession(ITextView textView, ITrackingPoint triggerPoint, bool trackMouse, IIntellisensePresenterFactoryService intellisensePresenterFactoryService, Lazy<IQuickInfoSourceProvider, IOrderableContentTypeMetadata>[] quickInfoSourceProviders) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (triggerPoint == null)
				throw new ArgumentNullException(nameof(triggerPoint));
			if (intellisensePresenterFactoryService == null)
				throw new ArgumentNullException(nameof(intellisensePresenterFactoryService));
			if (quickInfoSourceProviders == null)
				throw new ArgumentNullException(nameof(quickInfoSourceProviders));
			Properties = new PropertyCollection();
			QuickInfoContent = new BulkObservableCollection<object>();
			TextView = textView;
			this.triggerPoint = triggerPoint;
			TrackMouse = trackMouse;
			this.intellisensePresenterFactoryService = intellisensePresenterFactoryService;
			this.quickInfoSourceProviders = quickInfoSourceProviders;
			TextView.Closed += TextView_Closed;
		}

		void TextView_Closed(object sender, EventArgs e) {
			if (!IsDismissed)
				Dismiss();
		}

		IQuickInfoSource[] CreateQuickInfoSources() {
			var list = new List<IQuickInfoSource>();
			var textBuffer = TextView.TextBuffer;
			foreach (var provider in quickInfoSourceProviders) {
				if (!TextView.TextDataModel.ContentType.IsOfAnyType(provider.Metadata.ContentTypes))
					continue;
				var source = provider.Value.TryCreateQuickInfoSource(textBuffer);
				if (source != null)
					list.Add(source);
			}
			return list.ToArray();
		}

		void DisposeQuickInfoSources() {
			if (quickInfoSources != null) {
				foreach (var source in quickInfoSources)
					source.Dispose();
				quickInfoSources = null;
			}
		}

		public void Start() {
			if (IsStarted)
				throw new InvalidOperationException();
			if (IsDismissed)
				throw new InvalidOperationException();
			Recalculate();
		}

		public void Recalculate() {
			if (IsDismissed)
				throw new InvalidOperationException();
			IsStarted = true;

			DisposeQuickInfoSources();
			quickInfoSources = CreateQuickInfoSources();

			var newContent = new List<object>();
			ITrackingSpan applicableToSpan = null;
			foreach (var source in quickInfoSources) {
				ITrackingSpan applicableToSpanTmp;
				source.AugmentQuickInfoSession(this, newContent, out applicableToSpanTmp);
				if (IsDismissed)
					return;
				if (applicableToSpan == null)
					applicableToSpan = applicableToSpanTmp;
			}

			if (newContent.Count == 0 || applicableToSpan == null)
				Dismiss();
			else {
				QuickInfoContent.BeginBulkOperation();
				QuickInfoContent.Clear();
				QuickInfoContent.AddRange(newContent);
				QuickInfoContent.EndBulkOperation();

				HasInteractiveContent = CalculateHasInteractiveContent();
				ApplicableToSpan = applicableToSpan;
				if (quickInfoPresenter == null) {
					quickInfoPresenter = intellisensePresenterFactoryService.TryCreateIntellisensePresenter(this);
					if (quickInfoPresenter == null) {
						Dismiss();
						return;
					}
					PresenterChanged?.Invoke(this, EventArgs.Empty);
				}
			}
			Recalculated?.Invoke(this, EventArgs.Empty);
		}

		bool CalculateHasInteractiveContent() {
			foreach (var o in QuickInfoContent) {
				if (o is IInteractiveQuickInfoContent)
					return true;
			}
			return false;
		}

		public void Dismiss() {
			if (IsDismissed)
				return;
			IsDismissed = true;
			TextView.Closed -= TextView_Closed;
			Dismissed?.Invoke(this, EventArgs.Empty);
			DisposeQuickInfoSources();
		}

		public bool Match() {
			// There's nothing to match...
			return false;
		}

		public void Collapse() => Dismiss();

		public ITrackingPoint GetTriggerPoint(ITextBuffer textBuffer) {
			if (!IsStarted)
				throw new InvalidOperationException();
			if (IsDismissed)
				throw new InvalidOperationException();

			return IntellisenseSessionHelper.GetTriggerPoint(TextView, triggerPoint, textBuffer);
		}

		public SnapshotPoint? GetTriggerPoint(ITextSnapshot textSnapshot) {
			if (!IsStarted)
				throw new InvalidOperationException();
			if (IsDismissed)
				throw new InvalidOperationException();

			return IntellisenseSessionHelper.GetTriggerPoint(TextView, triggerPoint, textSnapshot);
		}
	}
}
