using GenTreesCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GenTreesCore.Services
{
    public class Replacements
    {
        public List<IdError> Errors { get; }

        private readonly Dictionary<int, IIdentified> replacements;

        public Replacements()
        {
            replacements = new Dictionary<int, IIdentified>();
            Errors = new List<IdError>();
        }

        public void Add(int modelId, IIdentified entity)
        {
            replacements[modelId] = entity;
        }

        public bool Contains<T>(int id)
        {
            return (replacements.ContainsKey(id) && replacements[id] is T);
        }

        /// <summary>
        /// Возвращает объект, который заменил модель с id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns><typeparamref name="T"/> если найдена замена; default(<typeparamref name="T"/>) если для <paramref name="id"/> не было замен</returns>
        public T Get<T>(int id) where T : IIdentified
        {
            return (T)replacements[id];
        }

        public Dictionary<int, int> GetIdReplacements()
        {
            return replacements.ToDictionary(x => x.Key, x => x.Value.Id);
        }

        public void AddError(int id, string message, bool wasRemoved)
        {
            Errors.Add(new IdError
            {
                Id = id,
                Message = message,
                WasRemoved = wasRemoved
            });
        }
    }

    public class IdError
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public bool WasRemoved { get; set; }
    }
}
