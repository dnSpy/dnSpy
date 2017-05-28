// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace dnSpy.Roslyn.EditorFeatures.Host
{
    internal interface IWaitIndicator
    {
        /// <summary>
        /// Schedule the action on the caller's thread and wait for the task to complete.
        /// </summary>
        WaitIndicatorResult Wait(string title, string message, bool allowCancel, bool showProgress, Action<IWaitContext> action);
        IWaitContext StartWait(string title, string message, bool allowCancel, bool showProgress);
    }

    internal static class IWaitIndicatorExtensions
    {
        public static WaitIndicatorResult Wait(
            this IWaitIndicator waitIndicator, string title, string message, bool allowCancel, Action<IWaitContext> action)
        {
            return waitIndicator.Wait(title, message, allowCancel, showProgress: false, action: action);
        }
    }
}
