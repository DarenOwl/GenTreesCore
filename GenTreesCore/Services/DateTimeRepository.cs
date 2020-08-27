using GenTreesCore.Entities;
using GenTreesCore.Models;
using System.Linq;

namespace GenTreesCore.Services
{
    public interface IDateTimeRepository
    {
        GenTreeDateTime Add(GenTreeDateViewModel model, GenTreeDateTimeSetting setting, Replacements replacements);
        void Delete(GenTreeDateTime date);
        GenTreeDateTime Update(GenTreeDateTime date, GenTreeDateViewModel model, GenTreeDateTimeSetting setting, Replacements replacements);
    }

    public class DateTimeRepository : IDateTimeRepository
    {
        ApplicationContext db;

        public DateTimeRepository(ApplicationContext context)
        {
            db = context;
        }

        public GenTreeDateTime Add(GenTreeDateViewModel model, GenTreeDateTimeSetting setting, Replacements replacements)
        {
            var era = GetEra(model.EraId, setting, replacements);
            if (era == null)
            {
                AddEraError(model.Id, replacements);
                return null;
            }

            var date = new GenTreeDateTime
            {
                Era = era,
                Year = model.Year,
                Month = model.Month,
                Day = model.Day,
                Hour = model.Hour,
                Minute = model.Minute,
                Second = model.Second
            };
            replacements.Add(model.Id, date);
            return date; 
        }

        public void Delete(GenTreeDateTime date)
        {
            db.Set<GenTreeDateTime>().Remove(date);
        }

        public GenTreeDateTime Update(GenTreeDateTime date, GenTreeDateViewModel model, GenTreeDateTimeSetting setting, Replacements replacements)
        {
            if (setting.Eras == null)
            {
                AddEraError(model.Id, replacements);
                Delete(date);
                return null;
            };

            var era = GetEra(model.EraId, setting, replacements);
            if (era == null)
            {
                era = setting.Eras.FirstOrDefault(); //TODO better date convertation
            }
            if (era == null)
            {
                AddEraError(model.Id, replacements);
                Delete(date);
                return null;
            }

            date.Era = era;
            date.Year = model.Year;
            date.Month = model.Month;
            date.Day = model.Day;
            date.Month = model.Month;
            date.Second = model.Second;

            return date;
        }

        private GenTreeEra GetEra(int id, GenTreeDateTimeSetting setting, Replacements replacements)
        {
            if (replacements.Contains<GenTreeEra>(id))
            {
                return replacements.Get<GenTreeEra>(id);
            }
            else
            {
                return setting.Eras.FirstOrDefault(e => e.Id == id);
            }
        }

        private void AddEraError(int dateId, Replacements replacements)
        {
            replacements.AddError(dateId, $"invalid date. No eras were found to replace the wrong id.", wasRemoved: true);
        }
    }
}
