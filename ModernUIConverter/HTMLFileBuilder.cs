using HtmlAgilityPack;
using System.Diagnostics;

namespace ModernUIConverter
{
    public class HTMLFileBuilder
    {
        private HtmlDocument _document;
        private HtmlNode _templateNode;

        public HTMLFileBuilder()
        {
            _document = new HtmlDocument();
            _templateNode = _document.CreateElement("template");
            _document.DocumentNode.AppendChild(_templateNode);
        }

        public string GetFileContent() => _document.DocumentNode.OuterHtml;

        public void AddPageContent(PageContent pageContent, HtmlNode? parentNode = null)
        {
            if (pageContent == null)
            {
                return;
            }

            if (parentNode == null)
            {
                parentNode = _templateNode;
            }

            if (pageContent.SectionType == ContentType.Tab)
            {
                BuildTabs(pageContent, parentNode);
            }
            else if (pageContent.SectionType == ContentType.Panel)
            {

            }
            else if (pageContent.SectionType == ContentType.Form)
            {
                BuildForm(pageContent, parentNode);
            }
            else if (pageContent.SectionType == ContentType.Grid)
            {
                BuildGrid(pageContent, parentNode);
            }
            else if (pageContent.ChildContent == null)
            {
                return;
            }
            else
            {
                foreach (var childContent in pageContent.ChildContent)
                {
                    AddPageContent(childContent, parentNode);
                }
            }
        }

        private void BuildTabs(PageContent pageContent, HtmlNode parentNode)
        {
            var tabBar = _document.CreateElement("qp-tabbar");
            tabBar.SetAttributeValue("id", "mainTab");
            tabBar.SetAttributeValue("class", "stretch");

            var isFirst = true;
            foreach (var tabItem in pageContent.ChildContent[0].ChildContent)
            {
                if (string.IsNullOrWhiteSpace(tabItem.ID) && !string.IsNullOrWhiteSpace(tabItem.Caption))
                {
                    tabItem.ID = $"tab{tabItem.Caption.Replace(" ","")}";
                }

                Debug.WriteLine($"Tab: {tabItem.ID} - {tabItem.Caption}");

                if (isFirst && !string.IsNullOrWhiteSpace(tabItem.ID))
                {
                    tabBar.SetAttributeValue("active-tab-id", tabItem.ID);
                    isFirst = false;
                }

                var tabItemNode = _document.CreateElement("qp-tab");
                tabItemNode.SetAttributeValue("id", tabItem.ID);
                tabItemNode.SetAttributeValue("caption", tabItem.Caption);
                tabItemNode.SetAttributeValue("class", "stretch");

                AddPageContent(tabItem, tabItemNode);

                tabBar.AppendChild(tabItemNode);
            }

            parentNode.AppendChild(tabBar);
        }

        private void BuildForm(PageContent pageContent, HtmlNode parentNode)
        {
            var isPrimaryForm = parentNode.Name == "template";
            var pane = isPrimaryForm ? "gray-pane " : "";

            var formNode = _document.CreateElement("div");
            formNode.SetAttributeValue("id", pageContent.ID);
            formNode.SetAttributeValue("class", $"h-stack {pane}col-sm-12 col-md-7 col-lg-9");

            var columnFieldSets = BuildFormFieldSets(pageContent);
            foreach (var cfs in columnFieldSets) 
            {
                var column = cfs.Key;
                var fieldSets = cfs.Value;

                foreach (var fieldSet in fieldSets)
                {
                    formNode.AppendChild(fieldSet);
                }
            }

            if (isPrimaryForm)
            {
                var shinkingPanelNode = _document.CreateElement("qp-shrinking-panel");
                shinkingPanelNode.AppendChild(formNode);
                parentNode.AppendChild(shinkingPanelNode);
                return;
            }

            parentNode.AppendChild(formNode);
        }

        private Dictionary<int, List<HtmlNode>> BuildFormFieldSets(PageContent pageContent)
        {
            var results = new Dictionary<int, List<HtmlNode>>();

            var columnFieldSets = new List<HtmlNode>();

            HtmlNode? fieldSetNode = null;
            var fieldSetCounter = 0;
            for (var i = 0; i < pageContent.Fields.Count; i++)
            {
                var field = pageContent.Fields[i];
                Debug.WriteLine($"Field={field.Name}, Col: {field.Column}, Section: {field.Section}");
                if (fieldSetNode == null)
                {
                    fieldSetCounter++;
                    fieldSetNode = _document.CreateElement("qp-fieldset");
                    fieldSetNode.SetAttributeValue("id", $"fields{fieldSetCounter}C{field.Column}");
                    fieldSetNode.SetAttributeValue("view.bind", pageContent.DataMember);
                    if (!string.IsNullOrWhiteSpace(field.Section))
                    {
                        fieldSetNode.SetAttributeValue("caption", field.Section);
                    }
                }

                var fieldNode = _document.CreateElement("field");
                fieldNode.SetAttributeValue("name", field.Name);
                if (field.AllowEdit)
                {
                    fieldNode.SetAttributeValue("config-allow-edit.bind", "true");
                }
                fieldSetNode.AppendChild(fieldNode);

                var nextField = i+1 == pageContent.Fields.Count ? null : pageContent.Fields[i + 1];
                if (nextField != null && nextField.HasSameColumn(field) && nextField.HasSameSection(field))
                {
                    continue;
                }

                // Reset for next field set
                columnFieldSets.Add(fieldSetNode);
                fieldSetNode = null;
                if (!nextField.HasSameColumn(field))
                {
                    results[field.Column] = columnFieldSets;
                    columnFieldSets = new List<HtmlNode>();
                }
            }

            return results;
        }

        private void BuildGrid(PageContent pageContent, HtmlNode parentNode)
        {
            var gridNode = _document.CreateElement("qp-grid");
            if (!string.IsNullOrWhiteSpace(pageContent.ID))
            {
                gridNode.SetAttributeValue("id", pageContent.ID);
            }
            gridNode.SetAttributeValue("class", "stretch");
            gridNode.SetAttributeValue("view.bind", pageContent.DataMember);

            var isPrimaryForm = parentNode.Name == "template";
            if (isPrimaryForm)
            {
                var gridDivNode = _document.CreateElement("div");
                gridDivNode.SetAttributeValue("class", "gray-pane stretch");
                gridDivNode.AppendChild(gridNode);
                parentNode.AppendChild(gridDivNode);
                return;
            }

            parentNode.AppendChild(gridNode);
        }
    }
}
