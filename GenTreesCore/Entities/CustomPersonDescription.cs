
namespace GenTreesCore.Entities
{
    public class CustomPersonDescriptionTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TemplateType Type { get; set; }
    }

    public class CustomPersonDescription
    {
        public int Id { get; set; }
        public CustomPersonDescriptionTemplate Template { get; set; }
        public string Value { get; set; }
    }

    public enum TemplateType
    {
        Number,
        String,
        Image
    }
}
