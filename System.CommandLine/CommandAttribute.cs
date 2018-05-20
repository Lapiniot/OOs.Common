namespace System.CommandLine
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class CommandAttribute : Attribute
    {
        public CommandAttribute(string name) => Name = name;

        public string Name { get; }

        public bool Default { get; set; }
    }
}