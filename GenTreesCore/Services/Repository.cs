using GenTreesCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GenTreesCore.Services
{
    public class Repository
    {
        /// <summary>
        /// Возвращает full join список пар элементов двух коллекций
        /// </summary>
        /// <typeparam name="E">Entity type</typeparam>
        /// <typeparam name="M">Model type</typeparam>
        /// <param name="entities"></param>
        /// <param name="models"></param>
        /// <param name="matcher"></param>
        /// <returns></returns>
        public List<ModelEntityPair<E, M>> FullJoin<E, M>(IEnumerable<E> entities, IEnumerable<M> models, Func<E, M, bool> matcher)
            where E : class
            where M : class
        {
            var result = new List<ModelEntityPair<E,M>>();
            /*пары элементов (entity-model) и (entity-null)*/
            if (entities != null)
                foreach (var entity in entities)
                {
                    result.Add(new ModelEntityPair<E, M>(entity, models?.FirstOrDefault(model => matcher(entity, model))));
                }
            /*пары элементов (null-model)*/
            if (models != null)
                foreach (var model in models)
                {
                    if (entities?.FirstOrDefault(ent => matcher(ent, model)) == null)
                        result.Add(new ModelEntityPair<E, M>(default, model));
                }
            return result;
        }

        public void UpdateRange<E,M>(List<ModelEntityPair<E, M>> fulljoin, Action<M> add, Action<E> delete, Action<E,M> update)
        {
            foreach (var pair in fulljoin)
            {
                if (pair.Entity == null)
                {
                    add(pair.Model);
                }
                else if (pair.Model == null)
                {
                    delete(pair.Entity);
                }
                else
                {
                    update(pair.Entity, pair.Model);
                }
            }
        }
    }
}
