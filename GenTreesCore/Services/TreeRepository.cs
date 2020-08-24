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
        void AddGenTree(int userId, string name, bool isPrivate);
        GenTree GetGenTree(int treeId);
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
        IDateTimeSettingRepository dateTimeSettingRepository;

        public TreeRepository(ApplicationContext context)
        {
            db = context;
            converter = new ModelEntityConverter();
            dateTimeSettingRepository = new DateTimeSettingRepository(context);
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
            tree.DateCreated = DateTime.Now;
            db.GenTrees.Add(tree);
            db.SaveChanges();
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

        public GenTree GetGenTree(int treeId)
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
                .Where(t => t.Id == treeId)
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
    }
}
