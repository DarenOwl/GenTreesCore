using GenTreesCore.Entities;

namespace GenTreesCore.Services
{
    public interface IEraRepository
    {
        GenTreeEra Add(GenTreeEra model, GenTreeDateTimeSetting setting, Replacements replacements);
        void Delete(GenTreeEra era, GenTreeDateTimeSetting setting);
        void Update(GenTreeEra era, GenTreeEra model, Replacements replacements);
    }

    public class EraRepository : IEraRepository
    {
        private ApplicationContext db;

        public EraRepository(ApplicationContext context)
        {
            db = context;
        }

        public GenTreeEra Add(GenTreeEra model, GenTreeDateTimeSetting setting, Replacements replacements)
        {
            var era = new GenTreeEra
            {
                Description = model.Description,
                ThroughBeginYear = model.ThroughBeginYear,
                YearCount = model.YearCount
            };

            if (model.Name == null || model.ShortName == null)
            {
                replacements.AddError(model.Id, $"Era must have a Name and a Short Name. Era was not added", wasRemoved: true);
            }
            else
            {
                era.Name = model.Name;
                era.ShortName = model.ShortName;
            }

            replacements.Add(model.Id, era);
            setting.Eras.Add(era);
            return era;
        }

        public void Delete(GenTreeEra era, GenTreeDateTimeSetting setting)
        {
            setting.Eras.Remove(era);
            db.Set<GenTreeEra>().Remove(era);
        }

        public void Update(GenTreeEra era, GenTreeEra model, Replacements replacements)
        {
            if (model.Name == null || model.ShortName == null)
            {
                replacements.AddError(model.Id, $"Era must have a Name and a Short Name. Name and ShortName were not updated", wasRemoved: false);
            }
            else
            {
                era.Name = model.Name;
                era.ShortName = model.ShortName;
            }

            era.Name = model.Name;
            era.ShortName = model.ShortName;
            era.Description = model.Description;
            era.ThroughBeginYear = model.ThroughBeginYear;
            era.YearCount = model.YearCount;
        }
    }
}
