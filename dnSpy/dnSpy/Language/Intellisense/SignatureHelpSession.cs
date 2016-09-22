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
using System.Linq;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Text;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	sealed class SignatureHelpSession : ISignatureHelpSession {
		public PropertyCollection Properties { get; }
		public ITextView TextView { get; }
		public ReadOnlyObservableCollection<ISignature> Signatures { get; }
		public event EventHandler<SelectedSignatureChangedEventArgs> SelectedSignatureChanged;
		public bool IsDismissed { get; private set; }
		public event EventHandler Dismissed;
		public bool IsStarted { get; private set; }
		public IIntellisensePresenter Presenter => signatureHelpPresenter;
		public event EventHandler PresenterChanged;
		public event EventHandler Recalculated;

		public ISignature SelectedSignature {
			get { return selectedSignature; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (value == selectedSignature)
					return;
				if (!signatures.Contains(value))
					throw new ArgumentException(nameof(value));
				var oldSig = selectedSignature;
				selectedSignature = value;
				SelectedSignatureChanged?.Invoke(this, new SelectedSignatureChangedEventArgs(oldSig, selectedSignature));
			}
		}
		ISignature selectedSignature;

		readonly ITrackingPoint triggerPoint;
		readonly IIntellisensePresenterFactoryService intellisensePresenterFactoryService;
		readonly Lazy<ISignatureHelpSourceProvider, IOrderableContentTypeMetadata>[] signatureHelpSourceProviders;
		readonly ObservableCollection<ISignature> signatures;
		readonly bool trackCaret;
		IIntellisensePresenter signatureHelpPresenter;
		ISignatureHelpSource[] signatureHelpSources;

		public SignatureHelpSession(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret, IIntellisensePresenterFactoryService intellisensePresenterFactoryService, Lazy<ISignatureHelpSourceProvider, IOrderableContentTypeMetadata>[] signatureHelpSourceProviders) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (triggerPoint == null)
				throw new ArgumentNullException(nameof(triggerPoint));
			if (intellisensePresenterFactoryService == null)
				throw new ArgumentNullException(nameof(intellisensePresenterFactoryService));
			if (signatureHelpSourceProviders == null)
				throw new ArgumentNullException(nameof(signatureHelpSourceProviders));
			Properties = new PropertyCollection();
			TextView = textView;
			this.triggerPoint = triggerPoint;
			this.trackCaret = trackCaret;
			this.intellisensePresenterFactoryService = intellisensePresenterFactoryService;
			this.signatureHelpSourceProviders = signatureHelpSourceProviders;
			this.signatures = new ObservableCollection<ISignature>();
			Signatures = new ReadOnlyObservableCollection<ISignature>(this.signatures);

			TextView.Closed += TextView_Closed;
		}

		void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e) {
			if (IsDismissed)
				return;
			var caretPos = TextView.Caret.Position;
			List<int> sigsToRemove = null;
			for (int i = 0; i < signatures.Count; i++) {
				if (!IsInSignature(signatures[i], caretPos)) {
					if (sigsToRemove == null)
						sigsToRemove = new List<int>();
					sigsToRemove.Add(i);
				}
			}
			if (sigsToRemove != null) {
				for (int i = sigsToRemove.Count - 1; i >= 0; i--)
					signatures.RemoveAt(sigsToRemove[i]);
			}
			if (signatures.Count == 0)
				Dismiss();
			else {
				Match();
				if (!signatures.Contains(SelectedSignature))
					SelectedSignature = signatures.FirstOrDefault();
			}
		}

		bool IsInSignature(ISignature signature, CaretPosition caretPos) {
			if (caretPos.VirtualSpaces > 0)
				return false;
			var atSpan = signature.ApplicableToSpan;
			if (atSpan == null)
				return false;
			var span = atSpan.GetSpan(caretPos.BufferPosition.Snapshot);
			return span.IntersectsWith(new SnapshotSpan(caretPos.BufferPosition, 0));
		}

		void TextView_Closed(object sender, EventArgs e) {
			if (!IsDismissed)
				Dismiss();
		}

		ISignatureHelpSource[] CreateSignatureHelpSources() {
			var list = new List<ISignatureHelpSource>();
			var textBuffer = TextView.TextBuffer;
			foreach (var provider in signatureHelpSourceProviders) {
				if (!TextView.TextDataModel.ContentType.IsOfAnyType(provider.Metadata.ContentTypes))
					continue;
				var source = provider.Value.TryCreateSignatureHelpSource(textBuffer);
				if (source != null)
					list.Add(source);
			}
			return list.ToArray();
		}

		public void Start() => Recalculate();

		public void Recalculate() {
			if (IsDismissed)
				throw new InvalidOperationException();
			bool firstTime = !IsStarted;
			IsStarted = true;
			if (firstTime && trackCaret)
				TextView.Caret.PositionChanged += Caret_PositionChanged;

			DisposeSignatureHelpSources();
			signatureHelpSources = CreateSignatureHelpSources();

			var list = new List<ISignature>();
			foreach (var source in signatureHelpSources)
				source.AugmentSignatureHelpSession(this, list);
			signatures.Clear();
			foreach (var sig in list)
				signatures.Add(sig);

			if (signatures.Count == 0)
				Dismiss();
			else {
				SelectedSignature = signatures[0];
				if (signatureHelpPresenter == null) {
					signatureHelpPresenter = intellisensePresenterFactoryService.TryCreateIntellisensePresenter(this);
					if (signatureHelpPresenter == null) {
						Dismiss();
						return;
					}
					PresenterChanged?.Invoke(this, EventArgs.Empty);
				}
			}
			Recalculated?.Invoke(this, EventArgs.Empty);
		}

		void DisposeSignatureHelpSources() {
			if (signatureHelpSources != null) {
				foreach (var source in signatureHelpSources)
					source.Dispose();
				signatureHelpSources = null;
			}
			selectedSignature = null;
		}

		public void Dismiss() {
			if (IsDismissed)
				return;
			IsDismissed = true;
			TextView.Caret.PositionChanged -= Caret_PositionChanged;
			TextView.Closed -= TextView_Closed;
			Dismissed?.Invoke(this, EventArgs.Empty);
			DisposeSignatureHelpSources();
		}

		public void Collapse() => Dismiss();

		public bool Match() {
			if (!IsStarted)
				throw new InvalidOperationException();
			if (IsDismissed)
				throw new InvalidOperationException();

			foreach (var source in signatureHelpSources) {
				var signature = source.GetBestMatch(this);
				if (signature != null) {
					SelectedSignature = signature;
					return true;
				}
			}

			return false;
		}

		public ITrackingPoint GetTriggerPoint(ITextBuffer textBuffer) {
			if (triggerPoint.TextBuffer == textBuffer)
				return triggerPoint;
			return null;
		}

		public SnapshotPoint? GetTriggerPoint(ITextSnapshot textSnapshot) {
			if (!IsStarted)
				throw new InvalidOperationException();
			if (IsDismissed)
				throw new InvalidOperationException();
			if (textSnapshot.TextBuffer != triggerPoint.TextBuffer)
				return null;
			return triggerPoint.GetPoint(textSnapshot);
		}
	}
}
