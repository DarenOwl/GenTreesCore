using GenTreesCore.Entities;
using GenTreesCore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GenTreesCore.Services
{
    public interface ITreeRepository
    {
        void Add(GenTreeViewModel model, int userId, Changes changes = null);
        void Update(GenTreeViewModel model, int userId, Changes changes = null);
        void AddGenTree(int userId, string name, bool isPrivate);
        GenTree GetTree(int id, int userId, bool forUpdate);
        List<GenTree> GetPublicTrees();
        List<GenTree> GetUserGenTrees(int userId);
        void SaveChanges();
        Changes Update(GenTree entity, GenTreeViewModel model);
        void Update<T>(T entity, int id, bool saveChanges = true);
    }

    public class TreeRepository : Repository, ITreeRepository
    {
        private ApplicationContext db;
        private ModelEntityConverter converter;
        private IDateTimeSettingRepository dateTimeSettingRepository;
        private IDescriptionTemplateRepository templateRepository;
        private IPersonRepository personRepository;

        public TreeRepository(ApplicationContext context)
        {
            db = context;
            converter = new ModelEntityConverter();
            dateTimeSettingRepository = new DateTimeSettingRepository(context);
            templateRepository = new DescriptionTemplateRepository(context);
            personRepository = new PersonRepository(context);
        }

        public void Add(GenTreeViewModel model, int userId, Changes changes = null)
        {
            var owner = db.Users.FirstOrDefault(u => u.Id == userId);

            if (owner == null)
            {
                changes?.Errors.Add(new IdError(userId, "tree must have an owner"));
                return;
            }

            var tree = new GenTree();
            tree.DateCreated = DateTime.Now;
            //TODO удалить ковертер и раскидать его методы по репозиториям - все равно не пересекаются
            converter.ApplyModelData(tree, model);
            tree.Owner = owner;
            if (tree.Name == null) tree.Name = "New tree";

            /*добавление-обновление сеттинга*/
            if (model.DateTimeSetting == null)
            {
                tree.GenTreeDateTimeSetting = dateTimeSettingRepository.GetDefault();
            }
            else
            {
                tree.GenTreeDateTimeSetting = dateTimeSettingRepository.UpdateOrAdd(model.DateTimeSetting, userId, changes);
            }
            
            db.GenTrees.Add(tree);
            db.SaveChanges();
            return;
        }

        public void Update(GenTreeViewModel model, int userId, Changes changes = null)
        {
                        var tree = GetTree(model.Id, userId, forUpdate: true);

            if (tree == null)
            {
                changes?.Errors.Add(new IdError(model.Id, $"tree with id {model.Id} not found"));
                return;
            }

            converter.ApplyModelData(tree, model);
            if (tree.Name == null) tree.Name = "family tree";

            var replacements = new Dictionary<int, IIdentified>();
            /*добавление-обновление сеттинга*/
            if (model.DateTimeSetting == null)
            {
                tree.GenTreeDateTimeSetting = dateTimeSettingRepository.GetDefault();
            }
            else
            {
                tree.GenTreeDateTimeSetting = dateTimeSettingRepository.UpdateOrAdd(model.DateTimeSetting, userId, changes);
            }

            UpdateTemplates(model.DescriptionTemplates, tree, replacements);
            UpdatePersons(model.Persons, tree, replacements);

            db.SaveChanges();
            foreach (var replacement in replacements)
            {
                changes.Replacements[replacement.Key] = replacement.Value.Id;
            }
            return;
        }

        public List<GenTree> GetUserGenTrees(int userId)
        {
            return db.GenTrees
                .Where(tree => tree.Owner.Id == userId)
                .ToList();
        }

        public List<GenTree> GetPublicTrees()
        {
            return db.GenTrees
                .Include(t => t.Owner)
                .Where(tree => !tree.IsPrivate)
                .ToList();
        }

        public GenTree GetTree(int id, int userId, bool forUpdate)
        {
            return db.GenTrees
                .Include(t => t.Owner)
                .Include(t => t.Persons)
                    .ThenInclude(p => p.CustomDescriptions)
                .Include(t => t.CustomPersonDescriptionTemplates)
                .Include(t => t.Persons)
                    .ThenInclude(p => p.BirthDate)
                .Include(t => t.Persons)
                    .ThenInclude(p => p.DeathDate)
                .Include(t => t.Persons)
                    .ThenInclude(p => p.Relations)
                .Include(t => t.GenTreeDateTimeSetting)
                    .ThenInclude(s => s.Eras)
                .Where(tree => tree.Id == id && (tree.Owner.Id == userId || (!tree.IsPrivate && !forUpdate)))
                .FirstOrDefault();
        }

        public void AddGenTree(int userId, string name, bool isPrivate)
        {
            var owner = db.Users.FirstOrDefault(u => u.Id == userId);
            db.GenTrees.Add(new GenTree
            {
                Name = name,
                IsPrivate = isPrivate,
                DateCreated = DateTime.Now,
                LastUpdated = DateTime.Now,
                Owner = owner
            });
            db.SaveChanges();
        }

        public Changes Update(GenTree entity, GenTreeViewModel model)
        {
            var updateService = new TreeUpdateService(db);
            updateService.UpdateTree(entity, model);
            return updateService.UpdateResult;
        }

        public void Update<T>(T entity, int id, bool saveChanges = true)
        {
            var record = (T)db.Find(typeof(T), id);
            if (record != null)
            {
                var props = record.GetType().GetProperties();
                foreach (var prop in props)
                {
                    if (prop.PropertyType.Namespace != nameof(GenTreesCore))
                        prop.SetValue(record, prop.GetValue(entity));
                }
            }
            else return;
            if (saveChanges)
                db.SaveChanges();
        }

        public void SaveChanges()
        {
            db.SaveChanges();
        }

        private void UpdateTemplates(List<CustomPersonDescriptionTemplate> models, GenTree tree, Dictionary<int, IIdentified> replacements)
        {

            if (tree.CustomPersonDescriptionTemplates == null)
                tree.CustomPersonDescriptionTemplates = new List<CustomPersonDescriptionTemplate>();

            UpdateRange(
                fulljoin: FullJoin(tree.CustomPersonDescriptionTemplates, models, (e, m) => e.Id == m.Id),
                add: model => replacements[model.Id] = templateRepository.Add(model, tree),
                delete: template => templateRepository.Delete(template, tree),
                update: (template, model) => templateRepository.Update(template, model));
        }

        private void UpdatePersons(List<PersonViewModel> models, GenTree tree, Dictionary<int, IIdentified> replacements)
        {
            if (tree.Persons == null)
                tree.Persons = new List<Person>();

            UpdateRange(
                fulljoin: FullJoin(tree.Persons, models, (e, m) => e.Id == m.Id),
                add: model => replacements[model.Id] = personRepository.Add(model, tree),
                delete: person => personRepository.Delete(person, tree),
                update: (person, model) => personRepository.Update(person, model));
        }
    }
}
