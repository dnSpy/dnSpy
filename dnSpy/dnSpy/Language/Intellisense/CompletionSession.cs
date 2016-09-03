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
using System.Collections.ObjectModel;
using System.Diagnostics;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Text;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	interface ICompletionSessionImpl : ICompletionSession {
		ICompletionPresenter Presenter { get; }
	}

	sealed class CompletionSession : ICompletionSessionImpl {
		public PropertyCollection Properties { get; }
		public ITextView TextView { get; }
		public ReadOnlyObservableCollection<CompletionCollection> CompletionCollections { get; }
		public event EventHandler<SelectedCompletionCollectionEventArgs> SelectedCompletionCollectionChanged;
		public bool IsDismissed { get; private set; }
		public event EventHandler Dismissed;
		public bool IsStarted { get; private set; }
		ICompletionPresenter ICompletionSessionImpl.Presenter => completionPresenter;

		public CompletionCollection SelectedCompletionCollection {
			get { return selectedCompletionCollection; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (!CompletionCollections.Contains(value))
					throw new ArgumentException();
				if (selectedCompletionCollection == value)
					return;
				var oldValue = selectedCompletionCollection;
				selectedCompletionCollection = value;
				SelectedCompletionCollectionChanged?.Invoke(this, new SelectedCompletionCollectionEventArgs(oldValue, selectedCompletionCollection));
				Filter();
				Match();
			}
		}
		CompletionCollection selectedCompletionCollection;

		readonly ObservableCollection<CompletionCollection> completionCollections;
		readonly Lazy<ICompletionSourceProvider, IOrderableContentTypeMetadata>[] completionSourceProviders;
		readonly ITrackingPoint triggerPoint;
		readonly ICompletionPresenterService completionPresenterService;
		SessionCommandTargetFilter sessionCommandTargetFilter;
		ICompletionPresenter completionPresenter;
		ICompletionSource[] completionSources;
		TextViewPopup textViewPopup;

		public CompletionSession(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret, ICompletionPresenterService completionPresenterService, Lazy<ICompletionSourceProvider, IOrderableContentTypeMetadata>[] completionSourceProviders) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (triggerPoint == null)
				throw new ArgumentNullException(nameof(triggerPoint));
			if (completionPresenterService == null)
				throw new ArgumentNullException(nameof(completionPresenterService));
			if (completionSourceProviders == null)
				throw new ArgumentNullException(nameof(completionSourceProviders));
			this.completionCollections = new ObservableCollection<CompletionCollection>();
			CompletionCollections = new ReadOnlyObservableCollection<CompletionCollection>(this.completionCollections);
			Properties = new PropertyCollection();
			TextView = textView;
			this.triggerPoint = triggerPoint;
			//TODO: Use trackCaret
			this.completionPresenterService = completionPresenterService;
			TextView.Closed += TextView_Closed;
			this.completionSourceProviders = completionSourceProviders;
		}

		void TextView_Closed(object sender, EventArgs e) {
			if (!IsDismissed)
				Dismiss();
		}

		ICompletionSource[] CreateCompletionSources() {
			var list = new List<ICompletionSource>();
			foreach (var provider in completionSourceProviders) {
				if (!TextView.TextDataModel.ContentType.ContainsAny(provider.Metadata.ContentTypes))
					continue;
				var source = provider.Value.Create(TextView);
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
			this.completionSources = CreateCompletionSources();

			var list = new List<CompletionCollection>();
			foreach (var source in completionSources)
				source.AugmentCompletionSession(this, list);
			foreach (var cc in list)
				completionCollections.Add(cc);

			if (completionCollections.Count == 0)
				Dismiss();
			else {
				SelectedCompletionCollection = completionCollections[0];
				completionPresenter = completionPresenterService.Create(this);
				Debug.Assert(completionPresenter != null);
				sessionCommandTargetFilter = new SessionCommandTargetFilter(this);
				textViewPopup = new TextViewPopup(TextView, GetTrackingPoint(SelectedCompletionCollection), completionPresenter);
				textViewPopup.Show();
			}
		}

		ITrackingPoint GetTrackingPoint(CompletionCollection coll) {
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
			SelectedCompletionCollection.Commit();
			Dismiss();
		}

		public void Dismiss() {
			if (IsDismissed)
				return;
			IsDismissed = true;
			TextView.Closed -= TextView_Closed;
			textViewPopup?.Dispose();
			textViewPopup = null;
			sessionCommandTargetFilter?.Close();
			sessionCommandTargetFilter = null;
			Dismissed?.Invoke(this, EventArgs.Empty);
			if (completionSources != null) {
				foreach (var source in completionSources)
					source.Dispose();
				completionSources = null;
			}
		}

		public void Filter() {
			if (!IsStarted)
				throw new InvalidOperationException();
			if (IsDismissed)
				throw new InvalidOperationException();
			if (selectedCompletionCollection == null)
				return;
			selectedCompletionCollection.Filter();
		}

		public bool Match() {
			if (!IsStarted)
				throw new InvalidOperationException();
			if (IsDismissed)
				throw new InvalidOperationException();
			selectedCompletionCollection.SelectBestMatch();
			return selectedCompletionCollection.CurrentCompletion.Completion != null;
		}

		public SnapshotPoint? GetTriggerPoint(ITextSnapshot textSnapshot) {
			if (!IsStarted)
				throw new InvalidOperationException();
			if (IsDismissed)
				throw new InvalidOperationException();
			return triggerPoint.GetPoint(textSnapshot);
		}
	}
}
