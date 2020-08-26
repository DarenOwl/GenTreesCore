using System.Collections.Generic;
using GenTreesCore.Entities;

namespace GenTreesCore.Models
{
    public class GenTreeViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Creator { get; set; }
        public bool CanEdit { get; set; }
        public bool IsPrivate { get; set; }
        public List<PersonViewModel> Persons { get; set; }
        public string DateCreated { get; set; }
        public string LastUpdated { get; set; }
        public string Image { get; set; }
        public GenTreeDateTimeSetting DateTimeSetting { get; set; }
        public List<CustomPersonDescriptionTemplate> DescriptionTemplates {get; set;}
    }

    public class PersonViewModel
    {
        public int Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Gender { get; set; }
        public GenTreeDateViewModel BirthDate { get; set; }
        public GenTreeDateViewModel DeathDate { get; set; }
        public string BirthPlace { get; set; }
        public string Biography { get; set; }
        public string Image { get; set; }
        public List<DescriptionViewModel> CustomDescriptions { get; set; }
        public List<RelationViewModel> Relations { get; set; }
    }

    public class RelationViewModel
    {
        public int Id { get; set; }
        public int TargetPersonId { get; set; }
        public string RelationType { get { return GetRelationName(); } }

        public virtual string GetRelationName() => this.GetType().ToString();
    }

    public class ChildRelationViewModel: RelationViewModel
    {
        public int? SecondParentId { get; set; }
        public string RelationRate { get; set; }

        public override string GetRelationName()
        {
            return "ChildRelation";
        }
    }

    public class SpouseRelationViewModel: RelationViewModel
    {
        public bool IsFinished { get; set; }

        public override string GetRelationName()
        {
            return "SpouseRelation";
        }
    }

    public class GenTreeDateViewModel
    {
        public int Id { get; set; }
        public int EraId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int Second { get; set; }
        public string ShortDate { get; set; }
        public string FullDate { get; set; }
    }

    public class DescriptionViewModel
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string Value { get; set; }
    }
}
