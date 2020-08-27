using GenTreesCore.Entities;
using GenTreesCore.Models;
using System.Linq;

namespace GenTreesCore.Services
{
    public interface IDescriptionRepository
    {
        CustomPersonDescription Add(DescriptionViewModel model, Person person, GenTree tree, Replacements replacements);
        void Delete(CustomPersonDescription description, Person person);
        void Update(CustomPersonDescription description, DescriptionViewModel model, Person person, GenTree tree, Replacements replacements);
    }

    public class DescriptionRepository : IDescriptionRepository
    {
        ApplicationContext db;

        public DescriptionRepository(ApplicationContext context)
        {
            db = context;
        }

        public CustomPersonDescription Add(DescriptionViewModel model, Person person, GenTree tree, Replacements replacements)
        {
            var template = GetTemplate(model.TemplateId, tree, replacements);
            if (template == null)
            {
                replacements.AddError(model.Id, "no template found for the description. Description was not added", wasRemoved: true);
                return null;
            }

            var description = new CustomPersonDescription
            {
                Template = template,
                Value = model.Value
            };

            replacements.Add(model.Id, description);
            person.CustomDescriptions.Add(description);
            return description;
        }

        public void Delete(CustomPersonDescription description, Person person)
        {
            person.CustomDescriptions?.Remove(description);
            db.Set<CustomPersonDescription>().Remove(description);
        }

        public void Update(CustomPersonDescription description, DescriptionViewModel model, Person person, GenTree tree, Replacements replacements)
        {
            var template = GetTemplate(model.TemplateId, tree, replacements);
            if (template == null)
            {
                replacements.AddError(model.Id, "no template found for the description. Description was removed", wasRemoved: true);
                Delete(description, person);
                return;
            }
            description.Value = model.Value;
        }

        private CustomPersonDescriptionTemplate GetTemplate(int id, GenTree tree, Replacements replacements)
        {
            if (replacements.Contains<CustomPersonDescriptionTemplate>(id))
            {
                return replacements.Get<CustomPersonDescriptionTemplate>(id);
            }
            else
            {
                return tree.CustomPersonDescriptionTemplates?.FirstOrDefault(t => t.Id == id);
            }
        }
    }
}
