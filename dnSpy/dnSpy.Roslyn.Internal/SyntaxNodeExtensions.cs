// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace dnSpy.Roslyn.Internal
{
    internal static class SyntaxNodeExtensions
    {
        public static IEnumerable<SyntaxNodeOrToken> DepthFirstTraversal(this SyntaxNode node)
        {
            return SyntaxNodeOrTokenExtensions.DepthFirstTraversal(node);
        }

        public static IEnumerable<SyntaxNode> GetAncestors(this SyntaxNode node)
        {
            var current = node.Parent;

            while (current != null)
            {
                yield return current;

                current = current is IStructuredTriviaSyntax
                    ? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
                    : current.Parent;
            }
        }

        public static IEnumerable<TNode> GetAncestors<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            var current = node.Parent;
            while (current != null)
            {
                if (current is TNode)
                {
                    yield return (TNode)current;
                }

                current = current is IStructuredTriviaSyntax
                    ? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
                    : current.Parent;
            }
        }

        public static TNode GetAncestor<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            if (node == null)
            {
                return default(TNode);
            }

            return node.GetAncestors<TNode>().FirstOrDefault();
        }

        public static TNode GetAncestorOrThis<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            if (node == null)
            {
                return default(TNode);
            }

            return node.GetAncestorsOrThis<TNode>().FirstOrDefault();
        }

        public static IEnumerable<TNode> GetAncestorsOrThis<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            var current = node;
            while (current != null)
            {
                if (current is TNode)
                {
                    yield return (TNode)current;
                }

                current = current is IStructuredTriviaSyntax
                    ? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
                    : current.Parent;
            }
        }

        public static bool HasAncestor<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            return node.GetAncestors<TNode>().Any();
        }

        public static IEnumerable<TSyntaxNode> Traverse<TSyntaxNode>(
            this SyntaxNode node, TextSpan searchSpan, Func<SyntaxNode, bool> predicate)
            where TSyntaxNode : SyntaxNode
        {
            Contract.ThrowIfNull(node);

            var nodes = new LinkedList<SyntaxNode>();
            nodes.AddFirst(node);

            while (nodes.Count > 0)
            {
                var currentNode = nodes.First.Value;
                nodes.RemoveFirst();

                if (currentNode != null && searchSpan.Contains(currentNode.FullSpan) && predicate(currentNode))
                {
                    if (currentNode is TSyntaxNode)
                    {
                        yield return (TSyntaxNode)currentNode;
                    }

                    nodes.AddRangeAtHead(currentNode.ChildNodes());
                }
            }
        }

        public static bool CheckParent<T>(this SyntaxNode node, Func<T, bool> valueChecker) where T : SyntaxNode
        {
            if (node == null)
            {
                return false;
            }

            var parentNode = node.Parent as T;
            if (parentNode == null)
            {
                return false;
            }

            return valueChecker(parentNode);
        }

        /// <summary>
        /// Returns true if is a given token is a child token of a certain type of parent node.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent node.</typeparam>
        /// <param name="node">The node that we are testing.</param>
        /// <param name="childGetter">A function that, when given the parent node, returns the child token we are interested in.</param>
        public static bool IsChildNode<TParent>(this SyntaxNode node, Func<TParent, SyntaxNode> childGetter)
            where TParent : SyntaxNode
        {
            var ancestor = node.GetAncestor<TParent>();
            if (ancestor == null)
            {
                return false;
            }

            var ancestorNode = childGetter(ancestor);

            return node == ancestorNode;
        }

        /// <summary>
        /// Returns true if this node is found underneath the specified child in the given parent.
        /// </summary>
        public static bool IsFoundUnder<TParent>(this SyntaxNode node, Func<TParent, SyntaxNode> childGetter)
           where TParent : SyntaxNode
        {
            var ancestor = node.GetAncestor<TParent>();
            if (ancestor == null)
            {
                return false;
            }

            var child = childGetter(ancestor);

            // See if node passes through child on the way up to ancestor.
            return node.GetAncestorsOrThis<SyntaxNode>().Contains(child);
        }

        public static SyntaxNode GetCommonRoot(this SyntaxNode node1, SyntaxNode node2)
        {
            Contract.ThrowIfTrue(node1.RawKind == 0 || node2.RawKind == 0);

            // find common starting node from two nodes.
            // as long as two nodes belong to same tree, there must be at least one common root (Ex, compilation unit)
            var ancestors = node1.GetAncestorsOrThis<SyntaxNode>();
            var set = new HashSet<SyntaxNode>(node2.GetAncestorsOrThis<SyntaxNode>());

            return ancestors.First(set.Contains);
        }

        public static int Width(this SyntaxNode node)
        {
            return node.Span.Length;
        }

        public static int FullWidth(this SyntaxNode node)
        {
            return node.FullSpan.Length;
        }

        public static SyntaxNode FindInnermostCommonNode(
            this IEnumerable<SyntaxNode> nodes,
            Func<SyntaxNode, bool> predicate)
        {
            IEnumerable<SyntaxNode> blocks = null;
            foreach (var node in nodes)
            {
                blocks = blocks == null
                    ? node.AncestorsAndSelf().Where(predicate)
                    : blocks.Intersect(node.AncestorsAndSelf().Where(predicate));
            }

            return blocks == null ? null : blocks.First();
        }

        public static TSyntaxNode FindInnermostCommonNode<TSyntaxNode>(this IEnumerable<SyntaxNode> nodes)
            where TSyntaxNode : SyntaxNode
        {
            return (TSyntaxNode)nodes.FindInnermostCommonNode(n => n is TSyntaxNode);
        }

        /// <summary>
        /// create a new root node from the given root after adding annotations to the tokens
        /// 
        /// tokens should belong to the given root
        /// </summary>
        public static SyntaxNode AddAnnotations(this SyntaxNode root, IEnumerable<Tuple<SyntaxToken, SyntaxAnnotation>> pairs)
        {
            Contract.ThrowIfNull(root);
            Contract.ThrowIfNull(pairs);

            var tokenMap = pairs.GroupBy(p => p.Item1, p => p.Item2).ToDictionary(g => g.Key, g => g.ToArray());
            return root.ReplaceTokens(tokenMap.Keys, (o, n) => o.WithAdditionalAnnotations(tokenMap[o]));
        }

        /// <summary>
        /// create a new root node from the given root after adding annotations to the nodes
        /// 
        /// nodes should belong to the given root
        /// </summary>
        public static SyntaxNode AddAnnotations(this SyntaxNode root, IEnumerable<Tuple<SyntaxNode, SyntaxAnnotation>> pairs)
        {
            Contract.ThrowIfNull(root);
            Contract.ThrowIfNull(pairs);

            var tokenMap = pairs.GroupBy(p => p.Item1, p => p.Item2).ToDictionary(g => g.Key, g => g.ToArray());
            return root.ReplaceNodes(tokenMap.Keys, (o, n) => o.WithAdditionalAnnotations(tokenMap[o]));
        }

        public static TextSpan GetContainedSpan(this IEnumerable<SyntaxNode> nodes)
        {
            Contract.ThrowIfNull(nodes);
            Contract.ThrowIfFalse(nodes.Any());

            TextSpan fullSpan = nodes.First().Span;
            foreach (var node in nodes)
            {
                fullSpan = TextSpan.FromBounds(
                    Math.Min(fullSpan.Start, node.SpanStart),
                    Math.Max(fullSpan.End, node.Span.End));
            }

            return fullSpan;
        }

        public static IEnumerable<TextSpan> GetContiguousSpans(
            this IEnumerable<SyntaxNode> nodes, Func<SyntaxNode, SyntaxToken> getLastToken = null)
        {
            SyntaxNode lastNode = null;
            TextSpan? textSpan = null;

            // Sort the nodes in source location order.
            foreach (var node in nodes.OrderBy(n => n.SpanStart))
            {
                if (lastNode == null)
                {
                    textSpan = node.Span;
                }
                else
                {
                    var lastToken = getLastToken == null
                        ? lastNode.GetLastToken()
                        : getLastToken(lastNode);
                    if (lastToken.GetNextToken(includeDirectives: true) == node.GetFirstToken())
                    {
                        // Expand the span
                        textSpan = TextSpan.FromBounds(textSpan.Value.Start, node.Span.End);
                    }
                    else
                    {
                        // Return the last span, and start a new one
                        yield return textSpan.Value;
                        textSpan = node.Span;
                    }
                }

                lastNode = node;
            }

            if (textSpan.HasValue)
            {
                yield return textSpan.Value;
            }
        }

        public static bool OverlapsHiddenPosition(this SyntaxNode node, CancellationToken cancellationToken)
        {
            return node.OverlapsHiddenPosition(node.Span, cancellationToken);
        }

        public static bool OverlapsHiddenPosition(this SyntaxNode node, TextSpan span, CancellationToken cancellationToken)
        {
            return node.SyntaxTree.OverlapsHiddenPosition(span, cancellationToken);
        }

        public static bool OverlapsHiddenPosition(this SyntaxNode declaration, SyntaxNode startNode, SyntaxNode endNode, CancellationToken cancellationToken)
        {
            var start = startNode.Span.End;
            var end = endNode.SpanStart;

            var textSpan = TextSpan.FromBounds(start, end);
            return declaration.OverlapsHiddenPosition(textSpan, cancellationToken);
        }

        public static IEnumerable<T> GetAnnotatedNodes<T>(this SyntaxNode node, SyntaxAnnotation syntaxAnnotation) where T : SyntaxNode
        {
            return node.GetAnnotatedNodesAndTokens(syntaxAnnotation).Select(n => n.AsNode()).OfType<T>();
        }

        /// <summary>
        /// Creates a new tree of nodes from the existing tree with the specified old nodes replaced with a newly computed nodes.
        /// </summary>
        /// <param name="root">The root of the tree that contains all the specified nodes.</param>
        /// <param name="nodes">The nodes from the tree to be replaced.</param>
        /// <param name="computeReplacementAsync">A function that computes a replacement node for
        /// the argument nodes. The first argument is one of the original specified nodes. The second argument is
        /// the same node possibly rewritten with replaced descendants.</param>
        /// <param name="cancellationToken"></param>
        public static Task<TRootNode> ReplaceNodesAsync<TRootNode>(
            this TRootNode root,
            IEnumerable<SyntaxNode> nodes,
            Func<SyntaxNode, SyntaxNode, CancellationToken, Task<SyntaxNode>> computeReplacementAsync,
            CancellationToken cancellationToken) where TRootNode : SyntaxNode
        {
            return root.ReplaceSyntaxAsync(
                nodes: nodes, computeReplacementNodeAsync: computeReplacementAsync,
                tokens: null, computeReplacementTokenAsync: null,
                trivia: null, computeReplacementTriviaAsync: null,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Creates a new tree of tokens from the existing tree with the specified old tokens replaced with a newly computed tokens.
        /// </summary>
        /// <param name="root">The root of the tree that contains all the specified tokens.</param>
        /// <param name="tokens">The tokens from the tree to be replaced.</param>
        /// <param name="computeReplacementAsync">A function that computes a replacement token for
        /// the argument tokens. The first argument is one of the originally specified tokens. The second argument is
        /// the same token possibly rewritten with replaced trivia.</param>
        /// <param name="cancellationToken"></param>
        public static Task<TRootNode> ReplaceTokensAsync<TRootNode>(
            this TRootNode root,
            IEnumerable<SyntaxToken> tokens,
            Func<SyntaxToken, SyntaxToken, CancellationToken, Task<SyntaxToken>> computeReplacementAsync,
            CancellationToken cancellationToken) where TRootNode : SyntaxNode
        {
            return root.ReplaceSyntaxAsync(
                nodes: null, computeReplacementNodeAsync: null,
                tokens: tokens, computeReplacementTokenAsync: computeReplacementAsync,
                trivia: null, computeReplacementTriviaAsync: null,
                cancellationToken: cancellationToken);
        }

        public static Task<TRoot> ReplaceTriviaAsync<TRoot>(
            this TRoot root,
            IEnumerable<SyntaxTrivia> trivia,
            Func<SyntaxTrivia, SyntaxTrivia, CancellationToken, Task<SyntaxTrivia>> computeReplacementAsync,
            CancellationToken cancellationToken) where TRoot : SyntaxNode
        {
            return root.ReplaceSyntaxAsync(
                nodes: null, computeReplacementNodeAsync: null,
                tokens: null, computeReplacementTokenAsync: null,
                trivia: trivia, computeReplacementTriviaAsync: computeReplacementAsync,
                cancellationToken: cancellationToken);
        }

        public static async Task<TRoot> ReplaceSyntaxAsync<TRoot>(
            this TRoot root,
            IEnumerable<SyntaxNode> nodes,
            Func<SyntaxNode, SyntaxNode, CancellationToken, Task<SyntaxNode>> computeReplacementNodeAsync,
            IEnumerable<SyntaxToken> tokens,
            Func<SyntaxToken, SyntaxToken, CancellationToken, Task<SyntaxToken>> computeReplacementTokenAsync,
            IEnumerable<SyntaxTrivia> trivia,
            Func<SyntaxTrivia, SyntaxTrivia, CancellationToken, Task<SyntaxTrivia>> computeReplacementTriviaAsync,
            CancellationToken cancellationToken)
            where TRoot : SyntaxNode
        {
            // index all nodes, tokens and trivia by the full spans they cover
            var nodesToReplace = nodes != null ? nodes.ToDictionary(n => n.FullSpan) : new Dictionary<TextSpan, SyntaxNode>();
            var tokensToReplace = tokens != null ? tokens.ToDictionary(t => t.FullSpan) : new Dictionary<TextSpan, SyntaxToken>();
            var triviaToReplace = trivia != null ? trivia.ToDictionary(t => t.FullSpan) : new Dictionary<TextSpan, SyntaxTrivia>();

            var nodeReplacements = new Dictionary<SyntaxNode, SyntaxNode>();
            var tokenReplacements = new Dictionary<SyntaxToken, SyntaxToken>();
            var triviaReplacements = new Dictionary<SyntaxTrivia, SyntaxTrivia>();

            var retryAnnotations = new AnnotationTable<object>("RetryReplace");

            var spans = new List<TextSpan>(nodesToReplace.Count + tokensToReplace.Count + triviaToReplace.Count);
            spans.AddRange(nodesToReplace.Keys);
            spans.AddRange(tokensToReplace.Keys);
            spans.AddRange(triviaToReplace.Keys);

            while (spans.Count > 0)
            {
                // sort the spans of the items to be replaced so we can tell if any overlap
                spans.Sort((x, y) =>
                {
                    // order by end offset, and then by length
                    var d = x.End - y.End;

                    if (d == 0)
                    {
                        d = x.Length - y.Length;
                    }

                    return d;
                });

                // compute replacements for all nodes that will go in the same batch
                // only spans that do not overlap go in the same batch.                
                TextSpan previous = default(TextSpan);
                foreach (var span in spans)
                {
                    // only add to replacement map if we don't intersect with the previous node. This taken with the sort order
                    // should ensure that parent nodes are not processed in the same batch as child nodes.
                    if (previous == default(TextSpan) || !previous.IntersectsWith(span))
                    {
                        SyntaxNode currentNode;
                        SyntaxToken currentToken;
                        SyntaxTrivia currentTrivia;

                        if (nodesToReplace.TryGetValue(span, out currentNode))
                        {
                            var original = (SyntaxNode)retryAnnotations.GetAnnotations(currentNode).SingleOrDefault() ?? currentNode;
                            var newNode = await computeReplacementNodeAsync(original, currentNode, cancellationToken).ConfigureAwait(false);
                            nodeReplacements[currentNode] = newNode;
                        }
                        else if (tokensToReplace.TryGetValue(span, out currentToken))
                        {
                            var original = (SyntaxToken)retryAnnotations.GetAnnotations(currentToken).SingleOrDefault();
                            if (original == default(SyntaxToken))
                            {
                                original = currentToken;
                            }

                            var newToken = await computeReplacementTokenAsync(original, currentToken, cancellationToken).ConfigureAwait(false);
                            tokenReplacements[currentToken] = newToken;
                        }
                        else if (triviaToReplace.TryGetValue(span, out currentTrivia))
                        {
                            var original = (SyntaxTrivia)retryAnnotations.GetAnnotations(currentTrivia).SingleOrDefault();
                            if (original == default(SyntaxTrivia))
                            {
                                original = currentTrivia;
                            }

                            var newTrivia = await computeReplacementTriviaAsync(original, currentTrivia, cancellationToken).ConfigureAwait(false);
                            triviaReplacements[currentTrivia] = newTrivia;
                        }
                    }

                    previous = span;
                }

                bool retryNodes = false;
                bool retryTokens = false;
                bool retryTrivia = false;

                // replace nodes in batch
                // submit all nodes so we can annotate the ones we don't replace
                root = root.ReplaceSyntax(
                        nodes: nodesToReplace.Values,
                        computeReplacementNode: (original, rewritten) =>
                            {
                                SyntaxNode replaced;
                                if (rewritten != original || !nodeReplacements.TryGetValue(original, out replaced))
                                {
                                    // the subtree did change, or we didn't have a replacement for it in this batch
                                    // so we need to add an annotation so we can find this node again for the next batch.
                                    replaced = retryAnnotations.WithAdditionalAnnotations(rewritten, original);
                                    retryNodes = true;
                                }

                                return replaced;
                            },
                        tokens: tokensToReplace.Values,
                        computeReplacementToken: (original, rewritten) =>
                            {
                                SyntaxToken replaced;
                                if (rewritten != original || !tokenReplacements.TryGetValue(original, out replaced))
                                {
                                    // the subtree did change, or we didn't have a replacement for it in this batch
                                    // so we need to add an annotation so we can find this node again for the next batch.
                                    replaced = retryAnnotations.WithAdditionalAnnotations(rewritten, original);
                                    retryTokens = true;
                                }

                                return replaced;
                            },
                        trivia: triviaToReplace.Values,
                        computeReplacementTrivia: (original, rewritten) =>
                            {
                                SyntaxTrivia replaced;
                                if (!triviaReplacements.TryGetValue(original, out replaced))
                                {
                                    // the subtree did change, or we didn't have a replacement for it in this batch
                                    // so we need to add an annotation so we can find this node again for the next batch.
                                    replaced = retryAnnotations.WithAdditionalAnnotations(rewritten, original);
                                    retryTrivia = true;
                                }

                                return replaced;
                            });

                nodesToReplace.Clear();
                tokensToReplace.Clear();
                triviaToReplace.Clear();
                spans.Clear();

                // prepare next batch out of all remaining annotated nodes
                if (retryNodes)
                {
                    nodesToReplace = retryAnnotations.GetAnnotatedNodes(root).ToDictionary(n => n.FullSpan);
                    spans.AddRange(nodesToReplace.Keys);
                }

                if (retryTokens)
                {
                    tokensToReplace = retryAnnotations.GetAnnotatedTokens(root).ToDictionary(t => t.FullSpan);
                    spans.AddRange(tokensToReplace.Keys);
                }

                if (retryTrivia)
                {
                    triviaToReplace = retryAnnotations.GetAnnotatedTrivia(root).ToDictionary(t => t.FullSpan);
                    spans.AddRange(triviaToReplace.Keys);
                }
            }

            return root;
        }

        /// <summary>
        /// Look inside a trivia list for a skipped token that contains the given position.
        /// </summary>
        private static readonly Func<SyntaxTriviaList, int, SyntaxToken> s_findSkippedTokenForward = FindSkippedTokenForward;

        /// <summary>
        /// Look inside a trivia list for a skipped token that contains the given position.
        /// </summary>
        private static SyntaxToken FindSkippedTokenForward(SyntaxTriviaList triviaList, int position)
        {
            foreach (var trivia in triviaList)
            {
                if (trivia.HasStructure)
                {
                    var skippedTokensTrivia = trivia.GetStructure() as ISkippedTokensTriviaSyntax;
                    if (skippedTokensTrivia != null)
                    {
                        foreach (var token in skippedTokensTrivia.Tokens)
                        {
                            if (token.Span.Length > 0 && position <= token.Span.End)
                            {
                                return token;
                            }
                        }
                    }
                }
            }

            return default(SyntaxToken);
        }

        /// <summary>
        /// Look inside a trivia list for a skipped token that contains the given position.
        /// </summary>
        private static readonly Func<SyntaxTriviaList, int, SyntaxToken> s_findSkippedTokenBackward = FindSkippedTokenBackward;

        /// <summary>
        /// Look inside a trivia list for a skipped token that contains the given position.
        /// </summary>
        private static SyntaxToken FindSkippedTokenBackward(SyntaxTriviaList triviaList, int position)
        {
            foreach (var trivia in triviaList.Reverse())
            {
                if (trivia.HasStructure)
                {
                    var skippedTokensTrivia = trivia.GetStructure() as ISkippedTokensTriviaSyntax;
                    if (skippedTokensTrivia != null)
                    {
                        foreach (var token in skippedTokensTrivia.Tokens)
                        {
                            if (token.Span.Length > 0 && token.SpanStart <= position)
                            {
                                return token;
                            }
                        }
                    }
                }
            }

            return default(SyntaxToken);
        }

        private static SyntaxToken GetInitialToken(
            SyntaxNode root,
            int position,
            bool includeSkipped = false,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
        {
            return (position < root.FullSpan.End || !(root is ICompilationUnitSyntax))
                ? root.FindToken(position, includeSkipped || includeDirectives || includeDocumentationComments)
                : root.GetLastToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true)
                      .GetPreviousToken(includeZeroWidth: false, includeSkipped: includeSkipped, includeDirectives: includeDirectives, includeDocumentationComments: includeDocumentationComments);
        }

        /// <summary>
        /// If the position is inside of token, return that token; otherwise, return the token to the right.
        /// </summary>
        public static SyntaxToken FindTokenOnRightOfPosition(
            this SyntaxNode root,
            int position,
            bool includeSkipped = false,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
        {
            var findSkippedToken = includeSkipped ? s_findSkippedTokenForward : ((l, p) => default(SyntaxToken));

            var token = GetInitialToken(root, position, includeSkipped, includeDirectives, includeDocumentationComments);

            if (position < token.SpanStart)
            {
                var skippedToken = findSkippedToken(token.LeadingTrivia, position);
                token = skippedToken.RawKind != 0 ? skippedToken : token;
            }
            else if (token.Span.End <= position)
            {
                do
                {
                    var skippedToken = findSkippedToken(token.TrailingTrivia, position);
                    token = skippedToken.RawKind != 0
                        ? skippedToken
                        : token.GetNextToken(includeZeroWidth: false, includeSkipped: includeSkipped, includeDirectives: includeDirectives, includeDocumentationComments: includeDocumentationComments);
                }
                while (token.RawKind != 0 && token.Span.End <= position && token.Span.End <= root.FullSpan.End);
            }

            if (token.Span.Length == 0)
            {
                token = token.GetNextToken();
            }

            return token;
        }

        /// <summary>
        /// If the position is inside of token, return that token; otherwise, return the token to the left.
        /// </summary>
        public static SyntaxToken FindTokenOnLeftOfPosition(
            this SyntaxNode root,
            int position,
            bool includeSkipped = false,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
        {
            var findSkippedToken = includeSkipped ? s_findSkippedTokenBackward : ((l, p) => default(SyntaxToken));

            var token = GetInitialToken(root, position, includeSkipped, includeDirectives, includeDocumentationComments);

            if (position <= token.SpanStart)
            {
                do
                {
                    var skippedToken = findSkippedToken(token.LeadingTrivia, position);
                    token = skippedToken.RawKind != 0
                        ? skippedToken
                        : token.GetPreviousToken(includeZeroWidth: false, includeSkipped: includeSkipped, includeDirectives: includeDirectives, includeDocumentationComments: includeDocumentationComments);
                }
                while (position <= token.SpanStart && root.FullSpan.Start < token.SpanStart);
            }
            else if (token.Span.End < position)
            {
                var skippedToken = findSkippedToken(token.TrailingTrivia, position);
                token = skippedToken.RawKind != 0 ? skippedToken : token;
            }

            if (token.Span.Length == 0)
            {
                token = token.GetPreviousToken();
            }

            return token;
        }
    }
}
