using System;
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
                DescriptionTemplates = tree.CustomPersonDescriptionTemplates
            };

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
                CustomDescriptions = person.CustomDescriptions
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
        public ChildRelationViewModel ToViewModel(ChildRelation relation)
        {
            var childRelationModel = new ChildRelationViewModel
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

        public SpouseRelationViewModel ToViewModel(SpouseRelation relation)
        {
            return new SpouseRelationViewModel
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

        #region ToEntity
        public Person ToEntity(PersonViewModel model, GenTree tree)
        {
            var entity = new Person();
            ApplyModelData(entity, model, tree);
            return entity;
        }

        public Relation ToEntity(RelationViewModel model, GenTree tree)
        {

            if (model.GetType() == typeof(SpouseRelationViewModel))
                return new SpouseRelation
                {
                    TargetPerson = tree.Persons.FirstOrDefault(p => p.Id == model.TargetPersonId),
                    IsFinished = (model as SpouseRelationViewModel).IsFinished
                };
            else if (model.GetType() == typeof(ChildRelationViewModel))
            {
                return new ChildRelation
                {
                    TargetPerson = tree.Persons.FirstOrDefault(p => p.Id == model.TargetPersonId),
                    RelationRate = (RelationRate)Enum.Parse(typeof(RelationRate), (model as ChildRelationViewModel).RelationRate),
                    SecondParent = tree.Persons.FirstOrDefault(p => p.Id == (model as ChildRelationViewModel).SecondParentId)
                };
            }
            else return null;
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

        public void ApplyModelData(Person entity, PersonViewModel model, GenTree tree)
        {
            entity.LastName = model.LastName;
            entity.FirstName = model.FirstName;
            entity.MiddleName = model.MiddleName;
            entity.Biography = model.Biography;
            entity.Gender = model.Gender;
            entity.Image = model.Image;

            //обновляем дату рождения
            if (model.BirthDate == null)
                entity.BirthDate = null;
            else
            {
                if (entity.BirthDate == null)
                    entity.BirthDate = new GenTreeDateTime();
                ApplyModelData(entity.BirthDate, model.BirthDate, tree);
            }

            //обновляем дату смерти
            if (model.DeathDate == null)
                entity.DeathDate = null;
            else
            {
                if (entity.DeathDate == null)
                    entity.DeathDate = new GenTreeDateTime();
                ApplyModelData(entity.DeathDate, model.DeathDate, tree);
            }
        }

        public void ApplyModelData(CustomPersonDescription entity, CustomPersonDescription model)
        {
            if (model == null || entity == null) return;
            entity.Value = model.Value;
        }

        public void ApplyModelData(CustomPersonDescriptionTemplate entity, CustomPersonDescriptionTemplate model)
        {
            if (model == null || entity == null) return;
            entity.Name = model.Name;
            entity.Type = model.Type;
        }

        public void ApplyModelData(Relation entity, RelationViewModel model)
        {
            if (model == null || entity == null) return;
            if (model is SpouseRelationViewModel && entity is SpouseRelation)
                (entity as SpouseRelation).IsFinished = (model as SpouseRelationViewModel).IsFinished;
        }

        public void ApplyModelData(GenTreeDateTime entity, GenTreeDateViewModel model, GenTree tree)
        {
            if (model == null || entity == null) return;
            entity.Era = tree.GenTreeDateTimeSetting.Eras.FirstOrDefault(era => era.Id == model.EraId);
            if (entity.Era == null) return;
            entity.Year = model.Year;
            entity.Month = model.Month;
            entity.Day = model.Day;
            entity.Hour = model.Hour;
            entity.Minute = model.Minute;
            entity.Second = model.Second;
        }

        public void ApplyModelData(GenTreeDateTimeSetting entity, GenTreeDateTimeSetting model)
        {
            entity.Name = model.Name;
            entity.IsPrivate = model.IsPrivate;
            entity.YearMonthCount = model.YearMonthCount;
        }

        public void ApplyModelData(GenTreeEra entity, GenTreeEra model)
        {
            entity.Name = model.Name;
            entity.ShortName = model.ShortName;
            entity.Description = model.Description;
            entity.ThroughBeginYear = model.ThroughBeginYear;
            entity.YearCount = model.YearCount;
        }
        #endregion
    }
}
