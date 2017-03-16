using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace OlegShilo.CSScript
{
    static class Constants
    {
        public const String CSS_DIRECTIVE_CLASSIF_NAME = "CS-Script - Directive";
    }

    public static class CSSDirectiveClassificationDefinition
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name(Constants.CSS_DIRECTIVE_CLASSIF_NAME)]
        internal static ClassificationTypeDefinition CSSDirectiveClassificationType = null;
    }
    
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.CSS_DIRECTIVE_CLASSIF_NAME)]
    [Name(Constants.CSS_DIRECTIVE_CLASSIF_NAME)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    public sealed class CSSDirectiveFormat : ClassificationFormatDefinition
    {
        public CSSDirectiveFormat()
        {
            this.ForegroundColor = Colors.Gray;
            this.IsBold = false;
            this.IsItalic = false;
        }
    }

    class KeywordTagger : ITagger<ClassificationTag>
    {
        private ClassificationTag cssClassification;
        private ITagAggregator<ClassificationTag> aggregator;
        private static readonly IList<ClassificationSpan> EmptyList = new List<ClassificationSpan>();

#pragma warning disable 67
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
#pragma warning restore 67

        internal KeywordTagger(IClassificationTypeRegistryService registry, ITagAggregator<ClassificationTag> aggregator)
        {
            cssClassification = new ClassificationTag(registry.GetClassificationType(Constants.CSS_DIRECTIVE_CLASSIF_NAME));
            this.aggregator = aggregator;
        }

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            ITextSnapshot snapshot = spans[0].Snapshot;
            var allSpans = from mappedSpan in aggregator.GetTags(spans)
                           let cs = mappedSpan.Span.GetSpans(snapshot)
                           where cs.Count > 0
                           select cs[0];

            foreach (var span in allSpans)
            {
                string text = span.GetText();

                if (text.TrimStart().StartsWith("//css_"))
                    yield return new TagSpan<ClassificationTag>(span, cssClassification);
            }
        }
    }
}