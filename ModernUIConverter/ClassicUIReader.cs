using HtmlAgilityPack;
using System.Diagnostics;

namespace ModernUIConverter
{
    public class ClassicUIReader
    {
        public string ScreenID { get; private set; }
        public string GraphType { get; private set; }
        public string PrimaryView { get; private set; }
        public List<PageContent> PageContents { get; private set; }
        public Dictionary<string, View> Views { get; private set; }

        public ClassicUIReader(string classicAspxFilePath)
        {
            if (!File.Exists(classicAspxFilePath))
            {
                throw new FileNotFoundException($"File {classicAspxFilePath} not found.");
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.Load(classicAspxFilePath);
            ScreenID = Path.GetFileNameWithoutExtension(classicAspxFilePath);
            PageContents = new List<PageContent>();
            Views = new Dictionary<string, View>();
            ReadContents(htmlDoc);
        }

        private void ReadContents(HtmlDocument htmlDoc)
        {
            if (htmlDoc?.DocumentNode?.ChildNodes == null)
            {
                return;
            }

            foreach (var node in htmlDoc.DocumentNode.ChildNodes)
            {
                var content = ConvertToPageContent(node);
                if (content == null)
                {
                    continue;
                }

                PageContents.Add(content);
            }
        }

        private bool IgnoreHtmlNode(HtmlNode node) => node?.Name == null || node.Name.StartsWith("#") || node.Name.ToUpper() == "PX:PXDATASOURCE";

        private PageContent? ConvertToPageContent(HtmlNode node)
        {
            if (IgnoreHtmlNode(node))
            {
                return null;
            }

            var syncPosition = false;
            var pageContent = new PageContent(GetContentType(node), node.Name);
            foreach(var attribute in node.Attributes)
            {
                if (string.IsNullOrEmpty(attribute?.Name))
                {
                    continue;
                }

                switch (attribute.Name.ToUpper())
                {
                    case "ID":
                        pageContent.ID = attribute.Value;
                        break;
                    case "DATAMEMBER":
                        pageContent.DataMember = attribute.Value;
                        break;
                    case "TEXT":
                    case "CAPTION":
                        pageContent.Caption = attribute.Value;
                        break;
                    case "SYNCPOSITION":
                        syncPosition = attribute.Value.ToUpper() == "TRUE";
                        break;
                }
            }

            Debug.WriteLine($"{node?.Name}-{pageContent.DebuggerDisplay}");

            if (string.IsNullOrEmpty(GraphType) && node.Name.ToUpper() == "ASP:CONTENT")
            {
                SetGraphType(node);
            }

            var fields = new Dictionary<string, Field>();
            var childPageContent = new List<PageContent>();

            var skipNodes = new List<string>();
            if (pageContent.SectionType == ContentType.Grid)
            {
                skipNodes.Add("LEVELS");
                skipNodes.Add("MODE");
                skipNodes.Add("AUTOSIZE");
                skipNodes.Add("ACTIONBAR");
                skipNodes.Add("ROWTEMPLATE");
                skipNodes.Add("PX:PXGRIDLEVEL");
                skipNodes.Add("COLUMNS");

                if (string.IsNullOrWhiteSpace(pageContent.DataMember))
                {
                    var dm = GetPXGridDataMemeber(node);
                    if (dm != null)
                    {
                        pageContent.DataMember = dm;
                    }
                }
            }
            else if (pageContent.SectionType == ContentType.Form)
            {
                skipNodes.Add("TEMPLATE");
            }
            else if (pageContent.SectionType == ContentType.Tab)
            {
                skipNodes.Add("AUTOSIZE");
            }

            var isFieldMode = false;
            var startFieldOrder = false;
            var fieldOrder = 0;
            var column = 0;
            var fieldGroup = string.Empty;
            foreach (var child in GetChildNodes(node, skipNodes.ToArray()))
            {
                if (child?.Name != null && child.Name.ToUpper() == "PX:PXGRIDCOLUMN")
                {
                    isFieldMode = true;
                    startFieldOrder = true;
                }

                if (child?.Name != null && child.Name.ToUpper() == "PX:PXLAYOUTRULE")
                {
                    isFieldMode = true;
                    startFieldOrder = false;

                    foreach (var att in child.Attributes)
                    {
                        if (string.IsNullOrWhiteSpace(att?.Name))
                        {
                            continue;
                        }

                        if (att.Name.ToUpper() == "STARTCOLUMN")
                        {
                            column++;
                        }

                        fieldGroup = string.Empty;
                        if (att.Name.ToUpper() == "GROUPCAPTION")
                        {
                            fieldGroup = att.Value ?? string.Empty;
                        }
                    }
                }

                if (isFieldMode)
                {
                    var field = GetField(child);
                    if (field?.Name != null)
                    {
                        field.DataMemeber = pageContent.DataMember;
                        field.Column = column;
                        field.Section = fieldGroup;
                        field.FieldOrder = startFieldOrder ? ++fieldOrder : (field.FieldOrder == 0 ? 99 : field.FieldOrder);

                        if (fields.TryGetValue(field.Name, out var foundField))
                        {
                            field.Merge(foundField);
                        }

                        fields[field.Name] = field;
                    }

                    continue;
                }

                var childContent = ConvertToPageContent(child);
                if (childContent != null)
                {
                   childPageContent.Add(childContent);
                }
            }
            pageContent.Fields = fields.Values.ToList();
            pageContent.ChildContent = childPageContent;

            if (!string.IsNullOrWhiteSpace(pageContent.DataMember) && pageContent.Fields.Count > 0)
            {
                var view = Views.TryGetValue(pageContent.DataMember, out var existingView) 
                    ? existingView 
                    : new View(pageContent.DataMember, pageContent.SectionType == ContentType.Grid);
                view.SyncPosition |= syncPosition;
                view.AddFields(pageContent.Fields);
                Views[pageContent.DataMember] = view;
            }

            if (pageContent.ChildContent.Count == 0 && pageContent.Fields.Count == 0)
            {
                return null;
            }

            return pageContent;
        }

        private IEnumerable<HtmlNode> GetChildNodes(HtmlNode node, params string[] skipChildContent)
        {
            foreach (var child in node.ChildNodes)
            {
                if (IgnoreHtmlNode(child))
                {
                    continue;
                }

                if (skipChildContent != null && skipChildContent.Contains(child.Name.ToUpper()))
                {
                    foreach (var childNode in child.ChildNodes)
                    {
                        if (IgnoreHtmlNode(childNode))
                        {
                            continue;
                        }

                        foreach (var item in GetChildNodes(childNode, skipChildContent))
                        {
                            yield return item;
                        }

                        yield return childNode;
                    }
                }

                yield return child;
            }
        }

        private string? GetPXGridDataMemeber(HtmlNode node)
        {
            if (node?.Name == null || node.Name.ToUpper() != "PX:PXGRID")
            {
                return null;
            }

            var gridLevelNode = node.ChildNodes?.Where(c => c.Name.ToUpper() == "LEVELS").FirstOrDefault()?.ChildNodes?.Where(c => c.Name.ToUpper() == "PX:PXGRIDLEVEL").FirstOrDefault();
            return gridLevelNode?.Attributes == null ? null : gridLevelNode.Attributes.Where(a => a.Name.ToUpper() == "DATAMEMBER").FirstOrDefault()?.Value;
        }

        private ContentType GetContentType(HtmlNode node)
        {
            if (node?.Name == null)
            {
                return ContentType.GeneralSection;
            }

            switch (node?.Name.ToUpper())
            {
                case "PX:PXFORMVIEW":
                    return ContentType.Form;
                case "PX:PXTAB":
                    return ContentType.Tab;
                case "PX:PXTABITEM":
                    return ContentType.TabItem;
                case "PX:PXSMARTPANEL":
                    return ContentType.Panel;
                case "ASP:CONTENT":
                    return ContentType.Content;
                case "PX:PXLAYOUTRULE":
                    return ContentType.LayoutRule;
                case "PX:PXGRID":
                case "PX:PXGRIDLEVEL":
                    return ContentType.Grid;
                default:
                    return ContentType.GeneralSection;
            }
        }

        private Field? GetField(HtmlNode node)
        {
            if (string.IsNullOrWhiteSpace(node?.Name))
            {
                return null;
            }

            var field = new Field();
            foreach (var attribute in node.Attributes)
            {
                if (string.IsNullOrEmpty(attribute?.Name))
                {
                    continue;
                }

                switch (attribute.Name.ToUpper())
                {
                    case "DATAMEMEBER":
                        field.DataMemeber = attribute.Value;
                        break;
                    case "DATAFIELD":
                        field.Name = attribute.Value;
                        break;
                    case "COMMITCHANGES":
                        field.CommitChanges = attribute.Value.ToUpper() == "TRUE";
                        break;
                    case "LINKCOMMAND":
                        field.LinkCommand = attribute.Value;
                        break;
                    case "ALLOWEDIT":
                        field.AllowEdit = attribute.Value.ToUpper() == "TRUE";
                        break;
                    case "AUTOREFRESH":
                        field.AutoRefresh = attribute.Value.ToUpper() == "TRUE";
                        break;
                }
            }

            return field?.Name == null ? null : field;
        }

        private void SetGraphType(HtmlNode node)
        {
            if (node?.Name == null)
            {
                return;
            }

            foreach (var attribute in node.ChildNodes?.Where(c => c.Name.ToUpper() == "PX:PXDATASOURCE").FirstOrDefault()?.Attributes)
            {
                if (attribute?.Name?.ToUpper() == "PRIMARYVIEW")
                {
                    PrimaryView = attribute.Value;
                    continue;
                }

                if (attribute?.Name?.ToUpper() == "TYPENAME")
                {
                    GraphType = attribute.Value;
                }
            }
        }
    }
}
