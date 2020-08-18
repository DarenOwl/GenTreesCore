using System.Collections.Generic;

namespace GenTreesCore.Models
{
    public class TreeUpdateResult
    {
        public Dictionary<int, int> Replacements { get; set; }
        public List<IdError> Errors { get; set; }
    }

    public class IdError
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public IdError(int id, string message)
        {
            Id = id;
            Message = message;
        }
    }
}
