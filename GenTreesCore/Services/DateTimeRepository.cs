using GenTreesCore.Entities;
using GenTreesCore.Models;
using System.Collections.Generic;
using System.Linq;

namespace GenTreesCore.Services
{
    public interface IDateTimeRepository
    {
        GenTreeDateTime Add(GenTreeDateViewModel model, GenTreeDateTimeSetting setting, Dictionary<int, IIdentified> replacements);
        void Delete(GenTreeDateTime date);
        GenTreeDateTime Update(GenTreeDateTime date, GenTreeDateViewModel model, GenTreeDateTimeSetting setting, Dictionary<int, IIdentified> replacements);
    }

    public class DateTimeRepository : IDateTimeRepository
    {
        ApplicationContext db;

        public DateTimeRepository(ApplicationContext context)
        {
            db = context;
        }

        public GenTreeDateTime Add(GenTreeDateViewModel model, GenTreeDateTimeSetting setting, Dictionary<int, IIdentified> replacements)
        {
            var era = GetEra(model.EraId, setting, replacements);
            if (era == null) return null;

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
            replacements[model.Id] = date;
            return date; 
        }

        public void Delete(GenTreeDateTime date)
        {
            db.Set<GenTreeDateTime>().Remove(date);
        }

        public GenTreeDateTime Update(GenTreeDateTime date, GenTreeDateViewModel model, GenTreeDateTimeSetting setting, Dictionary<int, IIdentified> replacements)
        {
            if (setting.Eras == null)
            {
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

        private GenTreeEra GetEra(int id, GenTreeDateTimeSetting setting, Dictionary<int, IIdentified> replacements)
        {
            var era = new GenTreeEra();
            if (replacements.ContainsKey(id) && replacements[id] is GenTreeEra)
                era = replacements[id] as GenTreeEra;
            else
                era = setting.Eras.FirstOrDefault(e => e.Id == id);
            return era;
        }
    }
}
