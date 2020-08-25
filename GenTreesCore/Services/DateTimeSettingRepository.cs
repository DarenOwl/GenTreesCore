using GenTreesCore.Entities;
using GenTreesCore.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace GenTreesCore.Services
{
    public interface IDateTimeSettingRepository
    {
        GenTreeDateTimeSetting Add(GenTreeDateTimeSetting model, int userId, Dictionary<int, IIdentified> replacements);
        void Update(GenTreeDateTimeSetting setting, GenTreeDateTimeSetting model, Dictionary<int, IIdentified> replacements);
        GenTreeDateTimeSetting UpdateOrAdd(GenTreeDateTimeSetting model, int userId, Dictionary<int, IIdentified> replacements);
        GenTreeDateTimeSetting GetDefault();
        GenTreeDateTimeSetting GetSetting(int id, int userId);
    }

    public class DateTimeSettingRepository : Repository, IDateTimeSettingRepository
    {
        private ApplicationContext db;
        private IEraRepository eraRepository;
        private ModelEntityConverter converter;

        public DateTimeSettingRepository(ApplicationContext context)
        {
            db = context;
            converter = new ModelEntityConverter();
            eraRepository = new EraRepository(context);
        }

        public GenTreeDateTimeSetting UpdateOrAdd(GenTreeDateTimeSetting model, int userId, Dictionary<int, IIdentified> replacements)
        {
            var setting = GetSetting(model.Id, userId);
            if (setting == null)
                return Add(model, userId, replacements);

            Update(setting, model, replacements);
            return setting;
        }

        public GenTreeDateTimeSetting Add(GenTreeDateTimeSetting model, int userId, Dictionary<int, IIdentified> replacements)
        {
            /*поиск пользователя по id*/
            var owner = db.Users.FirstOrDefault(User => User.Id == userId);
            if (owner == null)
            {
                //TODO ошибка
                return null;
            }

            var setting = new GenTreeDateTimeSetting()
            {
                Name = model.Name,
                IsPrivate = model.IsPrivate,
                Owner = owner,
                YearMonthCount = model.YearMonthCount,
                Eras = new List<GenTreeEra>()
            };
            replacements[model.Id] = setting;

            if (model.Eras != null)
                foreach (var eraModel in model.Eras)
                {
                    replacements[eraModel.Id] = eraRepository.Add(eraModel, setting);
                }

            db.GenTreeDateTimeSettings.Add(setting);
            return setting;
        }

        public void Update(GenTreeDateTimeSetting setting, GenTreeDateTimeSetting model, Dictionary<int, IIdentified> replacements)
        {
            setting.Name = model.Name; //TODO проверка на null
            setting.IsPrivate = model.IsPrivate;
            setting.YearMonthCount = model.YearMonthCount;

            UpdateEras(model.Eras, setting, replacements);
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

        private void UpdateEras(List<GenTreeEra> models, GenTreeDateTimeSetting setting, Dictionary<int, IIdentified> replacements)
        {
            if (setting.Eras == null)
                setting.Eras = new List<GenTreeEra>();

            UpdateRange(
                fulljoin: FullJoin(setting.Eras, models, (e, m) => e.Id == m.Id).ToList(),
                add: model => replacements[model.Id] = eraRepository.Add(model, setting),
                delete: era => eraRepository.Delete(era, setting),
                update: (era, model) => eraRepository.Update(era, model));
        }
    }
}
