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
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Properties;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	sealed class SignatureHelpPresenter : IPopupIntellisensePresenter, INotifyPropertyChanged {
		UIElement IPopupIntellisensePresenter.SurfaceElement => control;
		PopupStyles IPopupIntellisensePresenter.PopupStyles => PopupStyles.None;
		string IPopupIntellisensePresenter.SpaceReservationManagerName => PredefinedSpaceReservationManagerNames.SignatureHelp;
		IIntellisenseSession IIntellisensePresenter.Session => session;
		public event PropertyChangedEventHandler PropertyChanged;

		public ICommand SelectPreviousSignatureCommand => new RelayCommand(a => IncrementSelectedSignature(-1));
		public ICommand SelectNextSignatureCommand => new RelayCommand(a => IncrementSelectedSignature(1));
		public bool HasMoreThanOneSignature => session.Signatures.Count > 1;
		public object SignatureCountObject => CreateSignatureCountObject();
		public bool HasSignatureDocumentationObject => !string.IsNullOrEmpty(currentSignature?.Documentation);
		public object SignatureDocumentationObject => CreateSignatureDocumentationObject();
		public object SignatureObject => CreateSignatureObject();
		public object ParameterNameObject => CreateParameterNameObject();
		public object ParameterDocumentationObject => CreateParameterDocumentationObject();

		public bool HasParameter {
			get {
				var parameter = session.SelectedSignature?.CurrentParameter;
				return parameter != null &&
					!string.IsNullOrEmpty(parameter.Documentation) &&
					!string.IsNullOrEmpty(parameter.Name);
			}
		}

		public ITrackingSpan PresentationSpan {
			get { return presentationSpan; }
			private set {
				if (!TrackingSpanHelpers.IsSameTrackingSpan(presentationSpan, value)) {
					presentationSpan = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PresentationSpan)));
				}
			}
		}
		ITrackingSpan presentationSpan;

		public double Opacity {
			get { return control.Opacity; }
			set { control.Opacity = value; }
		}

		readonly SignatureHelpPresenterControl control;
		readonly ISignatureHelpSession session;
		// Used to classify the signature text
		readonly ITextBuffer signatureTextBuffer;
		// Used to classify all the other text (param, param doc, signature doc)
		readonly ITextBuffer otherTextBuffer;
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly IClassifierAggregatorService classifierAggregatorService;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly IContentType defaultExtendedContentType;
		ISignature currentSignature;
		IClassifier signatureClassifier;
		IClassifier otherClassifier;

		const string DefaultExtendedContentTypeName = " default " + SignatureHelpConstants.ExtendedSignatureHelpContentTypeSuffix;
#pragma warning disable 0169
		[Export]
		[Name(DefaultExtendedContentTypeName)]
		[BaseDefinition(ContentTypes.SignatureHelp)]
		static readonly ContentTypeDefinition defaultContentTypeDefinition;
