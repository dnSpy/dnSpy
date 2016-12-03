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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dnSpy.Contracts.Utilities;
using dnSpy.Roslyn.Internal.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Roslyn.Shared.Intellisense.SignatureHelp {
	sealed class SignatureHelpSession {
		public event EventHandler Disposed;
		readonly Lazy<ISignatureHelpBroker> signatureHelpBroker;
		readonly ITextView textView;
		readonly SignatureHelpService signatureHelpService;
		readonly List<Signature> signatures;
		CancellationTokenSource cancellationTokenSource;
		ISignatureHelpSession session;
		static readonly object sigHelpSessionKey = new object();


		SignatureHelpSession(SignatureHelpService signatureHelpService, Lazy<ISignatureHelpBroker> signatureHelpBroker, ITextView textView) {
			this.signatureHelpBroker = signatureHelpBroker;
			this.textView = textView;
			signatures = new List<Signature>();
			this.signatureHelpService = signatureHelpService;
		}

		/// <summary>
		/// Gets the Roslyn sig help session stored in a <see cref="ISignatureHelpSession"/> or null if none
		/// </summary>
		/// <param name="session">Intellisense sig help session</param>
		/// <returns></returns>
		public static SignatureHelpSession TryGetSession(ISignatureHelpSession session) {
			if (session == null)
				return null;
			SignatureHelpSession ourSession;
			if (session.Properties.TryGetProperty(sigHelpSessionKey, out ourSession))
				return ourSession;
			return null;
		}

		public static SignatureHelpSession TryCreate(SnapshotPoint triggerPosition, SignatureHelpTriggerInfo triggerInfo, Lazy<ISignatureHelpBroker> signatureHelpBroker, ITextView textView) {
			var info = SignatureHelpInfo.Create(triggerPosition.Snapshot);
			if (info == null)
				return null;
			if (triggerInfo.TriggerReason == SignatureHelpTriggerReason.TypeCharCommand) {
				Debug.Assert(triggerInfo.TriggerCharacter != null);
				if (triggerInfo.TriggerCharacter != null && !info.Value.SignatureHelpService.IsTriggerCharacter(triggerInfo.TriggerCharacter.Value))
					return null;
			}
			else if (triggerInfo.TriggerReason == SignatureHelpTriggerReason.RetriggerCommand) {
				if (triggerInfo.TriggerCharacter != null && !info.Value.SignatureHelpService.IsRetriggerCharacter(triggerInfo.TriggerCharacter.Value))
					return null;
			}

			return new SignatureHelpSession(info.Value.SignatureHelpService, signatureHelpBroker, textView);
		}

		public void Restart(SnapshotPoint triggerPosition, SignatureHelpTriggerInfo triggerInfo) {
			if (!RestartCore(triggerPosition, triggerInfo))
				Dispose();
		}

		bool RestartCore(SnapshotPoint triggerPosition, SignatureHelpTriggerInfo triggerInfo) {
			var info = SignatureHelpInfo.Create(triggerPosition.Snapshot);
			if (info == null)
				return false;

			Start(info.Value, triggerPosition, triggerInfo);
			return true;
		}

		void CancelFetchItems() {
			cancellationTokenSource?.Cancel();
			cancellationTokenSource?.Dispose();
			cancellationTokenSource = null;
		}

		void Start(SignatureHelpInfo info, SnapshotPoint triggerPosition, SignatureHelpTriggerInfo triggerInfo) {
			CancelFetchItems();
			Debug.Assert(cancellationTokenSource == null);
			cancellationTokenSource = new CancellationTokenSource();
			var cancellationTokenSourceTmp = cancellationTokenSource;
			StartAsync(info, triggerPosition, triggerInfo, cancellationTokenSource.Token)
			.ContinueWith(t => {
				var ex = t.Exception;
				// Free resources
				if (cancellationTokenSource == cancellationTokenSourceTmp)
					CancelFetchItems();
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		async Task StartAsync(SignatureHelpInfo info, SnapshotPoint triggerPosition, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken) {
			// This helps a little to speed up the code
			ProfileOptimizationHelper.StartProfile("roslyn-sighelp-" + info.SignatureHelpService.Language);

			var result = await info.SignatureHelpService.GetItemsAsync(info.Document, triggerPosition.Position, triggerInfo, cancellationToken);
			if (result == null) {
				Dispose();
				return;
			}
			if (cancellationToken.IsCancellationRequested)
				return;
			StartSession(triggerPosition, result);
		}

		ITrackingSpan CreateApplicableToSpan(SnapshotPoint triggerPosition, TextSpan applicableSpan) {
			var snapshot = triggerPosition.Snapshot;
			Debug.Assert(applicableSpan.End <= snapshot.Length);
			Debug.Assert(applicableSpan.Start <= triggerPosition.Position && triggerPosition.Position <= applicableSpan.End);

			int startPos = Math.Min(applicableSpan.Start, snapshot.Length);
			int endPos = Math.Max(startPos, Math.Min(Math.Min(triggerPosition.Position, applicableSpan.End), snapshot.Length));
			var span = Span.FromBounds(startPos, endPos);
			return snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
		}

		void StartSession(SnapshotPoint triggerPosition, SignatureHelpResult signatureHelpResult) {
			if (isDisposed || textView.IsClosed) {
				Dispose();
				return;
			}

			var triggerPoint = triggerPosition.Snapshot.CreateTrackingPoint(triggerPosition.Position, PointTrackingMode.Negative);
			var applicableSpan = signatureHelpResult.Items.ApplicableSpan;
			var trackingSpan = CreateApplicableToSpan(triggerPosition, applicableSpan);

			InitializeSignatures(trackingSpan, signatureHelpResult);

			if (session == null) {
				session = signatureHelpBroker.Value.CreateSignatureHelpSession(textView, triggerPoint, trackCaret: false);
				session.Dismissed += Session_Dismissed;
				session.Properties.AddProperty(sigHelpSessionKey, this);
			}
			session.Recalculate();

			// It's set to null if it got dismissed
			if (session == null || session.IsDismissed) {
				Debug.Assert(isDisposed);
				Dispose();
				return;
			}

			var selectedSig = signatures.FirstOrDefault(a => a.IsSelected);
			if (selectedSig != null)
				session.SelectedSignature = selectedSig;
		}

		void Session_Dismissed(object sender, EventArgs e) => Dispose();

		void InitializeSignatures(ITrackingSpan applicableToSpan, SignatureHelpResult signatureHelpResult) {
			Debug.Assert(signatureHelpResult.Items != null);
			signatures.Clear();
			foreach (var item in signatureHelpResult.Items.Items) {
				bool isSelected = signatureHelpResult.SelectedItem == item;
				signatures.Add(new Signature(applicableToSpan, item, isSelected, signatureHelpResult.SelectedParameter));
			}
		}

		public void AugmentSignatureHelpSession(IList<ISignature> signatures) {
			foreach (var sig in this.signatures)
				signatures.Add(sig);
		}

		public ISignature GetBestMatch() => session.SelectedSignature;
		public bool IsTriggerCharacter(char c) => signatureHelpService.IsTriggerCharacter(c);
		public bool IsRetriggerCharacter(char c) => signatureHelpService.IsRetriggerCharacter(c);

		public void Dispose() {
			if (isDisposed)
				return;
			isDisposed = true;
			CancelFetchItems();
			if (session != null) {
				session.Dismissed -= Session_Dismissed;
				session.Dismiss();
			}
			session = null;
			signatures.Clear();
			Disposed?.Invoke(this, EventArgs.Empty);
		}
		bool isDisposed;
	}
}
