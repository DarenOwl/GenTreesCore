using System.Collections.Generic;

namespace GenTreesCore.Models
{
    public class ReplacementsModel
    {
        public Dictionary<int, int> Replacements { get; set; }
        public List<Services.IdError> Errors { get; set; }

        public ReplacementsModel()
        {
            Replacements = new Dictionary<int, int>();
            Errors = new List<Services.IdError>();
        }
    }
}