#pragma warning restore 0169

		public SignatureHelpPresenter(ISignatureHelpSession session, ITextBufferFactoryService textBufferFactoryService, IContentTypeRegistryService contentTypeRegistryService, IClassifierAggregatorService classifierAggregatorService, IClassificationFormatMap classificationFormatMap) {
			if (session == null)
				throw new ArgumentNullException(nameof(session));
			if (textBufferFactoryService == null)
				throw new ArgumentNullException(nameof(textBufferFactoryService));
			if (contentTypeRegistryService == null)
				throw new ArgumentNullException(nameof(contentTypeRegistryService));
			if (classifierAggregatorService == null)
				throw new ArgumentNullException(nameof(classifierAggregatorService));
			if (classificationFormatMap == null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			this.session = session;
			this.control = new SignatureHelpPresenterControl { DataContext = this };
			this.signatureTextBuffer = textBufferFactoryService.CreateTextBuffer();
			this.otherTextBuffer = textBufferFactoryService.CreateTextBuffer();
			signatureTextBuffer.Properties[SignatureHelpConstants.SessionBufferKey] = session;
			otherTextBuffer.Properties[SignatureHelpConstants.SessionBufferKey] = session;
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.classifierAggregatorService = classifierAggregatorService;
			this.classificationFormatMap = classificationFormatMap;
			this.defaultExtendedContentType = contentTypeRegistryService.GetContentType(DefaultExtendedContentTypeName);
			Debug.Assert(defaultExtendedContentType != null);
			classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
			session.Dismissed += Session_Dismissed;
			session.SelectedSignatureChanged += Session_SelectedSignatureChanged;
			control.MouseDown += Control_MouseDown;
			// This isn't exposed but ReadOnlyObservableCollection<ISignature> does implement the interface
			((INotifyCollectionChanged)session.Signatures).CollectionChanged += Signatures_CollectionChanged;
			UpdateSelectedSignature();
			UpdatePresentationSpan();
		}

		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SignatureDocumentationObject)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SignatureObject)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ParameterNameObject)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ParameterDocumentationObject)));
		}

		void UnregisterCurrentSignature() {
			if (currentSignature == null)
				return;
			currentSignature.CurrentParameterChanged -= Signature_CurrentParameterChanged;
		}

		void UpdateSelectedSignature() {
			if (session.IsDismissed)
				return;
			UnregisterCurrentSignature();
			var signature = session.SelectedSignature;
			if (signature == null)
				return;
			// Can happen if the session removes a sig
			if (!session.Signatures.Contains(signature))
				return;
			currentSignature = signature;
			signature.CurrentParameterChanged += Signature_CurrentParameterChanged;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SignatureCountObject)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSignatureDocumentationObject)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SignatureDocumentationObject)));
			UpdateCurrentParameter();
		}

		void UpdateCurrentParameter() {
			if (session.IsDismissed)
				return;
			// The signature also gets updated when a new parameter gets selected (eg. the parameter gets bolded)
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SignatureObject)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ParameterNameObject)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ParameterDocumentationObject)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasParameter)));
		}

		void Signature_CurrentParameterChanged(object sender, CurrentParameterChangedEventArgs e) => UpdateCurrentParameter();
		void Session_SelectedSignatureChanged(object sender, SelectedSignatureChangedEventArgs e) => UpdateSelectedSignature();

		bool inCollectionChangedUpdate;
		void Signatures_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (session.IsDismissed)
				return;
			if (inCollectionChangedUpdate)
				return;
			inCollectionChangedUpdate = true;
			// Wait a little so the session has time to update its collection with new items or
			// it will be closed when PresentationSpan is set to null (because of an empty collection)
			control.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				if (session.IsDismissed)
					return;
				inCollectionChangedUpdate = false;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasMoreThanOneSignature)));
				UpdateSelectedSignature();
				UpdatePresentationSpan();
			}));
		}

		void UpdatePresentationSpan() => PresentationSpan = CalculatePresentationSpan();
		ITrackingSpan CalculatePresentationSpan() {
			SnapshotSpan? currSpan = null;
			SpanTrackingMode? spanTrackingMode = null;

			var snapshot = session.TextView.TextSnapshot;
			foreach (var sig in session.Signatures) {
				var atSpan = sig.ApplicableToSpan;
				if (atSpan == null)
					continue;
				if (spanTrackingMode == null)
					spanTrackingMode = atSpan.TrackingMode;
				var span = atSpan.GetSpan(snapshot);
				if (currSpan == null)
					currSpan = span;
				else
					currSpan = new SnapshotSpan(snapshot, Span.FromBounds(Math.Min(currSpan.Value.Start.Position, span.Start.Position), Math.Max(currSpan.Value.End.Position, span.End.Position)));
			}

			return currSpan == null ? null : currSpan.Value.Snapshot.CreateTrackingSpan(currSpan.Value.Span, spanTrackingMode ?? SpanTrackingMode.EdgeInclusive);
		}

		void Control_MouseDown(object sender, MouseButtonEventArgs e) => IncrementSelectedSignature(1);

		object CreateSignatureCountObject() {
			if (session.IsDismissed)
				return null;

			int sigIndex = session.Signatures.IndexOf(currentSignature);
			// Can happen if the session removes a sig
			if (sigIndex < 0)
				return null;
			var text = string.Format(dnSpy_Resources.SignatureHelp_Signature_N_of_TotalCount, sigIndex + 1, session.Signatures.Count);

			var propsSpans = Array.Empty<TextRunPropertiesAndSpan>();
			return TextBlockFactory.Create(text, classificationFormatMap.DefaultTextProperties, propsSpans, TextBlockFactory.Flags.DisableSetTextBlockFontFamily | TextBlockFactory.Flags.DisableFontSize);
		}

		object CreateSignatureDocumentationObject() {
			if (session.IsDismissed)
				return null;

			var signature = currentSignature;
			var doc = signature?.Documentation;
			if (string.IsNullOrEmpty(doc))
				return null;

			return CreateUIObject(doc, GetExtendedClassifierContentType(), new SignatureDocumentationSignatureHelpClassifierContext(session, signature));
		}

		object CreateSignatureObject() {
			if (session.IsDismissed)
				return null;

			var signature = session.SelectedSignature;
			if (signature == null)
				return null;

			bool prettyPrintedContent = false;
			var text = signature.Content;
			if (text == null) {
				prettyPrintedContent = true;
				text = signature.PrettyPrintedContent;
			}
			if (text == null)
				return null;
			signatureTextBuffer.Properties[SignatureHelpConstants.UsePrettyPrintedContentBufferKey] = prettyPrintedContent;
			signatureTextBuffer.Replace(new Span(0, signatureTextBuffer.CurrentSnapshot.Length), text);
			var oldContentType = signatureTextBuffer.ContentType;
			var atSpan = signature.ApplicableToSpan;
			Debug.Assert(atSpan != null);
			if (atSpan != null) {
				var span = atSpan.GetStartPoint(atSpan.TextBuffer.CurrentSnapshot);
				signatureTextBuffer.ChangeContentType(GetSigHelpContentType(span.Snapshot.ContentType), null);
			}
			if (signatureClassifier == null || oldContentType != signatureTextBuffer.ContentType) {
				UnregisterSignatureClassifierEvents();
				signatureClassifier = classifierAggregatorService.GetClassifier(signatureTextBuffer);
				RegisterSignatureClassifierEvents();
			}

			var classificationSpans = signatureClassifier.GetClassificationSpans(new SnapshotSpan(signatureTextBuffer.CurrentSnapshot, 0, signatureTextBuffer.CurrentSnapshot.Length));
			var propsSpans = classificationSpans.Select(a => new TextRunPropertiesAndSpan(a.Span.Span, classificationFormatMap.GetTextProperties(a.ClassificationType)));
			return TextBlockFactory.Create(text, classificationFormatMap.DefaultTextProperties, propsSpans, TextBlockFactory.Flags.DisableSetTextBlockFontFamily | TextBlockFactory.Flags.DisableFontSize);
		}

		void RegisterSignatureClassifierEvents() {
			if (signatureClassifier == null)
				return;
			signatureClassifier.ClassificationChanged += SignatureClassifier_ClassificationChanged;
		}

		void UnregisterSignatureClassifierEvents() {
			if (signatureClassifier == null)
				return;
			signatureClassifier.ClassificationChanged -= SignatureClassifier_ClassificationChanged;
			(signatureClassifier as IDisposable)?.Dispose();
			signatureClassifier = null;
		}

		void SignatureClassifier_ClassificationChanged(object sender, ClassificationChangedEventArgs e) => DelayReclassifySignature();

		void DelayReclassifySignature() {
			if (session.IsDismissed)
				return;
			if (delayReclassifySignatureActive)
				return;
			delayReclassifySignatureActive = true;
			control.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				if (session.IsDismissed)
					return;
				delayReclassifySignatureActive = false;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SignatureObject)));
			}));
		}
		bool delayReclassifySignatureActive;

		IContentType GetSigHelpContentType(IContentType contentType) {
			var sigHelpContentTypeString = contentType.TypeName + SignatureHelpConstants.SignatureHelpContentTypeSuffix;
			var sigHelpContentType = contentTypeRegistryService.GetContentType(sigHelpContentTypeString);
			if (sigHelpContentType == null)
				sigHelpContentType = contentTypeRegistryService.AddContentType(sigHelpContentTypeString, new[] { ContentTypes.SignatureHelp });
			return sigHelpContentType;
		}

		object CreateUIObject(string text, IContentType contentType, SignatureHelpClassifierContext context) {
			otherTextBuffer.Properties[SignatureHelpConstants.SignatureHelpClassifierContextBufferKey] = context;
			otherTextBuffer.Replace(new Span(0, otherTextBuffer.CurrentSnapshot.Length), text);

			var oldContentType = otherTextBuffer.ContentType;
			otherTextBuffer.ChangeContentType(contentType, null);
			if (otherClassifier == null || oldContentType != contentType) {
				(otherClassifier as IDisposable)?.Dispose();
				otherClassifier = classifierAggregatorService.GetClassifier(otherTextBuffer);
			}

			var classificationSpans = otherClassifier.GetClassificationSpans(new SnapshotSpan(otherTextBuffer.CurrentSnapshot, 0, otherTextBuffer.CurrentSnapshot.Length));
			var propsSpans = classificationSpans.Select(a => new TextRunPropertiesAndSpan(a.Span.Span, classificationFormatMap.GetTextProperties(a.ClassificationType)));
			var result = TextBlockFactory.Create(text, classificationFormatMap.DefaultTextProperties, propsSpans, TextBlockFactory.Flags.DisableSetTextBlockFontFamily | TextBlockFactory.Flags.DisableFontSize);
			otherTextBuffer.Properties.RemoveProperty(SignatureHelpConstants.SignatureHelpClassifierContextBufferKey);
			return result;
		}

		IContentType GetExtendedClassifierContentType() => TryGetExtendedClassifierContentTypeCore() ?? defaultExtendedContentType;
		IContentType TryGetExtendedClassifierContentTypeCore() {
			var signature = session.SelectedSignature;
			if (signature == null)
				return null;
			var atSpan = signature.ApplicableToSpan;
			if (atSpan == null)
				return null;
			var bufferContentType = atSpan.TextBuffer.CurrentSnapshot.ContentType;
			return contentTypeRegistryService.GetContentType(bufferContentType.TypeName + SignatureHelpConstants.ExtendedSignatureHelpContentTypeSuffix);
		}

		object CreateParameterNameObject() {
			if (session.IsDismissed)
				return null;

			var parameter = session.SelectedSignature?.CurrentParameter;
			if (parameter == null)
				return null;
			var name = parameter.Name;
			if (string.IsNullOrEmpty(name))
				return null;

			int nameOffset = 0;
			name = name + ":";
			return CreateUIObject(name, GetExtendedClassifierContentType(), new ParameterNameSignatureHelpClassifierContext(session, parameter, nameOffset));
		}

		object CreateParameterDocumentationObject() {
			if (session.IsDismissed)
				return null;

			var parameter = session.SelectedSignature?.CurrentParameter;
			if (parameter == null)
				return null;
			var text = parameter.Documentation;
			if (string.IsNullOrEmpty(text))
				return null;

			return CreateUIObject(text, GetExtendedClassifierContentType(), new ParameterDocumentationSignatureHelpClassifierContext(session, parameter));
		}

		public bool ExecuteKeyboardCommand(IntellisenseKeyboardCommand command) {
			switch (command) {
			case IntellisenseKeyboardCommand.Escape:
				session.Dismiss();
				return true;

			case IntellisenseKeyboardCommand.Up:
				if (session.Signatures.Count > 1) {
					IncrementSelectedSignature(-1);
					return true;
				}
				return false;

			case IntellisenseKeyboardCommand.Down:
				if (session.Signatures.Count > 1) {
					IncrementSelectedSignature(1);
					return true;
				}
				return false;

			case IntellisenseKeyboardCommand.PageUp:
			case IntellisenseKeyboardCommand.PageDown:
			case IntellisenseKeyboardCommand.Home:
			case IntellisenseKeyboardCommand.End:
			case IntellisenseKeyboardCommand.TopLine:
			case IntellisenseKeyboardCommand.BottomLine:
			case IntellisenseKeyboardCommand.Enter:
			case IntellisenseKeyboardCommand.IncreaseFilterLevel:
			case IntellisenseKeyboardCommand.DecreaseFilterLevel:
			default:
				return false;
			}
		}

		void IncrementSelectedSignature(int count) {
			var sigs = session.Signatures;
			int sigCount = sigs.Count;
			Debug.Assert(sigCount != 0, "Should've been Dismiss()'ed");
			if (sigCount == 0)
				return;
			int index = sigs.IndexOf(session.SelectedSignature);
			Debug.Assert(index >= 0);
			if (index < 0)
				index = 0;
			else
				index += count;
			index %= sigCount;
			if (index < 0)
				index += sigCount;
			session.SelectedSignature = sigs[index];
		}

		void Session_Dismissed(object sender, EventArgs e) {
			signatureTextBuffer.Properties.RemoveProperty(SignatureHelpConstants.SessionBufferKey);
			otherTextBuffer.Properties.RemoveProperty(SignatureHelpConstants.SessionBufferKey);
			otherTextBuffer.Properties.RemoveProperty(SignatureHelpConstants.SignatureHelpClassifierContextBufferKey);
			classificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
			session.Dismissed -= Session_Dismissed;
			session.SelectedSignatureChanged -= Session_SelectedSignatureChanged;
			control.MouseDown -= Control_MouseDown;
			((INotifyCollectionChanged)session.Signatures).CollectionChanged -= Signatures_CollectionChanged;
			(otherClassifier as IDisposable)?.Dispose();
			UnregisterCurrentSignature();
			UnregisterSignatureClassifierEvents();
		}
	}
}
