using System.Text;

namespace ModernUIConverter
{
    public class TSFileBuilder
    {
        private StringBuilder _baseContent { get; set; }
        private StringBuilder _viewsContent { get; set; }

        private const string viewFormat = @"export class {0} extends PXView";
        private const string linkCommandFormat = @"@linkCommand(""{0}"") ";

        private List<string> TypicalHideViewLinkFields = new List<string>
        {
            "UOM", "ORDERTYPE", "OPERATIONID", "SITEID", "LOCATIONID", "LOTSERIALNBR"
        };

        public TSFileBuilder(string graphType, string primaryView, string screenID)
        {
            _baseContent = new StringBuilder();
            _baseContent.AppendLine(@"import {
	PXScreen,
	createCollection,
	createSingle,
	graphInfo,
	PXView,
	PXFieldState,
	PXActionState,
	PXFieldOptions,
	columnSettings,
	headerDescription
} from 'client-controls';");
            _baseContent.AppendLine();
            _baseContent.Append(@"@graphInfo({ graphType: '");
            _baseContent.Append(graphType);
            _baseContent.Append(@"', primaryView: '");
            _baseContent.Append(primaryView);
            _baseContent.AppendLine(@"' })");
            _baseContent.AppendLine();
            _baseContent.Append($"export class {screenID} extends PXScreen");
            _baseContent.AppendLine(" {");

            _viewsContent = new StringBuilder();
        }

        public void AddView(View view)
        {
            if (view?.Name == null)
            {
                return;
            }

            _viewsContent.Append(string.Format(viewFormat, view.Name));
            _viewsContent.AppendLine(" {");

            if (view.Fields != null)
            {
                foreach (var field in view.Fields.OrderBy(f => f.FieldOrder))
                {
                    _viewsContent.AppendLine(ConstructFieldState(field, view));
                }
            }

            _viewsContent.AppendLine("}");
            _viewsContent.AppendLine();

            if (view.IsCollection)
            {
                _baseContent.AppendLine($"\t{view.Name} = createCollection(");
                _baseContent.AppendLine($"\t\t{view.Name},");
                _baseContent.AppendLine("\t\t{");
                _baseContent.AppendLine($"\t\t\tsyncPosition: {view.SyncPosition.ToString().ToLower()},");
                _baseContent.AppendLine("\t\t\tadjustPageSize: true,");
                _baseContent.AppendLine("\t\t\tallowInsert: true,");
                _baseContent.AppendLine("\t\t\tallowDelete: true");
                _baseContent.AppendLine("\t\t}");
                _baseContent.AppendLine("\t);");
                _baseContent.AppendLine();
                return;
            }

            _baseContent.AppendLine($"\t{view.Name} = createSingle({view.Name});");
            _baseContent.AppendLine();
        }

        private bool IsTypicalHideViewLink(Field field)
            => field?.Name != null && TypicalHideViewLinkFields.Contains(field.Name.ToUpper());

        private string ConstructFieldState(Field field, View view)
        {
            if (field?.Name == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(field.LinkCommand))
            {
                sb.AppendFormat(linkCommandFormat, field.LinkCommand);
            }

            sb.Append("\t");

            if (view.IsCollection == true && IsTypicalHideViewLink(field))
            {
                sb.Append("@columnSettings({ hideViewLink: true }) ");
            }

            sb.Append(field.Name);
            sb.Append(": PXFieldState");

            if (field.CommitChanges)
            {
                sb.Append("<PXFieldOptions.CommitChanges>");
            }

            sb.Append(";");
            return sb.ToString();
        }

        public string GetFileContent()
        {
            var sb = new System.Text.StringBuilder(_baseContent.ToString());
            sb.AppendLine("}");
            sb.AppendLine();
            sb.Append(_viewsContent.ToString());
            return sb.ToString();
        }
    }
}
