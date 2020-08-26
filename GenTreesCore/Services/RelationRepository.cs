using GenTreesCore.Entities;
using GenTreesCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenTreesCore.Services
{
    public interface IRelationRepository
    {
        Relation Add(RelationViewModel model, Person person, GenTree tree, Dictionary<int, IIdentified> replacements);
        void Delete(Relation relation, Person person);
        void Update(Relation relation, RelationViewModel model, Person person, GenTree tree, Dictionary<int, IIdentified> replacements);
    }

    public class RelationRepository : IRelationRepository
    {
        ApplicationContext db;

        public RelationRepository(ApplicationContext context)
        {
            db = context;
        }

        public Relation Add(RelationViewModel model, Person person, GenTree tree, Dictionary<int, IIdentified> replacements)
        {
            if (model.RelationType == null)
            {
                //TODO error "relation has an unknown type. Relation was not added"
                return null;
            }

            var targetPerson = GetPerson(model.TargetPersonId, tree, replacements);
            if (targetPerson == null)
            {
                //TODO error "relation must have a target person. Relation was not added"
                return null;
            }

            if (model.GetRelationType() == RelationViewModel.Type.Child)
            {
                if (model.RelationRate == null)
                {
                    //TODO error "relation has a wrong relation rate value. Relation was not added"
                    return null;
                }
                var relation = new ChildRelation
                {
                    TargetPerson = targetPerson,
                    RelationRate = model.GetRelationRate()
                };
                if (model.SecondParentId != null)
                    relation.SecondParent = GetPerson((int)model.SecondParentId, tree, replacements);
                replacements[model.Id] = relation;
                person.Relations.Add(relation);
                return relation;
            }
            else if (model.GetRelationType() == RelationViewModel.Type.Spouse)
            {
                var relation = new SpouseRelation
                {
                    TargetPerson = targetPerson,
                    IsFinished = model.IsFinished
                };
                replacements[model.Id] = relation;
                person.Relations.Add(relation);
                return relation;
            }
            else
            {
                return null;
            }
        }

        public void Delete(Relation relation, Person person)
        {
            person.Relations.Remove(relation);
            db.Set<Relation>().Remove(relation);
        }

        public void Update(Relation relation, RelationViewModel model, Person person, GenTree tree, Dictionary<int, IIdentified> replacements)
        {
            if (!tree.Persons.Contains(relation.TargetPerson))
            {
                //TODO error "target person does not exist. Relation was removed"
                Delete(relation, person);
            }
            if (model.GetRelationType() == RelationViewModel.Type.Child && relation is ChildRelation)
            {
                (relation as ChildRelation).RelationRate = model.GetRelationRate();
                if (model.SecondParentId != null)
                    (relation as ChildRelation).SecondParent = GetPerson((int)model.SecondParentId, tree, replacements);
            }
            else if (model.GetRelationType() == RelationViewModel.Type.Spouse && relation is SpouseRelation)
            {
                (relation as SpouseRelation).IsFinished = model.IsFinished;
            }
            else
            {
                //TODO error "unknown relation type. Relation was not changed"
            }
        }

        private Person GetPerson(int id, GenTree tree, Dictionary<int, IIdentified> replacements)
        {
            if (replacements.ContainsKey(id) && replacements[id] is Person)
            {
                return replacements[id] as Person;
            }
            else
            {
                return tree.Persons.FirstOrDefault(person => person.Id == id);
            }
        }
    }
}
