
namespace ModernUIConverter
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Field
    {
        internal string DebuggerDisplay => $"{Name} - {FieldOrder}";

        public string? Name { get; set; }
        public int Column { get; set; }
        /// <summary>
        /// The section the field is found within (Ex: First column could have 3 sections)
        /// </summary>
        public string? Section { get; set; }
        public string? LinkCommand { get; set; }
        public bool AllowEdit { get; set; }
        public bool CommitChanges { get; set; }
        public bool AutoRefresh { get; set; }
        public string DataMemeber { get; set; }
        public int FieldOrder { get; set; }

        /// <summary>
        /// Merge covers the repeat of the same field under such examples as grids with columns and rowtemplates
        /// </summary>
        /// <param name="fieldMerge"></param>
        public void Merge(Field fieldMerge)
        {
            if (fieldMerge == null)
            {
                return;
            }

            DataMemeber = fieldMerge.DataMemeber ?? DataMemeber;
            Column = Math.Max(fieldMerge.Column, Column);
            FieldOrder = Math.Min(fieldMerge.FieldOrder, FieldOrder);
            DataMemeber = fieldMerge.DataMemeber ?? DataMemeber;
            Section = string.IsNullOrWhiteSpace(fieldMerge.Section) ? Section : fieldMerge.Section;
            LinkCommand = string.IsNullOrWhiteSpace(fieldMerge.LinkCommand) ? LinkCommand : fieldMerge.LinkCommand;
            Section = string.IsNullOrWhiteSpace(fieldMerge.Section) ? Section : fieldMerge.Section;
            AllowEdit |= fieldMerge.AllowEdit;
            CommitChanges |= fieldMerge.CommitChanges;
            AutoRefresh |= fieldMerge.AutoRefresh;
        }
    }
}
