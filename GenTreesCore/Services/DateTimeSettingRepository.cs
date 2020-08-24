using GenTreesCore.Entities;
using GenTreesCore.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace GenTreesCore.Services
{
    public interface IDateTimeSettingRepository
    {
        public GenTreeDateTimeSetting Add(GenTreeDateTimeSetting setting, int ownerId, Changes changes = null);
        public GenTreeDateTimeSetting Update(GenTreeDateTimeSetting setting, int userId, Changes changes = null);
        public GenTreeDateTimeSetting UpdateOrAdd(GenTreeDateTimeSetting setting, int userId, Changes changes = null);
        public GenTreeDateTimeSetting GetSetting(int id, int userId);
        public GenTreeDateTimeSetting GetDefault();
    }

    public class DateTimeSettingRepository : Repository, IDateTimeSettingRepository
    {
        private ApplicationContext db;
        private ModelEntityConverter converter;

        public DateTimeSettingRepository(ApplicationContext context)
        {
            db = context;
            converter = new ModelEntityConverter();
        }

        /// <summary>
        /// Добавление нового сеттинга летоисчисления
        /// </summary>
        /// <param name="model">модель сеттинга с данными</param>
        /// <param name="ownerId">id пользователя-владельца сеттинга</param>
        /// <returns></returns>
        public GenTreeDateTimeSetting Add(GenTreeDateTimeSetting model, int ownerId, Changes changes = null)
        {
            /*поиск пользователя по id*/
            var owner = db.Users.FirstOrDefault(User => User.Id == ownerId);
            if (owner == null)
            {
                changes?.Errors.Add(new IdError(ownerId, "user not found"));
                return null;
            }
            //TODO проверка эр
            var setting = new GenTreeDateTimeSetting();
            converter.ApplyModelData(setting, model) ;
            db.GenTreeDateTimeSettings.Add(setting);
            setting.Owner = owner;
            setting.Eras = new List<GenTreeEra>();
            /*Добавление эр*/
            var eras = model.Eras.Select(era => new ModelEntityPair<GenTreeEra,GenTreeEra>(new GenTreeEra(), era)).ToList();
            foreach (var era in eras)
            {
                converter.ApplyModelData(era.Entity, era.Model);
                setting.Eras.Add(era.Entity);
            }
            db.SaveChanges();
            foreach (var era in eras)
            {
                changes?.Replacements.Add(era.Model.Id, era.Entity.Id);
            }
            changes?.Replacements.Add(model.Id,setting.Id);
            return setting;
        }

        /// <summary>
        /// Обновление сеттинга летосчисления, включая даты
        /// </summary>
        /// <param name="model">Модель сеттинга</param>
        /// <param name="userId">Id пользователя, отправившего запрос на обновление</param>
        /// <returns></returns>
        public GenTreeDateTimeSetting Update(GenTreeDateTimeSetting model, int userId, Changes changes = null)
        {
            /*находим нужный сеттинг для обновления*/
            var setting = GetSetting(model.Id, userId);
            if (setting == null)
            {
                changes?.Errors.Add(new IdError(model.Id, "unable to access date-time setting"));
            }
            return setting;
        }

        public GenTreeDateTimeSetting UpdateOrAdd(GenTreeDateTimeSetting model, int userId, Changes changes = null)
        {
            var setting = GetSetting(model.Id, userId);
            if (setting == null)
                return Add(model, userId, changes);
            else
                return Update(setting, model, changes);
        }

        /// <summary>
        /// Получение сеттинга летосчисления
        /// </summary>
        /// <param name="id">id сеттинга</param>
        /// <param name="userId">id пользователя, отправившего запрос на получение дерева</param>
        /// <returns></returns>
        public GenTreeDateTimeSetting GetSetting(int id, int userId)
        {
            return db.GenTreeDateTimeSettings
                 .Include(setting => setting.Eras)
                 .FirstOrDefault(setting => setting.Id == id && userId == setting.Owner.Id);
        }

        public GenTreeDateTimeSetting GetDefault()
        {
            return db.GenTreeDateTimeSettings
                 .Include(setting => setting.Eras)
                 .FirstOrDefault(setting => !setting.IsPrivate);
        }

        private GenTreeDateTimeSetting Update(GenTreeDateTimeSetting setting, GenTreeDateTimeSetting model, Changes changes = null)
        {
            converter.ApplyModelData(setting, model);
            /*Обновление эр*/
            if (setting.Eras == null) setting.Eras = new List<GenTreeEra>();
            var eras = FullJoin(setting.Eras, model.Eras, (e, m) => e.Id == m.Id).ToList();
            var replacements = new Dictionary<int, GenTreeEra>();

            foreach (var era in eras)
            {
                if (era.Entity == null) /*Add*/
                {
                    var entity = new GenTreeEra();
                    converter.ApplyModelData(entity, era.Model);
                    setting.Eras.Add(entity);
                    replacements[era.Model.Id] = entity;
                }
                else if (era.Model == null) /*Delete*/
                {
                    //TODO удаление или изменение дат, завязанных на эру
                    setting.Eras.Remove(era.Entity);
                    db.Set<GenTreeEra>().Remove(era.Entity);
                }
                else /*Update*/
                {
                    converter.ApplyModelData(era.Entity, era.Model);
                }
            }
            db.SaveChanges();
            foreach (var replacement in replacements)
            {
                changes?.Replacements.Add(replacement.Key, replacement.Value.Id);
            }
            return setting;
        }
    }
}
