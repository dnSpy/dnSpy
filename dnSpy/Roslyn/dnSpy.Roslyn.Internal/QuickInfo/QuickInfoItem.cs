// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace dnSpy.Roslyn.Internal.QuickInfo
{
    internal class QuickInfoItem
    {
        public TextSpan TextSpan { get; }
        public QuickInfoContent Content { get; }

        public QuickInfoItem(TextSpan textSpan, QuickInfoContent content)
        {
            this.TextSpan = textSpan;
            this.Content = content;
        }
    }
}
