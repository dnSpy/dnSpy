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

using System.Globalization;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.UI;
using dnSpy.Debugger.UI.Wpf;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	interface IValueNodesContext {
		UIDispatcher UIDispatcher { get; }
		IEditValueNodeExpression EditValueNodeExpression { get; }
		DbgValueNodeImageReferenceService ValueNodeImageReferenceService { get; }
		DbgValueNodeReader ValueNodeReader { get; }
		IClassificationFormatMap ClassificationFormatMap { get; }
		ITextBlockContentInfoFactory TextBlockContentInfoFactory { get; }
		TextClassifierTextColorWriter TextClassifierTextColorWriter { get; }
		int UIVersion { get; }
		ValueNodeFormatter Formatter { get; }
		bool SyntaxHighlight { get; }
		bool HighlightChangedVariables { get; }
		DbgValueNodeFormatParameters ValueNodeFormatParameters { get; }
		RefreshNodeOptions RefreshNodeOptions { get; set; }
		string WindowContentType { get; }
		string NameColumnName { get; }
		string ValueColumnName { get; }
		string TypeColumnName { get; }
		ShowMessageBox ShowMessageBox { get; }
		LanguageEditValueProvider ValueEditValueProvider { get; }
		LanguageEditValueProvider NameEditValueProvider { get; }
		DbgEvaluationContext EvaluationContext { get; }
		DbgStackFrame StackFrame { get; }
		DbgEvaluationOptions EvaluationOptions { get; }
		DbgValueNodeEvaluationOptions ValueNodeEvaluationOptions { get; }
		string ExpressionToEdit { get; set; }
		bool IsWindowReadOnly { get; }
		CultureInfo FormatCulture { get; }
	}

	sealed class ValueNodesContext : IValueNodesContext {
		public UIDispatcher UIDispatcher { get; }
		public IEditValueNodeExpression EditValueNodeExpression { get; }
		public DbgValueNodeImageReferenceService ValueNodeImageReferenceService { get; }
		public DbgValueNodeReader ValueNodeReader { get; }
		public IClassificationFormatMap ClassificationFormatMap { get; }
		public ITextBlockContentInfoFactory TextBlockContentInfoFactory { get; }
		public TextClassifierTextColorWriter TextClassifierTextColorWriter { get; }
		public int UIVersion { get; set; }
		public ValueNodeFormatter Formatter { get; }
		public bool SyntaxHighlight { get; set; }
		public bool HighlightChangedVariables { get; set; }
		public DbgValueNodeFormatParameters ValueNodeFormatParameters { get; }
		public RefreshNodeOptions RefreshNodeOptions { get; set; }
		public string WindowContentType { get; }
		public string NameColumnName { get; }
		public string ValueColumnName { get; }
		public string TypeColumnName { get; }
		public ShowMessageBox ShowMessageBox { get; }
		public LanguageEditValueProvider ValueEditValueProvider { get; }
		public LanguageEditValueProvider NameEditValueProvider { get; }
		public DbgEvaluationContext EvaluationContext { get; set; }
		public DbgStackFrame StackFrame { get; set; }
		public DbgEvaluationOptions EvaluationOptions { get; set; }
		public DbgValueNodeEvaluationOptions ValueNodeEvaluationOptions { get; set; }
		public string ExpressionToEdit { get; set; }
		public bool IsWindowReadOnly { get; set; }
		public CultureInfo FormatCulture { get; }

		public ValueNodesContext(UIDispatcher uiDispatcher, IEditValueNodeExpression editValueNodeExpression, string windowContentType, string nameColumnName, string valueColumnName, string typeColumnName, LanguageEditValueProviderFactory languageEditValueProviderFactory, DbgValueNodeImageReferenceService dbgValueNodeImageReferenceService, DbgValueNodeReader dbgValueNodeReader, IClassificationFormatMap classificationFormatMap, ITextBlockContentInfoFactory textBlockContentInfoFactory, CultureInfo formatCulture, ShowMessageBox showMessageBox) {
			UIDispatcher = uiDispatcher;
			EditValueNodeExpression = editValueNodeExpression;
			WindowContentType = windowContentType;
			NameColumnName = nameColumnName;
			ValueColumnName = valueColumnName;
			TypeColumnName = typeColumnName;
			ShowMessageBox = showMessageBox;
			ValueEditValueProvider = languageEditValueProviderFactory.Create(windowContentType);
			NameEditValueProvider = languageEditValueProviderFactory.Create(windowContentType);
			ValueNodeImageReferenceService = dbgValueNodeImageReferenceService;
			ValueNodeReader = dbgValueNodeReader;
			ClassificationFormatMap = classificationFormatMap;
			TextBlockContentInfoFactory = textBlockContentInfoFactory;
			TextClassifierTextColorWriter = new TextClassifierTextColorWriter();
			Formatter = new ValueNodeFormatter();
			ValueNodeFormatParameters = new DbgValueNodeFormatParameters();
			FormatCulture = formatCulture;
		}
	}
}
