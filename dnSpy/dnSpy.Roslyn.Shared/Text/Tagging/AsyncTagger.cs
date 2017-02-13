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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Roslyn.Shared.Text.Tagging {
	/// <summary>
	/// Async tagger base class. Multiple <see cref="GetTags(NormalizedSnapshotSpanCollection)"/>
	/// calls are handled by one task to prevent too many created tasks.
	/// </summary>
	/// <typeparam name="TTagType">Type of tag, eg. <see cref="IClassificationTag"/></typeparam>
	/// <typeparam name="TUserAsyncState">User async state type</typeparam>
	/// <remarks>
	/// All tags are cached. The cache is invalidated whenever <see cref="GetTags(NormalizedSnapshotSpanCollection)"/>
	/// is called with a new snapshot.
	///
	/// It currently doesn't try to re-use the old calculated tags. It could return those (after
	/// TranslateTo()'ing them to the new snapshot) while it executes the async code in the background
	/// that calculates the new tags.
	/// </remarks>
	abstract class AsyncTagger<TTagType, TUserAsyncState> : ITagger<TTagType>, IDisposable where TTagType : ITag where TUserAsyncState : new() {
		readonly Dictionary<int, IEnumerable<ITagSpan<TTagType>>> cachedTags;
		readonly object lockObj;
		SnapshotState lastSnapshotState;

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		sealed class SnapshotState {
			public bool TaskStarted { get; set; }
			public GetTagsStateImpl GetTagsStateImpl { get; }
			public ITextSnapshot Snapshot { get; }
			public CancellationToken CancellationToken { get; }
			readonly CancellationTokenSource cancellationTokenSource;

			public SnapshotState(ITextSnapshot snapshot) {
				Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
				cancellationTokenSource = new CancellationTokenSource();
				CancellationToken = cancellationTokenSource.Token;
				GetTagsStateImpl = new GetTagsStateImpl(CancellationToken);
			}

			public void Cancel() {
				if (canceled)
					return;
				canceled = true;
				cancellationTokenSource.Cancel();
			}
			bool canceled;

			int refCounter;
			public void AddRef() => Interlocked.Increment(ref refCounter);

			public void FreeRef() {
				int newValue = Interlocked.Decrement(ref refCounter);
				Debug.Assert(newValue >= 0);
				if (newValue == 0)
					Dispose();
			}

			void Dispose() {
				cancellationTokenSource.Dispose();
				GetTagsStateImpl.Dispose();
			}
		}

		protected struct TagsResult {
			public SnapshotSpan Span { get; }
			public ITagSpan<TTagType>[] Tags { get; }

			public TagsResult(SnapshotSpan span, ITagSpan<TTagType>[] tags) {
				if (span.Snapshot == null)
					throw new ArgumentException();
				Span = span;
				Tags = tags ?? throw new ArgumentNullException(nameof(tags));
			}
		}

		protected abstract class GetTagsState {
			public CancellationToken CancellationToken { get; }
			public abstract void AddResult(TagsResult result);
			public TUserAsyncState UserAsyncState { get; }

			protected GetTagsState(CancellationToken cancellationToken) {
				CancellationToken = cancellationToken;
				UserAsyncState = new TUserAsyncState();
			}
		}

		sealed class GetTagsStateImpl : GetTagsState {
			readonly List<TagsResult> tagsResultList;
			readonly List<NormalizedSnapshotSpanCollection> jobs;
			readonly List<TagsResult> currentResult;
			HashSet<SnapshotSpan> snapshotHash;

			public GetTagsStateImpl(CancellationToken cancellationToken)
				: base(cancellationToken) {
				tagsResultList = new List<TagsResult>();
				jobs = new List<NormalizedSnapshotSpanCollection>();
				currentResult = new List<TagsResult>();
			}

			public override void AddResult(TagsResult result) {
				CancellationToken.ThrowIfCancellationRequested();
				currentResult.Add(result);
			}

			public void OnStartNewJob(NormalizedSnapshotSpanCollection spans) {
				currentResult.Clear();
			}

			public void OnEndNewJob(NormalizedSnapshotSpanCollection spans) {
				AddMissingResults(spans);
				tagsResultList.AddRange(currentResult);
				currentResult.Clear();
			}

			void AddMissingResults(NormalizedSnapshotSpanCollection spans) {
				if (currentResult.Count == spans.Count)
					return;

				// Make sure all spans with no extra info are also cached so GetTags() only gets called once per snapshot span

				// Common case
				if (spans.Count == 1 && currentResult.Count == 0) {
					currentResult.Add(new TagsResult(spans[0], Array.Empty<ITagSpan<TTagType>>()));
					return;
				}

				if (snapshotHash == null)
					snapshotHash = new HashSet<SnapshotSpan>();
				foreach (var r in currentResult)
					snapshotHash.Add(r.Span);
				foreach (var span in spans) {
					if (!snapshotHash.Contains(span))
						currentResult.Add(new TagsResult(span, Array.Empty<ITagSpan<TTagType>>()));
				}
				snapshotHash.Clear();
			}

			public TagsResult[] GetResult() {
				Debug.Assert(jobs.Count == 0);
				Debug.Assert(currentResult.Count == 0);
				Debug.Assert(snapshotHash == null || snapshotHash.Count == 0);
				var result = tagsResultList.ToArray();
				tagsResultList.Clear();
				currentResult.Clear();
				snapshotHash?.Clear();

				return result;
			}

			public NormalizedSnapshotSpanCollection TryGetJob() {
				if (jobs.Count == 0)
					return null;
				int index = jobs.Count - 1;
				var job = jobs[index];
				jobs.RemoveAt(index);
				return job;
			}

			public void AddJob(NormalizedSnapshotSpanCollection spans) => jobs.Add(spans);
			public void Dispose() => (UserAsyncState as IDisposable)?.Dispose();
		}

		protected AsyncTagger() {
			cachedTags = new Dictionary<int, IEnumerable<ITagSpan<TTagType>>>();
			lockObj = new object();
		}

		protected void RefreshAllTags(ITextSnapshot snapshot) {
			Debug.Assert(snapshot != null);
			if (snapshot == null)
				return;
			lock (lockObj) {
				lastSnapshotState?.Cancel();
				lastSnapshotState?.FreeRef();
				lastSnapshotState = null;
				cachedTags.Clear();
			}
			TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
		}

		public IEnumerable<ITagSpan<TTagType>> GetTags(NormalizedSnapshotSpanCollection spans) {
			if (spans.Count == 0)
				return Enumerable.Empty<ITagSpan<TTagType>>();

			var snapshot = spans[0].Snapshot;

			// The common case is spans.Count == 1, so try to prevent extra allocations
			IEnumerable<ITagSpan<TTagType>> singleResult = null;
			List<ITagSpan<TTagType>> multipleResults = null;
			SnapshotSpan? singleMissingSpan = null;
			List<SnapshotSpan> multipleMissingSpans = null;
			lock (lockObj) {
				if (lastSnapshotState?.Snapshot != snapshot) {
					lastSnapshotState?.Cancel();
					lastSnapshotState?.FreeRef();
					cachedTags.Clear();

					lastSnapshotState = new SnapshotState(snapshot);
					lastSnapshotState.AddRef();
				}

				foreach (var span in spans) {
					if (cachedTags.TryGetValue(span.Start.Position, out var tags)) {
						if (singleResult == null)
							singleResult = tags;
						else {
							if (multipleResults == null)
								multipleResults = new List<ITagSpan<TTagType>>(singleResult);
							multipleResults.AddRange(tags);
						}
					}
					else {
						if (singleMissingSpan == null)
							singleMissingSpan = span;
						else {
							if (multipleMissingSpans == null)
								multipleMissingSpans = new List<SnapshotSpan>() { singleMissingSpan.Value };
							multipleMissingSpans.Add(span);
						}
					}
				}
			}
			Debug.Assert(multipleResults == null || multipleResults.Count >= 2);
			Debug.Assert(multipleMissingSpans == null || multipleMissingSpans.Count >= 2);

			if (singleMissingSpan != null) {
				if (spans.Count != (multipleMissingSpans?.Count ?? 1)) {
					spans = multipleMissingSpans != null ?
						new NormalizedSnapshotSpanCollection(multipleMissingSpans) :
						new NormalizedSnapshotSpanCollection(singleMissingSpan.Value);
				}

				lock (lockObj) {
					var lastSnapshotStateTmp = lastSnapshotState;
					lastSnapshotStateTmp.GetTagsStateImpl.AddJob(spans);
					if (!lastSnapshotStateTmp.TaskStarted) {
						lastSnapshotStateTmp.TaskStarted = true;
						lastSnapshotStateTmp.AddRef();
						GetTagsAsync(lastSnapshotStateTmp)
						.ContinueWith(t => {
							lastSnapshotStateTmp.FreeRef();
							var ex = t.Exception;
							if (t.IsCompleted && !t.IsCanceled && !t.IsFaulted)
								SaveResult(t.Result);
						});
					}
				}
			}

			return multipleResults ?? singleResult ?? Enumerable.Empty<ITagSpan<TTagType>>();
		}

		void SaveResult(TagsResult[] tagsResultList) {
			if (tagsResultList.Length == 0)
				return;

			bool sameSnapshot = tagsResultList[0].Span.Snapshot == lastSnapshotState.Snapshot;
			if (sameSnapshot) {
				lock (lockObj) {
					foreach (var result in tagsResultList)
						cachedTags[result.Span.Span.Start] = result.Tags;
				}

				foreach (var result in tagsResultList)
					TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(result.Span));
			}
		}

		async Task<TagsResult[]> GetTagsAsync(SnapshotState snapshotState) {
			try {
				NormalizedSnapshotSpanCollection spans;
				for (;;) {
					lock (lockObj) {
						spans = snapshotState.GetTagsStateImpl.TryGetJob();
						if (spans == null) {
							snapshotState.TaskStarted = false;
							return snapshotState.GetTagsStateImpl.GetResult();
						}
					}

					snapshotState.GetTagsStateImpl.OnStartNewJob(spans);
					await GetTagsAsync(snapshotState.GetTagsStateImpl, spans).ConfigureAwait(false);
					snapshotState.GetTagsStateImpl.OnEndNewJob(spans);
				}
			}
			catch (OperationCanceledException) {
				throw;
			}
			catch {
				return Array.Empty<TagsResult>();
			}
		}

		protected abstract Task GetTagsAsync(GetTagsState state, NormalizedSnapshotSpanCollection spans);

		public void Dispose() {
			DisposeInternal();
			lastSnapshotState?.Cancel();
		}

		protected virtual void DisposeInternal() { }
	}
}
