using GenTreesCore.Entities;
using GenTreesCore.Models;
using System.Linq;

namespace GenTreesCore.Services
{
    public interface IRelationRepository
    {
        Relation Add(RelationViewModel model, Person person, GenTree tree, Replacements replacements);
        void Delete(Relation relation, Person person);
        void Update(Relation relation, RelationViewModel model, Person person, GenTree tree, Replacements replacements);
    }

    public class RelationRepository : IRelationRepository
    {
        ApplicationContext db;

        public RelationRepository(ApplicationContext context)
        {
            db = context;
        }

        public Relation Add(RelationViewModel model, Person person, GenTree tree, Replacements replacements)
        {
            if (model.RelationType == null)
            {
                replacements.AddError(model.Id, "relation has an unknown type. Relation was not added", wasRemoved: true);
                return null;
            }

            var targetPerson = GetPerson(model.TargetPersonId, tree, replacements);
            if (targetPerson == null)
            {
                replacements.AddError(model.Id, "relation must have a target person. Relation was not added", wasRemoved: true);
                return null;
            }

            if (model.GetRelationType() == RelationViewModel.Type.Child)
            {
                if (model.RelationRate == null)
                {
                    replacements.AddError(model.Id, "relation has a wrong relation rate value. Relation was not added", wasRemoved: true);
                    return null;
                }
                var relation = new ChildRelation
                {
                    TargetPerson = targetPerson,
                    RelationRate = model.GetRelationRate()
                };
                if (model.SecondParentId != null)
                    relation.SecondParent = GetPerson((int)model.SecondParentId, tree, replacements);
                replacements.Add(model.Id, relation);
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
                replacements.Add(model.Id, relation);
                person.Relations.Add(relation);
                return relation;
            }
            else
            {
                replacements.AddError(model.Id, "Relation was not added", wasRemoved: true);
                return null;
            }
        }

        public void Delete(Relation relation, Person person)
        {
            person.Relations.Remove(relation);
            db.Set<Relation>().Remove(relation);
        }

        public void Update(Relation relation, RelationViewModel model, Person person, GenTree tree, Replacements replacements)
        {
            if (!tree.Persons.Contains(relation.TargetPerson))
            {
                replacements.AddError(model.Id, "Target person does not exist. Relation was removed", wasRemoved: true);
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
                replacements.AddError(model.Id, "unknown relation type. Relation was not changed", wasRemoved: false);
            }
        }

        private Person GetPerson(int id, GenTree tree, Replacements replacements)
        {
            if (replacements.Contains<Person>(id))
            {
                return replacements.Get<Person>(id);
            }
            else
            {
                return tree.Persons.FirstOrDefault(person => person.Id == id);
            }
        }
    }
}
