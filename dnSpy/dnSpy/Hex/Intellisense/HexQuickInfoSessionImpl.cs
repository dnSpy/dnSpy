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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Intellisense;
using VSLI = Microsoft.VisualStudio.Language.Intellisense;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Intellisense {
	sealed class HexQuickInfoSessionImpl : HexQuickInfoSession {
		public override VSLI.BulkObservableCollection<object> QuickInfoContent { get; }
		public override event EventHandler ApplicableToSpanChanged;
		public override bool TrackMouse { get; }
		public override HexView HexView { get; }
		public override HexIntellisensePresenter Presenter => quickInfoPresenter;
		public override HexCellPosition TriggerPoint { get; }
		public override event EventHandler PresenterChanged;
		public override event EventHandler Recalculated;
		public override event EventHandler Dismissed;
		public override bool IsDismissed => isDismissed;
		bool isDismissed;
		public override bool HasInteractiveContent => hasInteractiveContent;
		bool hasInteractiveContent;
		bool IsStarted { get; set; }

		public override HexBufferSpanSelection ApplicableToSpan {
			get { return applicableToSpan; }
		}

		void SetApplicableToSpan(HexBufferSpanSelection newValue) {
			if (!applicableToSpan.Equals(newValue)) {
				applicableToSpan = newValue;
				ApplicableToSpanChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		HexBufferSpanSelection applicableToSpan;

		readonly Lazy<HexQuickInfoSourceProvider, VSUTIL.IOrderable>[] quickInfoSourceProviders;
		readonly HexIntellisensePresenterFactoryService intellisensePresenterFactoryService;
		HexQuickInfoSource[] quickInfoSources;
		HexIntellisensePresenter quickInfoPresenter;

		public HexQuickInfoSessionImpl(HexView hexView, HexCellPosition triggerPoint, bool trackMouse, HexIntellisensePresenterFactoryService intellisensePresenterFactoryService, Lazy<HexQuickInfoSourceProvider, VSUTIL.IOrderable>[] quickInfoSourceProviders) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			if (triggerPoint.IsDefault)
				throw new ArgumentException();
			if (intellisensePresenterFactoryService == null)
				throw new ArgumentNullException(nameof(intellisensePresenterFactoryService));
			if (quickInfoSourceProviders == null)
				throw new ArgumentNullException(nameof(quickInfoSourceProviders));
			QuickInfoContent = new VSLI.BulkObservableCollection<object>();
			HexView = hexView;
			TriggerPoint = triggerPoint;
			TrackMouse = trackMouse;
			this.intellisensePresenterFactoryService = intellisensePresenterFactoryService;
			this.quickInfoSourceProviders = quickInfoSourceProviders;
			HexView.Closed += HexView_Closed;
		}

		void HexView_Closed(object sender, EventArgs e) {
			if (!IsDismissed)
				Dismiss();
		}

		HexQuickInfoSource[] CreateQuickInfoSources() {
			var list = new List<HexQuickInfoSource>();
			foreach (var provider in quickInfoSourceProviders) {
				var source = provider.Value.TryCreateQuickInfoSource(HexView);
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

		public override void Start() {
			if (IsStarted)
				throw new InvalidOperationException();
			if (IsDismissed)
				throw new InvalidOperationException();
			Recalculate();
		}

		public override void Recalculate() {
			if (IsDismissed)
				throw new InvalidOperationException();
			IsStarted = true;

			DisposeQuickInfoSources();
			quickInfoSources = CreateQuickInfoSources();

			var newContent = new List<object>();
			var applicableToSpan = default(HexBufferSpanSelection);
			foreach (var source in quickInfoSources) {
				HexBufferSpanSelection applicableToSpanTmp;
				source.AugmentQuickInfoSession(this, newContent, out applicableToSpanTmp);
				if (IsDismissed)
					return;
				if (applicableToSpan.IsDefault)
					applicableToSpan = applicableToSpanTmp;
			}

			if (newContent.Count == 0 || applicableToSpan.IsDefault)
				Dismiss();
			else {
				QuickInfoContent.BeginBulkOperation();
				QuickInfoContent.Clear();
				QuickInfoContent.AddRange(newContent);
				QuickInfoContent.EndBulkOperation();

				hasInteractiveContent = CalculateHasInteractiveContent();
				SetApplicableToSpan(applicableToSpan);
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
				if (o is IHexInteractiveQuickInfoContent)
					return true;
			}
			return false;
		}

		public override void Dismiss() {
			if (IsDismissed)
				return;
			isDismissed = true;
			HexView.Closed -= HexView_Closed;
			Dismissed?.Invoke(this, EventArgs.Empty);
			DisposeQuickInfoSources();
		}

		public override bool Match() {
			// There's nothing to match...
			return false;
		}

		public override void Collapse() => Dismiss();
	}
}
