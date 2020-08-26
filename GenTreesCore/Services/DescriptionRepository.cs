using GenTreesCore.Entities;
using GenTreesCore.Models;
using System.Collections.Generic;
using System.Linq;

namespace GenTreesCore.Services
{
    public interface IDescriptionRepository
    {
        CustomPersonDescription Add(DescriptionViewModel model, Person person, GenTree tree, Dictionary<int, IIdentified> replacements);
        void Delete(CustomPersonDescription description, Person person);
        void Update(CustomPersonDescription description, DescriptionViewModel model, Person person, GenTree tree, Dictionary<int, IIdentified> replacements);
    }

    public class DescriptionRepository : IDescriptionRepository
    {
        ApplicationContext db;

        public DescriptionRepository(ApplicationContext context)
        {
            db = context;
        }

        public CustomPersonDescription Add(DescriptionViewModel model, Person person, GenTree tree, Dictionary<int, IIdentified> replacements)
        {
            var template = GetTemplate(model.TemplateId, tree, replacements);
            if (template == null)
            {
                return null; //TODO error "no template found for the description. Description was not added"
            }

            var description = new CustomPersonDescription
            {
                Template = template,
                Value = model.Value
            };
            replacements[model.Id] = description;

            person.CustomDescriptions.Add(description);
            return description;
        }

        public void Delete(CustomPersonDescription description, Person person)
        {
            person.CustomDescriptions?.Remove(description);
            db.Set<CustomPersonDescription>().Remove(description);
        }

        public void Update(CustomPersonDescription description, DescriptionViewModel model, Person person, GenTree tree, Dictionary<int, IIdentified> replacements)
        {
            var template = GetTemplate(model.TemplateId, tree, replacements);
            if (template == null)
            {
                Delete(description, person); //TODO error "no template found for the description. Description was removed"
                return;
            }
            description.Value = model.Value;
        }

        private CustomPersonDescriptionTemplate GetTemplate(int id, GenTree tree, Dictionary<int, IIdentified> replacements)
        {
            if (replacements.ContainsKey(id) && replacements[id] is CustomPersonDescriptionTemplate)
            {
                return replacements[id] as CustomPersonDescriptionTemplate;
            }
            else
            {
                return tree.CustomPersonDescriptionTemplates?.FirstOrDefault(t => t.Id == id);
            }
        }
    }
}
