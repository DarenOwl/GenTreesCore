using GenTreesCore.Entities;
using GenTreesCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GenTreesCore.Services
{
    public class TreeUpdateService
    {
        public TreeUpdateResult UpdateResult { get => updateResult; }

        ApplicationContext db;
        private TreeUpdateResult updateResult;
        ModelEntityConverter converter;

        public TreeUpdateService(ApplicationContext context)
        {
            db = context;
            converter = new ModelEntityConverter();
            updateResult = new TreeUpdateResult
            {
                Replacements = new Dictionary<int, int>(),
                Errors = new List<int>()
            };
        }

        public void ResetUpdateResult()
        {
            updateResult = new TreeUpdateResult
            {
                Replacements = new Dictionary<int, int>(),
                Errors = new List<int>()
            };
        }

        public void UpdateTree(GenTree tree, GenTreeViewModel model)
        {
            //обновление свойств
            converter.ApplyModelData(tree, model);

            //обновление настроек летоисчисления
            if (tree.GenTreeDateTimeSetting == null && model.DateTimeSetting != null)
            {
                var setting = db.GenTreeDateTimeSettings.FirstOrDefault(x => x.Id == model.DateTimeSetting.Id && x.Owner.Id == tree.Owner.Id || !x.IsPrivate);
                tree.GenTreeDateTimeSetting = setting ?? new GenTreeDateTimeSetting();
            }
            if (model.DateTimeSetting == null)
                tree.GenTreeDateTimeSetting = null;
            else
                UpdateDateTimeSetting(tree.GenTreeDateTimeSetting, model.DateTimeSetting);

            //обновление списка шаблонов описаний
            if (tree.CustomPersonDescriptionTemplates == null && model.DescriptionTemplates != null)
                tree.CustomPersonDescriptionTemplates = new List<CustomPersonDescriptionTemplate>();
            ApplyChanges(tree.CustomPersonDescriptionTemplates, model.DescriptionTemplates, e => e.Id, m => m.Id,
                add: m => AddEntity(m, x => tree.CustomPersonDescriptionTemplates.Add(x), x => x.Id, x => m.Id = x, m.Id),
                update: (e, m) => converter.ApplyModelData(e, m),
                delete: e => db.Set<CustomPersonDescriptionTemplate>().Remove(e));

            //обновление списка людей-узлов дерева
            if (tree.Persons == null && model.Persons != null)
                tree.Persons = new List<Person>();
            ApplyChanges(tree.Persons, model.Persons, e => e.Id, m => m.Id,
                add: m => AddEntity(converter.ToEntity(m, tree), x => tree.Persons.Add(x), x => x.Id, x => m.Id = x, m.Id),
                update: (e, m) => UpdatePerson(e, m, tree),
                delete: e => db.Set<Person>().Remove(e));

            //Обновление отношений (обязательно после полного обновления списка людей)
            if (tree.Persons != null)
                foreach (var person in tree.Persons)
                {
                    if (person.Relations == null)
                        person.Relations = new List<Relation>();
                    UpdatePersonRelations(person, model.Persons.FirstOrDefault(x => x.Id == person.Id), tree);
                }
        }

        public void UpdatePerson(Person entity, PersonViewModel model, GenTree tree)
        {
            //производим замены id в моделях дат перед применением модели
            if (model.BirthDate != null)
                model.BirthDate.EraId = GetDBId(model.BirthDate.EraId);
            if (model.DeathDate != null)
                model.DeathDate.EraId = GetDBId(model.DeathDate.EraId);

            //обновление свойств
            converter.ApplyModelData(entity, model, tree);

            //обновление пользовательских описаний (сопоставление по Id шаблона)
            ApplyChanges(entity.CustomDescriptions, model.CustomDescriptions, e => e.Template.Id, m => m.Template.Id,
                add: m => AddEntity(m, x =>
                {
                    //определять ссылку на шаблон нужно только при добавлении, так как сопоставляем по шаблонам
                    m.Template.Id = GetDBId(m.Template.Id);
                    x.Template = tree.CustomPersonDescriptionTemplates.FirstOrDefault(t => t.Id == m.Template.Id);
                    entity.CustomDescriptions.Add(x);
                },
                    x => x.Id, x => m.Id = x, m.Id),
                update: (e, m) => converter.ApplyModelData(e, m),
                delete: e => entity.CustomDescriptions.Remove(e));

            //WARNING не обновлять отношения до обновления всего списка людей!
        }

        public void UpdatePersonRelations(Person entity, PersonViewModel model, GenTree tree)
        {
            ApplyChanges(entity.Relations, model.Relations, e => e.Id, m => m.Id,
                add: m =>
                {
                    //производим замены Id связанных сущностей в модели
                    m.TargetPersonId = GetDBId(m.TargetPersonId);
                    if (m is ChildRelationViewModel)
                        (m as ChildRelationViewModel).SecondParentId = GetDBId((m as ChildRelationViewModel).SecondParentId ?? 0);

                    AddEntity(converter.ToEntity(m, tree), x => entity.Relations.Add(x), x => x.Id, x => m.Id = x, m.Id);
                },
                update: (e, m) => converter.ApplyModelData(e, m),
                delete: e => entity.Relations.Remove(e));
        }

        public void UpdateDateTimeSetting(GenTreeDateTimeSetting entity, GenTreeDateTimeSetting model)
        {
            //обновление свойств
            converter.ApplyModelData(entity, model);

            //обновление списка эр
            if (entity.Eras == null && model.Eras != null)
                entity.Eras = new List<GenTreeEra>();
            ApplyChanges(entity.Eras, model.Eras, e => e.Id, m => m.Id,
                add: m => AddEntity(m, x => entity.Eras.Add(x), x => x.Id, x => m.Id = x, m.Id),
                update: (e, m) => converter.ApplyModelData(e, m),
                delete: e => db.Set<GenTreeEra>().Remove(e));
        }

        /// <summary>
        /// Для пар модель данных - мдель представления выполняет действие добавления для пары без модели данных и действие обновления для пары view-entity
        /// </summary>
        /// <typeparam name="E">Модель данных</typeparam>
        /// <typeparam name="M">Модель представления</typeparam>
        /// <typeparam name="TKey">Ключ для сравнения</typeparam>
        public void ApplyChanges<E, M, TKey>(IEnumerable<E> entities, IEnumerable<M> models,
            Func<E, TKey> entityKeySelector, Func<M, TKey> modelKeySelector,
            Action<M> add, Action<E, M> update, Action<E> delete)
        {
            if (entities == null && models != null)
            {
                foreach (var model in models)
                    add(model);
                return;
            }

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
                .Where(e => models.FirstOrDefault(m => modelKeySelector(m).Equals(entityKeySelector(e))) == null).ToList();
            foreach (var element in forDelete)
                delete(element);
        }

        public void AddEntity<E>(E entity, Action<E> add, Func<E, int> idSelector, Action<int> idReplacement = null, int? id = null)
        {
            //запоминаем старый Id
            var oldId = id ?? idSelector(entity);
            try
            {
                //добавляем в бд, сохраняем изменения и записываем новый id
                add(entity);
                db.SaveChanges();
                updateResult.Replacements[oldId] = idSelector(entity);
                if (idReplacement != null)
                    idReplacement(idSelector(entity));
            }
            catch
            {
                //в случае ошибки добавляем в список ошибок
                updateResult.Errors.Add(oldId);
            }
        }

        public int GetDBId(int id)
        {
            if (updateResult.Replacements.ContainsKey(id))
                return updateResult.Replacements[id];
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
