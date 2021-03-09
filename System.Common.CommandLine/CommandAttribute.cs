namespace System.Common.CommandLine
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class CommandAttribute : Attribute, ICommandMetadata
    {
        public CommandAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public bool Default { get; set; }
    }
}