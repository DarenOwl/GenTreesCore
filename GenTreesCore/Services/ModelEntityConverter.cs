using System;
using System.Collections.Generic;
using System.Linq;
using GenTreesCore.Entities;
using GenTreesCore.Models;

namespace GenTreesCore.Services
{
    public class ModelEntityConverter
    {

        #region ToViewModel
        public GenTreeViewModel ToViewModel(GenTree tree)
        {
            var treeModel = new GenTreeViewModel
            {
                Id = tree.Id,
                Name = tree.Name,
                Description = tree.Description,
                Creator = tree.Owner.Login,
                IsPrivate = tree.IsPrivate,
                DateCreated = tree.DateCreated.ToString("d/MM/yyyy"),
                LastUpdated = tree.LastUpdated.ToString("d/MM/yyyy"),
                Image = tree.Image,
                DescriptionTemplates = tree.CustomPersonDescriptionTemplates,
                DateTimeSetting = tree.GenTreeDateTimeSetting
            };

            treeModel.DateTimeSetting.Owner = null;

            if (tree.Persons != null)
                treeModel.Persons = tree.Persons.Select(p => ToViewModel(p)).ToList();

            return treeModel;
        }

        public PersonViewModel ToViewModel(Person person)
        {
            var personModel = new PersonViewModel
            {
                Id = person.Id,
                LastName = person.LastName,
                FirstName = person.FirstName,
                MiddleName = person.MiddleName,
                Gender = person.Gender,
                Biography = person.Biography,
                Image = person.Image,
                CustomDescriptions = person.CustomDescriptions.Select(desc => new DescriptionViewModel 
                    { 
                        Id = desc.Id,
                        TemplateId = desc.Template.Id,
                        Value = desc.Value
                    }).ToList()
            };
            if (person.BirthDate != null)
            {
                personModel.BirthDate = ToViewModel(person.BirthDate);
            }
            if (person.DeathDate != null)
            {
                personModel.DeathDate = ToViewModel(person.DeathDate);
            }

            if (person.Relations != null)
                personModel.Relations = person.Relations.Select(r => ToViewModel(r)).ToList();
            return personModel;
        }

        public RelationViewModel ToViewModel(Relation relation)
        {
            if (relation is ChildRelation)
                return ToViewModel(relation as ChildRelation);
            else
                return ToViewModel(relation as SpouseRelation);
        }
        public RelationViewModel ToViewModel(ChildRelation relation)
        {
            var childRelationModel = new RelationViewModel
            {
                Id = relation.Id,
                TargetPersonId = relation.TargetPerson.Id,
                SecondParentId = null,
                RelationRate = relation.RelationRate.ToString()
            };
            if (relation.SecondParent != null)
                childRelationModel.SecondParentId = relation.SecondParent.Id;
            return childRelationModel;
        }

        public RelationViewModel ToViewModel(SpouseRelation relation)
        {
            return new RelationViewModel
            {
                Id = relation.Id,
                TargetPersonId = relation.TargetPerson.Id,
                IsFinished = relation.IsFinished
            };
        }

        public GenTreeDateViewModel ToViewModel(GenTreeDateTime date)
        {
            return new GenTreeDateViewModel
            {
                Id = date.Id,
                EraId = date.Era.Id,
                Year = date.Year,
                Month = date.Month,
                Day = date.Month,
                Hour = date.Month,
                Minute = date.Minute,
                Second = date.Second,
                ShortDate = date.ToShortDateTimeString(),
                FullDate = date.ToDateTimeString()
            };
        }

        #endregion

        #region ApplyModelData
        public void ApplyModelData(GenTree entity, GenTreeViewModel model)
        {
            entity.Name = model.Name;
            entity.Description = model.Description;
            entity.IsPrivate = model.IsPrivate;
            entity.LastUpdated = DateTime.Now;
            entity.Image = model.Image;
        }
        #endregion
    }
}
