using System;
using System.Collections.Generic;
using System.Linq;

namespace GenTreesCore.Services
{
    public abstract class Repository
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
        public IEnumerable<ModelEntityPair<E, M>> FullJoin<E, M>(IEnumerable<E> entities, IEnumerable<M> models, Func<E, M, bool> matcher)
            where E : class
            where M : class
        {
            /*пары элементов (entity-model) и (entity-null)*/
            foreach (var entity in entities)
            {
                yield return new ModelEntityPair<E, M>(entity, models?.FirstOrDefault(model => matcher(entity, model)));
            }
            /*пары элементов (null-model)*/
            foreach (var model in models)
            {
                if (entities?.FirstOrDefault(ent => matcher(ent, model)) == null)
                    yield return new ModelEntityPair<E, M>(default, model);
            }
        }
    }
}
