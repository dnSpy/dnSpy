// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace dnSpy.Roslyn.Internal.SmartIndent
{
    /// <summary>
    /// An indentation result represents where the indent should be placed.  It conveys this through
    /// a pair of values.  A position in the existing document where the indent should be relative,
    /// and the number of columns after that the indent should be placed at.  
    /// 
    /// This pairing provides flexibility to the implementor to compute the indentation results in
    /// a variety of ways.  For example, one implementation may wish to express indentation of a 
    /// newline as being four columns past the start of the first token on a previous line.  Another
    /// may wish to simply express the indentation as an absolute amount from the start of the 
    /// current line.  With this tuple, both forms can be expressed, and the implementor does not
    /// have to convert from one to the other.
    /// </summary>
    internal struct IndentationResult
    {
        /// <summary>
        /// The base position in the document that the indent should be relative to.  This position
        /// can occur on any line (including the current line, or a previous line).
        /// </summary>
        public int BasePosition { get; }

        /// <summary>
        /// The number of columns the indent should be at relative to the BasePosition's column.
        /// </summary>
        public int Offset { get; }

        public IndentationResult(int basePosition, int offset) : this()
        {
            this.BasePosition = basePosition;
            this.Offset = offset;
        }
    }

    internal interface IIndentationService : ILanguageService
    {
        Task<IndentationResult?> GetDesiredIndentation(Document document, int lineNumber, CancellationToken cancellationToken);
    }

    internal interface ISynchronousIndentationService : ILanguageService
    {
        IndentationResult? GetDesiredIndentation(Document document, int lineNumber, CancellationToken cancellationToken);
    }
}
