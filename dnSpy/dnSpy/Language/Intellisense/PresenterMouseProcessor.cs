/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	[Export(typeof(IMouseProcessorProvider))]
	[Name(PredefinedDsMouseProcessorProviders.IntellisensePresenter)]
	[ContentType(ContentTypes.Any)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	sealed class PresenterMouseProcessorProvider : IMouseProcessorProvider {
		readonly IIntellisenseSessionStackMapService intellisenseSessionStackMapService;

		[ImportingConstructor]
		PresenterMouseProcessorProvider(IIntellisenseSessionStackMapService intellisenseSessionStackMapService) => this.intellisenseSessionStackMapService = intellisenseSessionStackMapService;

		public IMouseProcessor GetAssociatedProcessor(IWpfTextView wpfTextView) =>
			wpfTextView.Properties.GetOrCreateSingletonProperty(typeof(PresenterMouseProcessor), () => new PresenterMouseProcessor(intellisenseSessionStackMapService.GetStackForTextView(wpfTextView)));
	}

	sealed class PresenterMouseProcessor : IMouseProcessor2 {
		readonly IIntellisenseSessionStack intellisenseSessionStack;

		IMouseProcessor? MouseProcessor => intellisenseSessionStack.TopSession?.Presenter as IMouseProcessor;
		IMouseProcessor2? MouseProcessor2 => intellisenseSessionStack.TopSession?.Presenter as IMouseProcessor2;

		public PresenterMouseProcessor(IIntellisenseSessionStack intellisenseSessionStack) => this.intellisenseSessionStack = intellisenseSessionStack ?? throw new ArgumentNullException(nameof(intellisenseSessionStack));

		void IMouseProcessor.PreprocessMouseLeftButtonDown(MouseButtonEventArgs e) => intellisenseSessionStack.CollapseAllSessions();
		void IMouseProcessor.PostprocessMouseLeftButtonDown(MouseButtonEventArgs e) => MouseProcessor?.PostprocessMouseLeftButtonDown(e);
		void IMouseProcessor.PreprocessMouseRightButtonDown(MouseButtonEventArgs e) => MouseProcessor?.PreprocessMouseRightButtonDown(e);
		void IMouseProcessor.PostprocessMouseRightButtonDown(MouseButtonEventArgs e) => MouseProcessor?.PostprocessMouseRightButtonDown(e);
		void IMouseProcessor.PreprocessMouseLeftButtonUp(MouseButtonEventArgs e) => MouseProcessor?.PreprocessMouseLeftButtonUp(e);
		void IMouseProcessor.PostprocessMouseLeftButtonUp(MouseButtonEventArgs e) => MouseProcessor?.PostprocessMouseLeftButtonUp(e);
		void IMouseProcessor.PreprocessMouseRightButtonUp(MouseButtonEventArgs e) => MouseProcessor?.PreprocessMouseRightButtonUp(e);
		void IMouseProcessor.PostprocessMouseRightButtonUp(MouseButtonEventArgs e) => MouseProcessor?.PostprocessMouseRightButtonUp(e);
		void IMouseProcessor.PreprocessMouseUp(MouseButtonEventArgs e) => MouseProcessor?.PreprocessMouseUp(e);
		void IMouseProcessor.PostprocessMouseUp(MouseButtonEventArgs e) => MouseProcessor?.PostprocessMouseUp(e);
		void IMouseProcessor.PreprocessMouseDown(MouseButtonEventArgs e) => MouseProcessor?.PreprocessMouseDown(e);
		void IMouseProcessor.PostprocessMouseDown(MouseButtonEventArgs e) => MouseProcessor?.PostprocessMouseDown(e);
		void IMouseProcessor.PreprocessMouseMove(MouseEventArgs e) => MouseProcessor?.PreprocessMouseMove(e);
		void IMouseProcessor.PostprocessMouseMove(MouseEventArgs e) => MouseProcessor?.PostprocessMouseMove(e);
		void IMouseProcessor.PreprocessMouseWheel(MouseWheelEventArgs e) => MouseProcessor?.PreprocessMouseWheel(e);
		void IMouseProcessor.PostprocessMouseWheel(MouseWheelEventArgs e) => MouseProcessor?.PostprocessMouseWheel(e);
		void IMouseProcessor.PreprocessMouseEnter(MouseEventArgs e) => MouseProcessor?.PreprocessMouseEnter(e);
		void IMouseProcessor.PostprocessMouseEnter(MouseEventArgs e) => MouseProcessor?.PostprocessMouseEnter(e);
		void IMouseProcessor.PreprocessMouseLeave(MouseEventArgs e) => MouseProcessor?.PreprocessMouseLeave(e);
		void IMouseProcessor.PostprocessMouseLeave(MouseEventArgs e) => MouseProcessor?.PostprocessMouseLeave(e);
		void IMouseProcessor.PreprocessDragLeave(DragEventArgs e) => MouseProcessor?.PreprocessDragLeave(e);
		void IMouseProcessor.PostprocessDragLeave(DragEventArgs e) => MouseProcessor?.PostprocessDragLeave(e);
		void IMouseProcessor.PreprocessDragOver(DragEventArgs e) => MouseProcessor?.PreprocessDragOver(e);
		void IMouseProcessor.PostprocessDragOver(DragEventArgs e) => MouseProcessor?.PostprocessDragOver(e);
		void IMouseProcessor.PreprocessDragEnter(DragEventArgs e) => MouseProcessor?.PreprocessDragEnter(e);
		void IMouseProcessor.PostprocessDragEnter(DragEventArgs e) => MouseProcessor?.PostprocessDragEnter(e);
		void IMouseProcessor.PreprocessDrop(DragEventArgs e) => MouseProcessor?.PreprocessDrop(e);
		void IMouseProcessor.PostprocessDrop(DragEventArgs e) => MouseProcessor?.PostprocessDrop(e);
		void IMouseProcessor.PreprocessQueryContinueDrag(QueryContinueDragEventArgs e) => MouseProcessor?.PreprocessQueryContinueDrag(e);
		void IMouseProcessor.PostprocessQueryContinueDrag(QueryContinueDragEventArgs e) => MouseProcessor?.PostprocessQueryContinueDrag(e);
		void IMouseProcessor.PreprocessGiveFeedback(GiveFeedbackEventArgs e) => MouseProcessor?.PreprocessGiveFeedback(e);
		void IMouseProcessor.PostprocessGiveFeedback(GiveFeedbackEventArgs e) => MouseProcessor?.PostprocessGiveFeedback(e);
		void IMouseProcessor2.PreprocessTouchDown(TouchEventArgs e) => MouseProcessor2?.PreprocessTouchDown(e);
		void IMouseProcessor2.PostprocessTouchDown(TouchEventArgs e) => MouseProcessor2?.PostprocessTouchDown(e);
		void IMouseProcessor2.PreprocessTouchUp(TouchEventArgs e) => MouseProcessor2?.PreprocessTouchUp(e);
		void IMouseProcessor2.PostprocessTouchUp(TouchEventArgs e) => MouseProcessor2?.PostprocessTouchUp(e);
		void IMouseProcessor2.PreprocessStylusSystemGesture(StylusSystemGestureEventArgs e) => MouseProcessor2?.PreprocessStylusSystemGesture(e);
		void IMouseProcessor2.PostprocessStylusSystemGesture(StylusSystemGestureEventArgs e) => MouseProcessor2?.PostprocessStylusSystemGesture(e);
		void IMouseProcessor2.PreprocessManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e) => MouseProcessor2?.PreprocessManipulationInertiaStarting(e);
		void IMouseProcessor2.PostprocessManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e) => MouseProcessor2?.PostprocessManipulationInertiaStarting(e);
		void IMouseProcessor2.PreprocessManipulationStarting(ManipulationStartingEventArgs e) => MouseProcessor2?.PreprocessManipulationStarting(e);
		void IMouseProcessor2.PostprocessManipulationStarting(ManipulationStartingEventArgs e) => MouseProcessor2?.PostprocessManipulationStarting(e);
		void IMouseProcessor2.PreprocessManipulationDelta(ManipulationDeltaEventArgs e) => MouseProcessor2?.PreprocessManipulationDelta(e);
		void IMouseProcessor2.PostprocessManipulationDelta(ManipulationDeltaEventArgs e) => MouseProcessor2?.PostprocessManipulationDelta(e);
		void IMouseProcessor2.PreprocessManipulationCompleted(ManipulationCompletedEventArgs e) => MouseProcessor2?.PreprocessManipulationCompleted(e);
		void IMouseProcessor2.PostprocessManipulationCompleted(ManipulationCompletedEventArgs e) => MouseProcessor2?.PostprocessManipulationCompleted(e);
	}
}
