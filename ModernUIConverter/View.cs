
namespace ModernUIConverter
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class View
    {
        internal string DebuggerDisplay => $"{Name}";

        public string Name { get; private set; }
        public bool IsCollection { get; private set; }
        public bool SyncPosition { get; set; }
        public List<Field> Fields { get; set; }

        public View(string name, bool isCollection)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            IsCollection = isCollection;
            Fields = new List<Field>();
        }

        public void AddFields(IEnumerable<Field> fields)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            foreach (var field in fields)
            {
                AddField(field);
            }
        }

        public void AddField(Field field)
        {
            if (field?.Name == null)
            {
                return;
            }

            var existingField = FindField(field.Name);
            if (existingField != null)
            {
                field.Merge(existingField);
                Fields.Remove(existingField);
            }

            Fields.Add(field);
        }

        public Field? FindField(string name) => string.IsNullOrWhiteSpace(name) ? null : Fields.Where(f => f.Name.ToUpper() == name.ToUpper()).FirstOrDefault();
    }
}
