namespace System.Common.CommandLine
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class ArgumentAttribute : Attribute, IArgumentMetadata
    {
        public ArgumentAttribute(string name, Type type)
        {
            Name = !string.IsNullOrEmpty(name)
                ? name
                : throw new ArgumentException($"{nameof(name)} cannot be null or empty");
            Type = type ?? throw new ArgumentException($"{nameof(type)} cannot be null");
        }

        public ArgumentAttribute(string name, Type type, string shortName) : this(name, type)
        {
            ShortName = shortName;
        }

        public string Name { get; set; }

        public Type Type { get; set; }

        public object DefaultValue { get; set; }

        public bool Optional { get; set; }

        public string ShortName { get; set; }

        public string Description { get; set; }
    }
}