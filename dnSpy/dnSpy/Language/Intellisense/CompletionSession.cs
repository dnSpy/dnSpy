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
using System.Collections.ObjectModel;
using System.Diagnostics;
using dnSpy.Text;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	sealed class CompletionSession : ICompletionSession {
		public PropertyCollection Properties { get; }
		public ITextView TextView { get; }
		public ReadOnlyObservableCollection<CompletionSet> CompletionSets { get; }
		public event EventHandler<ValueChangedEventArgs<CompletionSet>> SelectedCompletionSetChanged;
		public bool IsDismissed { get; private set; }
		public event EventHandler Dismissed;
		public bool IsStarted { get; private set; }
		public IIntellisensePresenter Presenter => completionPresenter;
		public event EventHandler PresenterChanged;
		public event EventHandler Recalculated;
		public event EventHandler Committed;

		public CompletionSet SelectedCompletionSet {
			get => selectedCompletionSet;
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (!CompletionSets.Contains(value))
					throw new ArgumentException();
				if (selectedCompletionSet == value)
					return;
				var oldValue = selectedCompletionSet;
				selectedCompletionSet = value;
				SelectedCompletionSetChanged?.Invoke(this, new ValueChangedEventArgs<CompletionSet>(oldValue, selectedCompletionSet));
				Filter();
				Match();
			}
		}
		CompletionSet selectedCompletionSet;

		readonly ObservableCollection<CompletionSet> completionSets;
		readonly Lazy<ICompletionSourceProvider, IOrderableContentTypeMetadata>[] completionSourceProviders;
		readonly ITrackingPoint triggerPoint;
		readonly IIntellisensePresenterFactoryService intellisensePresenterFactoryService;
		CompletionSessionCommandTargetFilter completionSessionCommandTargetFilter;
		IIntellisensePresenter completionPresenter;
		ICompletionSource[] completionSources;

		public CompletionSession(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret, IIntellisensePresenterFactoryService intellisensePresenterFactoryService, Lazy<ICompletionSourceProvider, IOrderableContentTypeMetadata>[] completionSourceProviders) {
			completionSets = new ObservableCollection<CompletionSet>();
			CompletionSets = new ReadOnlyObservableCollection<CompletionSet>(completionSets);
			Properties = new PropertyCollection();
			TextView = textView ?? throw new ArgumentNullException(nameof(textView));
			this.triggerPoint = triggerPoint ?? throw new ArgumentNullException(nameof(triggerPoint));
			this.intellisensePresenterFactoryService = intellisensePresenterFactoryService ?? throw new ArgumentNullException(nameof(intellisensePresenterFactoryService));
			this.completionSourceProviders = completionSourceProviders ?? throw new ArgumentNullException(nameof(completionSourceProviders));
			//TODO: Use trackCaret
			TextView.Closed += TextView_Closed;
		}

		void TextView_Closed(object sender, EventArgs e) {
			if (!IsDismissed)
				Dismiss();
		}

		ICompletionSource[] CreateCompletionSources() {
			var list = new List<ICompletionSource>();
			var textBuffer = TextView.TextBuffer;
			foreach (var provider in completionSourceProviders) {
				if (!TextView.TextDataModel.ContentType.IsOfAnyType(provider.Metadata.ContentTypes))
					continue;
				var source = provider.Value.TryCreateCompletionSource(textBuffer);
				if (source != null)
					list.Add(source);
			}
			return list.ToArray();
		}

		public void Start() {
			if (IsStarted)
				throw new InvalidOperationException();
			if (IsDismissed)
				throw new InvalidOperationException();
			IsStarted = true;
			completionSources = CreateCompletionSources();

			var list = new List<CompletionSet>();
			foreach (var source in completionSources)
				source.AugmentCompletionSession(this, list);
			foreach (var cc in list)
				completionSets.Add(cc);

			if (completionSets.Count == 0)
				Dismiss();
			else {
				SelectedCompletionSet = completionSets[0];
				completionPresenter = intellisensePresenterFactoryService.TryCreateIntellisensePresenter(this);
				if (completionPresenter == null) {
					Dismiss();
					return;
				}
				PresenterChanged?.Invoke(this, EventArgs.Empty);
				completionSessionCommandTargetFilter = new CompletionSessionCommandTargetFilter(this);
			}
		}

		public void Recalculate() {
			if (!IsStarted)
				throw new InvalidOperationException();
			if (IsDismissed)
				throw new InvalidOperationException();

			foreach (var completionSet in completionSets) {
				completionSet.Recalculate();
				if (IsDismissed)
					return;
			}
			Match();
			Recalculated?.Invoke(this, EventArgs.Empty);
		}

		ITrackingPoint GetTrackingPoint(CompletionSet coll) {
			var trackingSpan = coll.ApplicableTo;
			var snapshot = trackingSpan.TextBuffer.CurrentSnapshot;
			var point = trackingSpan.GetStartPoint(snapshot);
			return snapshot.CreateTrackingPoint(point.Position, PointTrackingMode.Negative);
		}

		public void Commit() {
			if (!IsStarted)
				throw new InvalidOperationException();
			if (IsDismissed)
				throw new InvalidOperationException();
			var completionSet = SelectedCompletionSet;
			var completion = completionSet?.SelectionStatus.Completion;
			if (completion != null) {
				Debug.Assert(completionSet.SelectionStatus.IsSelected);
				if (completion is ICustomCommit customCommit)
					customCommit.Commit();
				else {
					var insertionText = completion.InsertionText;
					if (insertionText != null) {
						var replaceSpan = completionSet.ApplicableTo;
						var buffer = replaceSpan.TextBuffer;
						var span = replaceSpan.GetSpan(buffer.CurrentSnapshot);
						buffer.Replace(span.Span, insertionText);
					}
				}
			}
			Committed?.Invoke(this, EventArgs.Empty);
			Dismiss();
		}

		public void Dismiss() {
			if (IsDismissed)
				return;
			IsDismissed = true;
			TextView.Closed -= TextView_Closed;
			completionSessionCommandTargetFilter?.Close();
			completionSessionCommandTargetFilter = null;
			Dismissed?.Invoke(this, EventArgs.Empty);
			if (completionSources != null) {
				foreach (var source in completionSources)
					source.Dispose();
				completionSources = null;
			}
		}

		public void Collapse() => Dismiss();

		public void Filter() {
			if (!IsStarted)
				throw new InvalidOperationException();
			if (IsDismissed)
				throw new InvalidOperationException();
			if (selectedCompletionSet == null)
				return;
			selectedCompletionSet.Filter();
		}

		public bool Match() {
			if (!IsStarted)
				throw new InvalidOperationException();
			if (IsDismissed)
				throw new InvalidOperationException();
			selectedCompletionSet.SelectBestMatch();
			return selectedCompletionSet.SelectionStatus.Completion != null;
		}

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
