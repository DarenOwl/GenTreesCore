using GenTreesCore.Entities;

namespace GenTreesCore.Services
{
    public interface IEraRepository
    {
        GenTreeEra Add(GenTreeEra model, GenTreeDateTimeSetting setting);
        void Delete(GenTreeEra era, GenTreeDateTimeSetting setting);
        void Update(GenTreeEra era, GenTreeEra model);
    }

    public class EraRepository : IEraRepository
    {
        private ApplicationContext db;

        public EraRepository(ApplicationContext context)
        {
            db = context;
        }

        public GenTreeEra Add(GenTreeEra model, GenTreeDateTimeSetting setting)
        {
            var era = new GenTreeEra
            {
                Name = model.Name, //TODO проверка на null
                ShortName = model.ShortName, //TODO проверка на null
                Description = model.Description,
                ThroughBeginYear = model.ThroughBeginYear,
                YearCount = model.YearCount
            };
            setting.Eras.Add(era);
            return era;
        }

        public void Delete(GenTreeEra era, GenTreeDateTimeSetting setting)
        {
            setting.Eras.Remove(era);
            db.Set<GenTreeEra>().Remove(era);
        }

        public void Update(GenTreeEra era, GenTreeEra model)
        {
            era.Name = model.Name; //TODO проверка на null
            era.ShortName = model.ShortName; //TODO проверка на null
            era.Description = model.Description;
            era.ThroughBeginYear = model.ThroughBeginYear;
            era.YearCount = model.YearCount;
        }
    }
}
