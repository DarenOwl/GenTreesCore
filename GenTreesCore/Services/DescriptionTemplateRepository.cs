using GenTreesCore.Entities;
using GenTreesCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenTreesCore.Services
{
    public interface IDescriptionTemplateRepository
    {
        void Update(List<CustomPersonDescriptionTemplate> models, GenTree tree, Changes changes = null);
    }

    public class DescriptionTemplateRepository : Repository, IDescriptionTemplateRepository
    {
        private ApplicationContext db;

        public DescriptionTemplateRepository(ApplicationContext context)
        {
            db = context;
        }

        public void Update(List<CustomPersonDescriptionTemplate> models, GenTree tree, Changes changes = null)
        {
            if (tree == null) return;

            if (tree.CustomPersonDescriptionTemplates == null)
                tree.CustomPersonDescriptionTemplates = new List<CustomPersonDescriptionTemplate>();
            var templates = FullJoin(tree.CustomPersonDescriptionTemplates, models, (e, m) => e.Id == m.Id);

            var replacements = new Dictionary<int, CustomPersonDescriptionTemplate>();

            UpdateRange(templates,
                add: model => replacements[model.Id] = Add(model, tree),
                delete: template => Delete(template, tree),
                update: (template, model) => Update(template, model));


            db.SaveChanges();
            foreach (var replacement in replacements)
            {
                changes?.Replacements.Add(replacement.Key, replacement.Value.Id);
            }
        }

        public CustomPersonDescriptionTemplate Add(CustomPersonDescriptionTemplate model, GenTree tree)
        {
            var entity = new CustomPersonDescriptionTemplate
            {
                Name = model.Name,
                /*TODO добавить изменение поля Type*/
            };
            tree.CustomPersonDescriptionTemplates.Add(entity);
            return entity;
        }

        public void Delete(CustomPersonDescriptionTemplate template, GenTree tree)
        {
            tree.CustomPersonDescriptionTemplates.Remove(template);
            db.Set<CustomPersonDescriptionTemplate>().Remove(template);
        }

        public void Update(CustomPersonDescriptionTemplate template, CustomPersonDescriptionTemplate model)
        {
            template.Name = model.Name;
            /*TODO добавить изменение поля Type*/
        }
    }
}
