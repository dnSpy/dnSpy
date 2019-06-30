// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using dnSpy.Roslyn.EditorFeatures.Extensions;
using dnSpy.Roslyn.Internal.SmartIndent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace dnSpy.Roslyn.EditorFeatures.SmartIndent
{
    internal partial class SmartIndent : ISmartIndent
    {
        private readonly ITextView _textView;

        public SmartIndent(ITextView textView)
        {
            _textView = textView ?? throw new ArgumentNullException(nameof(textView));
        }

        public int? GetDesiredIndentation(ITextSnapshotLine line)
        {
            return GetDesiredIndentation(line, CancellationToken.None);
        }

        public void Dispose()
        {
        }

        private int? GetDesiredIndentation(ITextSnapshotLine lineToBeIndented, CancellationToken cancellationToken)
        {
            if (lineToBeIndented == null)
            {
                throw new ArgumentNullException(@"line");
            }

            //using (Logger.LogBlock(FunctionId.SmartIndentation_Start, cancellationToken))
            {
                var document = lineToBeIndented.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                var syncService = document?.GetLanguageService<ISynchronousIndentationService>();

                if (syncService != null)
                {
                    var result = syncService.GetDesiredIndentation(document, lineToBeIndented.LineNumber, cancellationToken);
                    return result?.GetIndentation(_textView, lineToBeIndented);
                }

                var asyncService = document?.GetLanguageService<IIndentationService>();
                if (asyncService != null)
                {
                    var result = asyncService.GetDesiredIndentation(document, lineToBeIndented.LineNumber, cancellationToken).WaitAndGetResult(cancellationToken);
                    return result?.GetIndentation(_textView, lineToBeIndented);
                }

                return null;
            }
        }
    }
}
