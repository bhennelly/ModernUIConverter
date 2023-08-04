using System.Text;

namespace ModernUIConverter
{
    public static class ExtensionMethods
    {
        public static string? Indent(this string? originalString, int indentLevel)
        {
            if (indentLevel <= 0 || originalString == null)
            {
                return originalString;
            }
            
            var sb = new StringBuilder();
            for (int i = 0; i < indentLevel; i++)
            {
                sb.Append("\t");
            }
            sb.Append(originalString);
            return sb.ToString();
        }

        public static bool HasSameSection(this Field field1,  Field field2)
        {
            return field1 != null && field2 != null && field1.Section == field2.Section;
        }

        public static bool HasSameColumn(this Field field1, Field field2)
        {
            return field1 != null && field2 != null && field1.Column == field2.Column;
        }
    }
}
