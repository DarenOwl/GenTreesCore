using GenTreesCore.Entities;
using GenTreesCore.Models;
using System.Collections.Generic;
using System.Linq;

namespace GenTreesCore.Services
{
    public interface IPersonRepository
    {
        Person Add(PersonViewModel model, GenTree tree, Dictionary<int, IIdentified> replacements);
        void Delete(Person person, GenTree tree);
        void Update(Person person, PersonViewModel model, GenTree tree, Dictionary<int, IIdentified> replacements);
        void UpdateRelations(Person person, List<RelationViewModel> models, GenTree tree, Dictionary<int, IIdentified> replacements);
    }

    public class PersonRepository : Repository, IPersonRepository
    {
        ApplicationContext db;
        IDateTimeRepository dateTimeRepository;
        IDescriptionRepository descriptionRepository;
        IRelationRepository relationRepository;

        public PersonRepository(ApplicationContext context)
        {
            db = context;
            dateTimeRepository = new DateTimeRepository(context);
            descriptionRepository = new DescriptionRepository(context);
            relationRepository = new RelationRepository(context);
        }

        /// <summary>
        /// Добавление человека в дерево, запись в словарь замены, добавление дат рождения и запись их в замены
        /// </summary>
        /// <param name="model">модель представления человека</param>
        /// <param name="tree"></param>
        /// <param name="replacements">замены</param>
        /// <returns></returns>
        public Person Add(PersonViewModel model, GenTree tree, Dictionary<int, IIdentified> replacements)
        {
            var person = new Person
            {
                LastName = model.LastName,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                BirthPlace = model.BirthPlace,
                Biography = model.Biography,
                Gender = model.Gender,
                Image = model.Image,
            };
            if (model.BirthDate != null)
            {
                person.BirthDate = dateTimeRepository.Add(model.BirthDate, tree.GenTreeDateTimeSetting, replacements);
            }
            if (model.DeathDate != null)
            {
                person.DeathDate = dateTimeRepository.Add(model.DeathDate, tree.GenTreeDateTimeSetting, replacements);
            }
            tree.Persons.Add(person);
            replacements[model.Id] = person;
            UpdateDecsriptions(person, model.CustomDescriptions, tree, replacements);
            return person;
        }

        /// <summary>
        /// удаление человека из дерева и из бд. Удаление даты рождения-смерти, описаний и отношений человека
        /// </summary>
        /// <param name="person"></param>
        /// <param name="tree"></param>
        public void Delete(Person person, GenTree tree)
        {
            DeleteDates(person);
            DeleteDescriptions(person);
            DeleteRelations(person);

            tree.Persons.Remove(person);
            db.Set<Person>().Remove(person);      
        }

        /// <summary>
        /// Обновление человека, обновление дат рождения и запись их в замены при добавлении
        /// </summary>
        /// <param name="model">модель представления человека</param>
        /// <param name="tree"></param>
        /// <param name="replacements">замены</param>
        /// <returns></returns>
        public void Update(Person person, PersonViewModel model, GenTree tree, Dictionary<int, IIdentified> replacements)
        {
            person.LastName = model.LastName;
            person.FirstName = model.FirstName;
            person.MiddleName = model.MiddleName;
            person.BirthPlace = model.BirthPlace;
            person.BirthPlace = model.Biography;
            person.Gender = model.Gender;
            person.Image = model.Image;

            person.BirthDate = UpdateDate(person.BirthDate, model.BirthDate, tree.GenTreeDateTimeSetting, replacements);
            person.DeathDate = UpdateDate(person.DeathDate, model.DeathDate, tree.GenTreeDateTimeSetting, replacements);

            UpdateDecsriptions(person, model.CustomDescriptions, tree, replacements);
        }

        private GenTreeDateTime UpdateDate(GenTreeDateTime date, GenTreeDateViewModel model,
            GenTreeDateTimeSetting setting, Dictionary<int, IIdentified> replacements)
        {
            if (date != null)
            {
                if (model == null)
                {
                    dateTimeRepository.Delete(date);
                    return null;
                }
                else
                {
                    return dateTimeRepository.Update(date, model, setting, replacements);
                }
            }
            else if (model!= null)
            {
                return dateTimeRepository.Add(model, setting, replacements);
            }
            else
            {
                return null;
            }
        }

        private void UpdateDecsriptions(Person person, List<DescriptionViewModel> models, GenTree tree, Dictionary<int, IIdentified> replacements)
        {
            if (person.CustomDescriptions == null)
                person.CustomDescriptions = new List<CustomPersonDescription>();

            UpdateRange(
                fulljoin: FullJoin(person.CustomDescriptions, models, (e, m) => e.Template.Id == m.TemplateId).ToList(),
                add: model => descriptionRepository.Add(model, person, tree, replacements),
                delete: description => descriptionRepository.Delete(description, person),
                update: (description, model) => descriptionRepository.Update(description, model, person, tree, replacements));
        }

        /// <summary>
        /// Обновление отношений. Обновлять отношения только после обновления всего списка людей, иначе не все отношения будут добавлены.
        /// </summary>
        /// <param name="person">человек, отношения которого нужно обновить</param>
        /// <param name="models">модели отношений</param>
        /// <param name="tree">дерево</param>
        /// <param name="replacements">словарь замен</param>
        public void UpdateRelations(Person person, List<RelationViewModel> models, GenTree tree, Dictionary<int, IIdentified> replacements)
        {
            if (person.Relations == null)
                person.Relations = new List<Relation>();

            UpdateRange(
                fulljoin: FullJoin(person.Relations, models, 
                    matcher:  (e, m) => 
                        e.TargetPerson != null &&
                        e.TargetPerson.Id == m.TargetPersonId && (
                        e is ChildRelation && m.GetRelationType() == RelationViewModel.Type.Child ||
                        e is SpouseRelation && m.GetRelationType() == RelationViewModel.Type.Spouse))
                    .ToList(),
                add: model => relationRepository.Add(model, person, tree, replacements),
                delete: relation => relationRepository.Delete(relation, person),
                update: (relation, model) => relationRepository.Update(relation, model, person, tree, replacements));
        }

        private void DeleteDates(Person person)
        {
            if (person.BirthDate != null)
            {
                dateTimeRepository.Delete(person.BirthDate);
                person.BirthDate = null;
            }
            if (person.DeathDate != null)
            {
                dateTimeRepository.Delete(person.DeathDate);
                person.DeathDate = null;
            }
        }

        private void DeleteDescriptions(Person person)
        {
            if (person.CustomDescriptions == null)
                return;
            var forDelete = new List<CustomPersonDescription>(person.CustomDescriptions);
            foreach (var description in forDelete)
            {
                descriptionRepository.Delete(description, person);
            }
        }

        private void DeleteRelations(Person person)
        {
            if (person.Relations == null)
                return;
            var forDelete = new List<Relation>(person.Relations);
            foreach (var relation in forDelete)
            {
                relationRepository.Delete(relation, person);
            }
        }
    }
}
