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

        public void AddPageContent(PageContent pageContent)
        {
            AddPageContent(pageContent, _templateNode);
        }


        public void AddPageContent(PageContent pageContent, HtmlNode? parentNode)
        {
            if (pageContent == null)
            {
                throw new ArgumentNullException(nameof(pageContent));
            }

            if (parentNode == null)
            {
                throw new ArgumentNullException(nameof(parentNode));
            }

            if (pageContent.SectionType == ContentType.Tab)
            {
                BuildTabs(pageContent, parentNode);
                return;
            }
            
            if (pageContent.SectionType == ContentType.Panel)
            {
                BuildPanel(pageContent, parentNode);
                return;
            }
            
            if (pageContent.SectionType == ContentType.Form)
            {
                BuildForm(pageContent, parentNode);
                return;
            }
            
            if (pageContent.SectionType == ContentType.Grid)
            {
                BuildGrid(pageContent, parentNode);
                return;
            }
            
            if (pageContent.ChildContent == null)
            {
                return;
            }

            foreach (var childContent in pageContent.ChildContent)
            {
                AddPageContent(childContent, parentNode);
            }
        }

        private void BuildPanel(PageContent pageContent, HtmlNode parentNode)
        {
            if (pageContent == null)
            {
                return;
            }

            var isLineDetail = pageContent.Caption == "Line Details";

            var panel = _document.CreateElement("qp-panel");
            panel.SetAttributeValue("id", pageContent.DataMember);
            panel.SetAttributeValue("title", pageContent.Caption);
            panel.SetAttributeValue("auto-repaint", "true");
            panel.SetAttributeValue("width", "80vw");
            panel.SetAttributeValue("height", "80vh");

            if (isLineDetail)
            {
                BuildPanelLineDetails(pageContent, panel);
                parentNode.AppendChild(panel);
                return;
            }

            foreach (var childContent in pageContent.ChildContent)
            {
                if (childContent.SectionType == ContentType.Form)
                {
                    BuildForm(childContent, panel);
                }

                if (childContent.SectionType == ContentType.Grid)
                {
                    BuildGrid(childContent, panel);
                }
            }


            // TODO: buttons

            parentNode.AppendChild(panel);
        }

        private void BuildPanelLineDetails(PageContent pageContent, HtmlNode parentNode)
        {
            var form = _document.CreateElement("div");
            form.SetAttributeValue("class", "h-stack");

            // Form Column 1
            var formFieldSet1 = MakeFieldSetNode(
                        id: "ss-first",
                        dataMember: "LineSplittingExtension_LotSerOptions",
                        caption: null,
                        cssClass: "col-sm-12 col-md-6 col-lg-6",
                        includeWGContainer: true);
            formFieldSet1.AppendChild(MakeFieldNode("UnassignedQty"));
            formFieldSet1.AppendChild(MakeFieldNode("Qty"));
            form.AppendChild(formFieldSet1);

            // Form Column 2
            var formFieldSet2 = MakeFieldSetNode(
            id: "ss-second",
            dataMember: "LineSplittingExtension_LotSerOptions",
            caption: null,
            cssClass: "col-sm-12 col-md-6 col-lg-6",
            includeWGContainer: true);
            formFieldSet2.AppendChild(MakeFieldNode("StartNumVal"));
            var btnField = MakeFieldNode("btnGenerateNbr");
            var btnDiv = _document.CreateElement("div");
            btnDiv.SetAttributeValue("class", "qp-field qp-field-wrapper");
            var btnDivLabel = _document.CreateElement("div");
            btnDivLabel.SetAttributeValue("class", "label-container");
            btnDiv.AppendChild(btnDivLabel);
            var qpButton = _document.CreateElement("qp-button");
            qpButton.SetAttributeValue("id", "btnGenerate");
            qpButton.SetAttributeValue("class", "control-container size-default");
            qpButton.SetAttributeValue("state.bind", "LineSplittingExtension_GenerateNumbers");
            btnDiv.AppendChild(qpButton);
            btnField.AppendChild(btnDiv);
            formFieldSet2.AppendChild(btnField);
            form.AppendChild(formFieldSet2);

            // Grid
            var gridContent = pageContent.ChildContent?.Where(c => c.SectionType == ContentType.Grid)?.FirstOrDefault();
            var grid = MakeGridNode(id: "gridSplits", cssClass: "stretch", dataMember: gridContent?.DataMember);


            // button footer
            var footer = _document.CreateElement("footer");
            var footerButton = _document.CreateElement("qp-button");
            footerButton.SetAttributeValue("id", "btnOK");
            footerButton.SetAttributeValue("config.bind", "{text: SysMessages.Confirm, dialogResult: 1}");
            footer.AppendChild(footerButton);

            parentNode.AppendChild(form);
            parentNode.AppendChild(grid);
            parentNode.AppendChild(footer);
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
                    fieldSetNode = MakeFieldSetNode(
                        id: $"fields{fieldSetCounter}C{field.Column}", 
                        dataMember: pageContent.DataMember,
                        caption: field.Section);
                }

                var fieldNode = MakeFieldNode(field.Name);
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
                if (nextField != null && !nextField.HasSameColumn(field))
                {
                    results[field.Column] = columnFieldSets;
                    columnFieldSets = new List<HtmlNode>();
                }
            }

            return results;
        }

        private HtmlNode MakeFieldNode(string? fieldName)
        {
            var fieldNode = _document.CreateElement("field");
            fieldNode.SetAttributeValue("name", fieldName);
            return fieldNode;
        }

        private HtmlNode MakeFieldSetNode(string? id, string? dataMember, string? caption)
        {
            return MakeFieldSetNode(id, dataMember, caption, null, false);
        }

        private HtmlNode MakeFieldSetNode(string? id, string? dataMember, string? caption, string? cssClass, bool includeWGContainer)
        {
            var fieldSetNode = _document.CreateElement("qp-fieldset");
            fieldSetNode.SetAttributeValue("id", id);
            if (includeWGContainer)
            {
                fieldSetNode.SetAttributeValue("wg-container", "");
            }
            fieldSetNode.SetAttributeValue("view.bind", dataMember);
            if (!string.IsNullOrWhiteSpace(caption))
            {
                fieldSetNode.SetAttributeValue("caption", caption);
            }
            if (!string.IsNullOrWhiteSpace(cssClass))
            {
                fieldSetNode.SetAttributeValue("class", cssClass);
            }
            return fieldSetNode;
        }

        private void BuildGrid(PageContent pageContent, HtmlNode parentNode)
        {
            var gridNode = MakeGridNode(id: pageContent.ID, cssClass: "stretch", dataMember: pageContent.DataMember);

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

        private HtmlNode MakeGridNode(string? id, string? cssClass, string? dataMember)
        {
            var gridNode = _document.CreateElement("qp-grid");
            if (!string.IsNullOrWhiteSpace(id))
            {
                gridNode.SetAttributeValue("id", id);
            }
            if (!string.IsNullOrWhiteSpace(cssClass))
            {
                gridNode.SetAttributeValue("class", cssClass);
            }
            gridNode.SetAttributeValue("view.bind", dataMember);
            return gridNode;
        }
    }
}
