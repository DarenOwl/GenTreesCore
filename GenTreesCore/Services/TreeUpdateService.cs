using GenTreesCore.Entities;
using GenTreesCore.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GenTreesCore.Services
{
    public class TreeUpdateService
    {
        public TreeUpdateResult UpdateResult { get; private set; }

        private readonly ApplicationContext db;
        private readonly ModelEntityConverter converter;

        public TreeUpdateService(ApplicationContext context)
        {
            db = context;
            converter = new ModelEntityConverter();
            UpdateResult = new TreeUpdateResult
            {
                Replacements = new Dictionary<int, int>(),
                Errors = new List<IdError>()
            };
        }

        public void ResetUpdateResult()
        {
            UpdateResult.Replacements = new Dictionary<int, int>();
            UpdateResult.Errors = new List<IdError>();
        }

        public void ApplyDateTimeSetting(GenTreeDateTimeSetting model, GenTree tree)
        {
            if (model == null)
                return;
            else
            {
                /*сначала проверяем, есть ли в БД сеттинг летоисчисления с таким ID*/
                /*косяк - сеттинг может быть со случайным ID, но выберем существующий*/
                var entity = db.GenTreeDateTimeSettings.FirstOrDefault(setting => setting.Id == model.Id
                    && (!setting.IsPrivate || tree.Owner.Id == setting.Owner.Id)); /*проверяем, есть ли доступ к летоисчислению*/

                if (entity == null) /*если в БД нет, добавляем новое*/
                    AddDateTimeSetting(model, tree);
                else
                {
                    tree.GenTreeDateTimeSetting = entity;
                    if (entity.Owner.Id == tree.Owner.Id) /*если есть права - обновляем*/
                        UpdateDateTimeSetting(entity, model);
                }
            }
        }

        public void ApplyDescriptionTemplates(List<CustomPersonDescriptionTemplate> templatesModel, GenTree tree)
        {
            if (tree.CustomPersonDescriptionTemplates == null)
                tree.CustomPersonDescriptionTemplates = new List<CustomPersonDescriptionTemplate>();
            ApplyChanges(tree.CustomPersonDescriptionTemplates, templatesModel, e => e.Id, m => m.Id,
                add: m => AddDescriptionTemplate(m, tree),
                update: (e, m) => converter.ApplyModelData(e, m),
                delete: e => db.Set<CustomPersonDescriptionTemplate>().Remove(e));
        }

        public void ApplyPersons(List<PersonViewModel> personsModel, GenTree tree)
        {
            if (tree.Persons == null)
                tree.Persons = new List<Person>();
            ApplyChanges(tree.Persons, personsModel, e => e.Id, m => m.Id,
                add: m => AddPerson(m, tree),
                update: (e, m) => UpdatePerson(e, m, tree),
                delete: e => db.Set<Person>().Remove(e));
            foreach (var person in personsModel)
                ApplyRelations(person, tree);
        }

        public void ApplyCustomDescriptions(List<CustomPersonDescription> descriptionsModel, Person person, List<CustomPersonDescriptionTemplate> templates)
        {
            if (person.CustomDescriptions == null)
                person.CustomDescriptions = new List<CustomPersonDescription>();
            ApplyChanges(person.CustomDescriptions, descriptionsModel, e => e.Template.Id, m => m.Template.Id,
                add: m => AddDescription(m, person, templates),
                update: (e, m) => converter.ApplyModelData(e, m),
                delete: e => db.Set<CustomPersonDescription>().Remove(e));
        }

        public void ApplyRelations(PersonViewModel model, GenTree tree)
        {
            var person = tree.Persons.FirstOrDefault(p => p.Id == GetDBId(model.Id));
            if (person == null) return;
            if (person.Relations == null)
                person.Relations = new List<Relation>();
            ApplyChanges(person.Relations, model.Relations, e => e.Id, m => m.Id,
                add: m => AddRelation(m, person, tree),
                update: (e, m) => converter.ApplyModelData(e, m),
                delete: e => db.Set<Relation>().Remove(e));
        }

        #region Update
        public void UpdateTree(GenTree tree, GenTreeViewModel model)
        {
            converter.ApplyModelData(tree, model); //обновление свойств
            ApplyDateTimeSetting(model.DateTimeSetting, tree);
            ApplyDescriptionTemplates(model.DescriptionTemplates, tree);
            ApplyPersons(model.Persons, tree);
            var stop = 0;
        }

        public GenTreeDateTime ApplyDate(GenTreeDateTime date, GenTreeDateViewModel model, GenTree tree)
        {
            /*находим соответствующую дате эру*/
            var era = tree.GenTreeDateTimeSetting.Eras.FirstOrDefault(e => e.Id == GetDBId(model.EraId));
            /*временная мера, "конвертирующщая" дату в новый сеттинг (пока просто берем первую попавшуюся эру)*/
            if (era == null) era = tree.GenTreeDateTimeSetting.Eras.FirstOrDefault();
            /*если дату не удалось сконвертировать - удаляем как недействительную*/
            if (era == null)
            {
                db.Set<GenTreeDateTime>().Remove(date);
                return null;
            }
            if (date == null)
                date = new GenTreeDateTime();
            converter.ApplyModelData(date, model, tree);
            return date;
        }

        public void UpdatePerson(Person entity, PersonViewModel model, GenTree tree)
        {
            entity.BirthDate = ApplyDate(entity.BirthDate, model.BirthDate, tree);
            entity.DeathDate = ApplyDate(entity.DeathDate, model.DeathDate, tree);
            //копируем данные модели
            converter.ApplyModelData(entity, model);
            //обновление пользовательских описаний (сопоставление по Id шаблона)
            ApplyCustomDescriptions(model.CustomDescriptions, entity, tree.CustomPersonDescriptionTemplates);
        }

        public void UpdateDateTimeSetting(GenTreeDateTimeSetting entity, GenTreeDateTimeSetting model)
        {
            /*обновление свойств*/
            converter.ApplyModelData(entity, model);

            //обновление списка эр
            if (entity.Eras == null)
                entity.Eras = new List<GenTreeEra>();
            ApplyChanges(entity.Eras, model.Eras, e => e.Id, m => m.Id,
            add: m => AddEra(m, entity),
            update: (e, m) => converter.ApplyModelData(e, m),
            delete: e => db.Set<GenTreeEra>().Remove(e));
        }

        #endregion

        #region Add
        public void AddDateTimeSetting(GenTreeDateTimeSetting model, GenTree tree)
        {
            var entity = new GenTreeDateTimeSetting(); /*создаем новый сеттинг*/
            converter.ApplyModelData(entity, model); /*копируем данные из модели*/
            entity.Owner = tree.Owner; /*устанавливаем ссылку на автора*/
            AddEntity(entity, setting =>
            {
                db.GenTreeDateTimeSettings.Add(setting); /*добавляем в бд*/
                tree.GenTreeDateTimeSetting = entity; /*привязываем к дереву*/
            }, setting => setting.Id, model.Id, onErrorDelete: setting => { });
            /*создаем список эр и добавляем эры из модели */
            if (model.Eras != null)
            {
                entity.Eras = new List<GenTreeEra>();
                model.Eras.ForEach(era => AddEra(era, entity));
            }
        }

        public void AddEra(GenTreeEra model, GenTreeDateTimeSetting setting)
        {
            var era = new GenTreeEra();
            converter.ApplyModelData(era, model); /*копируем данные из модели*/
            AddEntity(era, e => setting.Eras.Add(e), e => e.Id, model.Id,
                onErrorDelete: e => setting.Eras.Remove(e)); /*сохраняем в бд*/
        }

        public void AddDescriptionTemplate(CustomPersonDescriptionTemplate model, GenTree tree)
        {
            var template = new CustomPersonDescriptionTemplate();
            converter.ApplyModelData(template, model);
            AddEntity(template, 
                add: t => tree.CustomPersonDescriptionTemplates.Add(t), t => t.Id, model.Id,
                onErrorDelete: t => tree.CustomPersonDescriptionTemplates.Remove(t));
        }

        public void AddPerson(PersonViewModel model, GenTree tree)
        {
            var person = new Person();
            converter.ApplyModelData(person, model);
            person.BirthDate = ApplyDate(person.BirthDate, model.BirthDate, tree);
            person.DeathDate = ApplyDate(person.DeathDate, model.DeathDate, tree);
            AddEntity(person, 
                add: pers => tree.Persons.Add(pers), pers => pers.Id, model.Id,
                onErrorDelete: pers => tree.Persons.Remove(pers));
            ApplyCustomDescriptions(model.CustomDescriptions, person, tree.CustomPersonDescriptionTemplates);
        }

        public void AddDescription(CustomPersonDescription model, Person person, List<CustomPersonDescriptionTemplate> templates)
        {
            /*получаем шаблон по его Id*/
            var template = templates?.FirstOrDefault(t => t.Id == GetDBId(model.Template.Id));
            if (template == null)
            {
                UpdateResult.Errors.Add(new IdError(model.Id, $"Description refers to unknown Template with id = {model.Template.Id}"));
                return;
            }
            var description = new CustomPersonDescription { Template = template };
            AddEntity(description, desc => person.CustomDescriptions.Add(desc), desc => desc.Id, model.Id,
                onErrorDelete: desc => person.CustomDescriptions.Remove(desc));
        }

        public void AddRelation(RelationViewModel model, Person person, GenTree tree)
        {
            model.TargetPersonId = GetDBId(model.TargetPersonId);
            if (model is ChildRelationViewModel)
                (model as ChildRelationViewModel).SecondParentId = GetDBId((model as ChildRelationViewModel).SecondParentId ?? 0);
            AddEntity(converter.ToEntity(model, tree), rel => person.Relations.Add(rel), rel => rel.Id, model.Id,
                onErrorDelete: rel => person.Relations.Remove(rel));
        }

        #endregion

        /// <summary>
        /// Для пар модель данных - мдель представления выполняет действие добавления для пары без модели данных и действие обновления для пары view-entity
        /// </summary>
        /// <typeparam name="E">Модель данных</typeparam>
        /// <typeparam name="M">Модель представления</typeparam>
        /// <typeparam name="TKey">Ключ для сравнения</typeparam>
        public void ApplyChanges<E, M>(IEnumerable<E> entities, IEnumerable<M> models,
            Func<E, int> entityKeySelector, Func<M, int> modelKeySelector,
            Action<M> add, Action<E, M> update, Action<E> delete)
        {
            if (models == null)
                models = new List<M>(); //такое себе

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
                {
                    add(pair.Model);
                }
                else
                    update(pair.Entity, pair.Model);
            });

            //удаляем элементы, которые были удалены из модели
            var forDelete = entities
                .Where(e => models.FirstOrDefault(m => GetDBId(modelKeySelector(m))== entityKeySelector(e)) == null).ToList();
            foreach (var element in forDelete)
            {
                entities.ToList().Remove(element);
                delete(element);
            }
        }

        public void AddEntity<E>(E entity, Action<E> add, Func<E, int> idSelector, int modelId, Action<E> onErrorDelete)
        {
            try
            {
                //добавляем в бд, сохраняем изменения и записываем новый id
                add(entity);
                db.SaveChanges();
                UpdateResult.Replacements[modelId] = idSelector(entity);
            }
            catch (Exception e)
            {
                //в случае ошибки добавляем в список ошибок
                UpdateResult.Errors.Add(new IdError(modelId, e.InnerException?.Message));
                onErrorDelete(entity);
            }
        }

        public int GetDBId(int id)
        {
            if (UpdateResult.Replacements.ContainsKey(id))
                return UpdateResult.Replacements[id];
            else
                return id;
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
