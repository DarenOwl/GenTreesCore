using System.Collections.Generic;

namespace GenTreesCore.Models
{
    public class Changes
    {
        public Dictionary<int, int> Replacements { get; set; }
        public List<IdError> Errors { get; set; }

        public Changes()
        {
            Replacements = new Dictionary<int, int>();
            Errors = new List<IdError>();
        }
    }

    public class IdError
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public IdError(int id, string message = null)
        {
            Id = id;
            Message = message;
        }
    }
}
