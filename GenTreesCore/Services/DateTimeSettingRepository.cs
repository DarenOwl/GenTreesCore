using GenTreesCore.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace GenTreesCore.Services
{
    public interface IDateTimeSettingRepository
    {
        GenTreeDateTimeSetting Add(GenTreeDateTimeSetting model, int userId, Replacements replacements);
        void Update(GenTreeDateTimeSetting setting, GenTreeDateTimeSetting model, Replacements replacements);
        GenTreeDateTimeSetting UpdateOrAdd(GenTreeDateTimeSetting model, int userId, Replacements replacements);
        GenTreeDateTimeSetting GetDefault();
        GenTreeDateTimeSetting GetSetting(int id, int userId);
    }

    public class DateTimeSettingRepository : Repository, IDateTimeSettingRepository
    {
        private ApplicationContext db;
        private IEraRepository eraRepository;

        public DateTimeSettingRepository(ApplicationContext context)
        {
            db = context;
            eraRepository = new EraRepository(context);
        }

        public GenTreeDateTimeSetting UpdateOrAdd(GenTreeDateTimeSetting model, int userId, Replacements replacements)
        {
            var setting = GetSetting(model.Id, userId);
            if (setting == null)
                return Add(model, userId, replacements);

            Update(setting, model, replacements);
            return setting;
        }

        public GenTreeDateTimeSetting Add(GenTreeDateTimeSetting model, int userId, Replacements replacements)
        {
            /*поиск пользователя по id*/
            var owner = db.Users.FirstOrDefault(User => User.Id == userId);
            if (owner == null)
            {
                replacements.AddError(model.Id, $"you must be registered to add a setting", wasRemoved: true);
                return null;
            }

            var setting = new GenTreeDateTimeSetting()
            {
                IsPrivate = model.IsPrivate,
                Owner = owner,
                YearMonthCount = model.YearMonthCount,
                Eras = new List<GenTreeEra>()
            };

            if (model.Name == null)
            {
                replacements.AddError(model.Id, "Setting must have a Name. Name was set to default", wasRemoved: false);
            }
            else
            {
                setting.Name = model.Name;
            }

            replacements.Add(model.Id, setting);
            AddEras(model.Eras, setting, replacements);
            db.GenTreeDateTimeSettings.Add(setting);
            return setting;
        }

        public void Update(GenTreeDateTimeSetting setting, GenTreeDateTimeSetting model, Replacements replacements)
        {
            if (model.Name == null)
            {
                replacements.AddError(model.Id, "Setting must have a Name. Old Name was not changed", wasRemoved: false);
            }
            else
            {
                setting.Name = model.Name;
            }
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

        private void AddEras(List<GenTreeEra> models, GenTreeDateTimeSetting setting, Replacements replacements)
        {
            if (models != null)
                foreach (var eraModel in models)
                {
                    eraRepository.Add(eraModel, setting, replacements);
                }
        }

        private void UpdateEras(List<GenTreeEra> models, GenTreeDateTimeSetting setting, Replacements replacements)
        {
            if (setting.Eras == null)
                setting.Eras = new List<GenTreeEra>();

            UpdateRange(
                fulljoin: FullJoin(setting.Eras, models, (e, m) => e.Id == m.Id).ToList(),
                add: model => eraRepository.Add(model, setting, replacements),
                delete: era => eraRepository.Delete(era, setting),
                update: (era, model) => eraRepository.Update(era, model, replacements));
        }
    }
}
