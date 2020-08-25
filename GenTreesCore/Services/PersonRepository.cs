using GenTreesCore.Entities;
using GenTreesCore.Models;
using System;

namespace GenTreesCore.Services
{
    public interface IPersonRepository
    {
        Person Add(PersonViewModel model, GenTree tree);
        void Delete(Person person, GenTree tree);
        void Update(Person person, PersonViewModel model);
    }

    public class PersonRepository : Repository, IPersonRepository
    {
        ApplicationContext db;

        public PersonRepository(ApplicationContext context)
        {
            db = context;
        }

        public Person Add(PersonViewModel model, GenTree tree)
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
            tree.Persons.Add(person);
            return person;
        }

        public void Delete(Person person, GenTree tree)
        {
            tree.Persons.Remove(person);
            db.Set<Person>().Remove(person);
        }

        public void Update(Person person, PersonViewModel model)
        {
            person.LastName = model.LastName;
            person.FirstName = model.FirstName;
            person.MiddleName = model.MiddleName;
            person.BirthPlace = model.BirthPlace;
            person.BirthPlace = model.Biography;
            person.Gender = model.Gender;
            person.Image = model.Image;
        }
    }
}
