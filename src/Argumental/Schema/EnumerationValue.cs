using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Argumental
{
  public class EnumerationValue
  {
    public bool Hidden { get; }
    public string Name { get; }
    public object Value { get; }
    public string Description { get; }

    public EnumerationValue(FieldInfo field)
    {
      var displayAttr = field.GetCustomAttribute<DisplayAttribute>();
      var descripAttr = field.GetCustomAttribute<DescriptionAttribute>();
      var browsable = field.GetCustomAttribute<BrowsableAttribute>();
      var editorBrowsable = field.GetCustomAttribute<EditorBrowsableAttribute>();
      Hidden = browsable?.Browsable == false
        || editorBrowsable?.State == EditorBrowsableState.Never;
      Name = field.Name;
      Value = field.GetRawConstantValue();
      Description = displayAttr?.Description ?? descripAttr?.Description;
    }
  }
}
