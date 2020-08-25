using System;
using System.Collections.Generic;

namespace GenTreesCore.Entities
{
    public class GenTree : IIdentified
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Person> Persons { get; set; }
        public User Owner { get; set; }
        public bool IsPrivate { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Image { get; set; }

        public GenTreeDateTimeSetting GenTreeDateTimeSetting { get; set; }
        public List<CustomPersonDescriptionTemplate> CustomPersonDescriptionTemplates { get; set; }
    }
}
