// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace dnSpy.Roslyn.Internal
{
    internal static class LinkedListExtensions
    {
        public static void AddRangeAtHead<T>(this LinkedList<T> list, IEnumerable<T> values)
        {
            var currentNode = default(LinkedListNode<T>);
            foreach (var value in values)
            {
                if (currentNode == null)
                {
                    currentNode = list.AddFirst(value);
                }
                else
                {
                    currentNode = list.AddAfter(currentNode, value);
                }
            }
        }
    }
}
