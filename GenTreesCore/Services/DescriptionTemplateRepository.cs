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
        CustomPersonDescriptionTemplate Add(CustomPersonDescriptionTemplate model, GenTree tree, Replacements replacements);
        void Delete(CustomPersonDescriptionTemplate template, GenTree tree);
        void Update(CustomPersonDescriptionTemplate template, CustomPersonDescriptionTemplate model);
    }

    public class DescriptionTemplateRepository : Repository, IDescriptionTemplateRepository
    {
        private ApplicationContext db;

        public DescriptionTemplateRepository(ApplicationContext context)
        {
            db = context;
        }

        public CustomPersonDescriptionTemplate Add(CustomPersonDescriptionTemplate model, GenTree tree, Replacements replacements)
        {
            var template = new CustomPersonDescriptionTemplate
            {
                Name = model.Name,
                /*TODO добавить изменение поля Type*/
            };
            tree.CustomPersonDescriptionTemplates.Add(template);
            replacements.Add(model.Id, template);
            return template;
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
