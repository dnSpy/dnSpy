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
using System.Windows.Threading;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.Text;
using dnSpy.Properties;

namespace dnSpy.Search {
	[Serializable]
	sealed class TooManyResultsException : Exception {
		public TooManyResultsException() {
		}
	}

	sealed class DocumentSearcher : IDocumentSearcher {
		const DispatcherPriority DISPATCHER_PRIO = DispatcherPriority.Background;
		readonly DocumentSearcherOptions options;
		readonly CancellationTokenSource cancellationTokenSource;
		readonly CancellationToken cancellationToken;
		readonly FilterSearcherOptions filterSearcherOptions;

		public bool SyntaxHighlight {
			get { return filterSearcherOptions.Context.SyntaxHighlight; }
			set { filterSearcherOptions.Context.SyntaxHighlight = value; }
		}

		public IDecompiler Decompiler {
			get { return filterSearcherOptions.Context.Decompiler; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				filterSearcherOptions.Context.Decompiler = value;
			}
		}

		public bool TooManyResults { get; set; }

		public DocumentSearcher(DocumentSearcherOptions options, IDocumentTreeView documentTreeView, IDotNetImageService dotNetImageService, SearchResultContext searchResultContext) {
			if (options.Filter == null)
				throw new ArgumentException("options.Filter is null", nameof(options));
			if (options.SearchComparer == null)
				throw new ArgumentException("options.SearchComparer is null", nameof(options));
			this.options = options.Clone();
			cancellationTokenSource = new CancellationTokenSource();
			cancellationToken = cancellationTokenSource.Token;
			filterSearcherOptions = new FilterSearcherOptions {
				Dispatcher = Dispatcher.CurrentDispatcher,
				DocumentTreeView = documentTreeView,
				DotNetImageService = dotNetImageService,
				Filter = options.Filter,
				SearchComparer = options.SearchComparer,
				OnMatch = r => AddSearchResult(r),
				Context = searchResultContext,
				CancellationToken = cancellationToken,
				SearchDecompiledData = options.SearchDecompiledData,
			};
		}

		public event EventHandler OnSearchCompleted;
		public event EventHandler<SearchResultEventArgs> OnNewSearchResults;

		public void Cancel() {
			if (!disposed)
				cancellationTokenSource.Cancel();
		}

		public void Start(IEnumerable<DsDocumentNode> files) => StartInternal(files.ToArray());
		public void Start(IEnumerable<SearchTypeInfo> typeInfos) => StartInternal(typeInfos.ToArray());

		void StartInternal(object o) {
			Debug.Assert(!hasStarted);
			if (hasStarted)
				throw new InvalidOperationException();
			hasStarted = true;
			var task = Task.Factory.StartNew(SearchNewThread, o, cancellationToken)
			.ContinueWith(t => {
				var ex = t.Exception;
				Debug.Assert(ex == null);
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}
		bool hasStarted = false;

		public ISearchResult SearchingResult { get; set; }

		void SearchNewThread(object o) {
			try {
				var searchMsg = SearchResult.CreateMessage(filterSearcherOptions.Context, dnSpy_Resources.Searching, BoxedTextColor.Text, true);
				SearchingResult = searchMsg;
				AddSearchResultNoCheck(searchMsg);
				var opts = new ParallelOptions {
					CancellationToken = cancellationToken,
					MaxDegreeOfParallelism = Environment.ProcessorCount,
				};

				if (o is DsDocumentNode[]) {
					Parallel.ForEach((DsDocumentNode[])o, opts, node => {
						cancellationToken.ThrowIfCancellationRequested();
						var searcher = new FilterSearcher(filterSearcherOptions);
						searcher.SearchAssemblies(new DsDocumentNode[] { node });
					});
				}
				else if (o is SearchTypeInfo[]) {
					Parallel.ForEach((SearchTypeInfo[])o, opts, info => {
						cancellationToken.ThrowIfCancellationRequested();
						var searcher = new FilterSearcher(filterSearcherOptions);
						searcher.SearchTypes(new SearchTypeInfo[] { info });
					});
				}
				else
					throw new InvalidOperationException();
			}
			catch (AggregateException ex) {
				if (ex.InnerExceptions.Any(a => a is TooManyResultsException))
					TooManyResults = true;
				else
					Cancel();
			}
			catch (TooManyResultsException) {
				TooManyResults = true;
			}
			catch {
				Cancel();
			}
			finally {
				filterSearcherOptions.Dispatcher.BeginInvoke(DISPATCHER_PRIO, new Action(SearchCompleted));
			}
		}

		void SearchCompleted() {
			disposed = true;
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
			Debug.Assert(OnSearchCompleted != null);
			OnSearchCompleted?.Invoke(this, EventArgs.Empty);
		}
		bool disposed;

		void AddSearchResult(SearchResult result) {
			cancellationToken.ThrowIfCancellationRequested();
			lock (lockObj) {
				if (totalResultsFound++ >= options.MaxResults) {
					if (totalResultsFound == options.MaxResults + 1)
						AddSearchResultNoCheck(SearchResult.CreateMessage(filterSearcherOptions.Context, string.Format(dnSpy_Resources.SearchAbortedMessage, options.MaxResults), BoxedTextColor.Error, true));
					throw new TooManyResultsException();
				}
			}
			AddSearchResultNoCheck(result);
		}

		void AddSearchResultNoCheck(SearchResult result) {
			bool start;
			lock (lockObj) {
				newSearchResults.Add(result);
				start = newSearchResults.Count == 1;
			}
			if (start)
				filterSearcherOptions.Dispatcher.BeginInvoke(DISPATCHER_PRIO, new Action(EmptySearchResultsQueue));
		}
		int totalResultsFound;
		readonly object lockObj = new object();
		readonly List<ISearchResult> newSearchResults = new List<ISearchResult>();

		void EmptySearchResultsQueue() {
			ISearchResult[] results;
			lock (lockObj) {
				results = newSearchResults.ToArray();
				newSearchResults.Clear();
			}

			// If it was cancelled, don't notify the owner
			if (cancellationTokenSource.IsCancellationRequested)
				return;

			Debug.Assert(OnNewSearchResults != null);
			OnNewSearchResults?.Invoke(this, new SearchResultEventArgs(results));
		}
	}
}
