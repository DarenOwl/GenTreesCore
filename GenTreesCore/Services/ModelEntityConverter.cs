using System;
using System.Collections.Generic;
using System.Linq;
using GenTreesCore.Entities;
using GenTreesCore.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Data.Common;

namespace GenTreesCore.Services
{
    public class ModelEntityConverter
    {
        ApplicationContext db;

        public ModelEntityConverter(ApplicationContext context)
        {
            db = context;
        }
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
                RelationRate = relation.RelationRate.ToString(),
                RelationType = "ChildRelation"
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
                IsFinished = relation.IsFinished,
                RelationType = "SpouseRelation"
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

        #region ToEntityModel
        public Person ToEntityModel(PersonViewModel model)
        {
            return new Person
            {
                LastName = model.LastName,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                BirthPlace = model.BirthPlace,
                Biography = model.Biography,
                Gender = model.Gender,
                Image = model.Image,
                CustomDescriptions = model.CustomDescriptions
            };
        }

        public Relation ToEntityModel(RelationViewModel model, GenTree tree)
        {
            if (model is SpouseRelationViewModel)
                return new SpouseRelation
                {
                    TargetPerson = tree.Persons.FirstOrDefault(p => p.Id == model.TargetPersonId),
                    IsFinished = (model as SpouseRelationViewModel).IsFinished
                };
            else if (model is ChildRelationViewModel)
                return new ChildRelation
                {
                    TargetPerson = tree.Persons.FirstOrDefault(p => p.Id == model.TargetPersonId),
                    RelationRate = (RelationRate)Enum.Parse(typeof(RelationRate), (model as ChildRelationViewModel).RelationRate),
                    SecondParent = tree.Persons.FirstOrDefault(p => p.Id == (model as ChildRelationViewModel).SecondParentId)
                };
            else return null;
        }
        #endregion

        public void UpdateEntity(GenTreeViewModel model, GenTree tree)
        {
            //обновление данных дерева
            tree.Name = model.Name;
            tree.Description = model.Description;
            tree.IsPrivate = model.IsPrivate;
            tree.LastUpdated = DateTime.Now;
            tree.Image = model.Image;

            //обновление шаблонов описаний
            ApplyChanges(tree.CustomPersonDescriptionTemplates, model.DescriptionTemplates, e => e.Id, m => m.Id,
                m => tree.CustomPersonDescriptionTemplates.Add(m), (e, m) => ApplyModelData(e, m),
                e => db.Set<CustomPersonDescriptionTemplate>().Remove(e));

            //обновление людей-узлов дерева
            ApplyChanges(tree.Persons, model.Persons, e => e.Id, m => m.Id,
                m =>
                {
                    var newPerson = new Person();
                    tree.Persons.Add(newPerson);
                    ApplyModelData(newPerson, m, tree);
                },
                (e, m) => ApplyModelData(e, m, tree),
                e => db.Set<Person>().Remove(e));
        }

        #region ApplyModelData
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

            //обновляем пользовательские описания
            ApplyChanges(entity.CustomDescriptions, model.CustomDescriptions, e => e.Id, m => m.Id,
                m =>
                {
                    var template = tree.CustomPersonDescriptionTemplates.FirstOrDefault(t => t.Id == m.Template.Id);
                    if (template != null)
                    {
                        m.Template = template;
                        entity.CustomDescriptions.Add(m);
                    }
                },
                (e, m) => ApplyModelData(e, m), 
                e => db.Set<CustomPersonDescription>().Remove(e));

            //обновляем отношения
            ApplyChanges(entity.Relations, model.Relations, e => e.Id, m => m.Id,
                m => entity.Relations.Add(ToEntityModel(m, tree)),
                (e, m) => ApplyModelData(e, m),
                e => db.Set<Relation>().Remove(e));
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

        #endregion 

        /// <summary>
        /// Для пар модель данных - мдель представления выполняет действие добавления для пары без модели данных и действие обновления для пары view-entity
        /// </summary>
        /// <typeparam name="E">Модель данных</typeparam>
        /// <typeparam name="M">Модель представления</typeparam>
        /// <typeparam name="TKey">Ключ для сравнения</typeparam>
        public void ApplyChanges<E,M,TKey>(IEnumerable<E> entities, IEnumerable<M> models,
            Func<E,TKey> entityKeySelector, Func<M,TKey> modelKeySelector,
            Action<M> add, Action<E,M> update, Action<E> delete)
        {
            if (entities == null && models != null)
            {
                foreach (var model in models)
                    add(model);
                return;
            }

            //формируем коллекцию пар entity-viewmodel
            var collection = from model in models
                             join entity in entities
                             on modelKeySelector(model) equals entityKeySelector(entity)
                             into temp
                             from entityOrNull in temp.DefaultIfEmpty()
                             select new ModelEntityPair<E, M>(entityOrNull, model);

            //применияем действия обновления и добавления
            collection.ToList().ForEach(pair =>
            {
                if (pair.Entity == null)
                    add(pair.Model);
                else
                    update(pair.Entity, pair.Model);
            });

            //удаляем элементы, которые были удалены из модели
            var forDelete = entities
                .Where(e => models.FirstOrDefault(m => modelKeySelector(m).Equals(entityKeySelector(e))) == null).ToList();
            foreach (var element in forDelete)
                delete(element);
        }
    }

    public class ModelEntityPair<TEntity, TModel>
    {
        public TEntity Entity { get; }
        public TModel Model { get; }

        public ModelEntityPair(TEntity entity, TModel model)
        {
            Entity = entity;
            Model = model;
        }
    }
}
