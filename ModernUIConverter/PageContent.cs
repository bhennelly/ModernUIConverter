
namespace ModernUIConverter
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public  class PageContent
    {
        internal string DebuggerDisplay => $"{Tag}-{SectionType}-{ID}";

        public ContentType SectionType { get; private set; }
        public string? ID { get; set; }
        public string? DataMember { get; set; }
        public string? Caption { get; set; }
        public string Tag { get; private set; }
        public List<Field> Fields { get; set; }
        public List<PageContent> ChildContent { get; set; }

        public PageContent(ContentType contentType, string tag)
        {
            SectionType = contentType;
            Tag = tag;
        }
    }
}
