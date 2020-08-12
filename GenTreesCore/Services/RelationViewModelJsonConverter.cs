using GenTreesCore.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace GenTreesCore.Services
{
    /// <summary>
    /// Json Converter for deserialization RelationViewModel subclasses using RelationType property
    /// </summary>
    class RelationViewModelJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(RelationViewModel).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            string relationType = (string)jo[nameof(RelationViewModel.RelationType)];

            RelationViewModel relation;
            if (relationType == new SpouseRelationViewModel().GetRelationName())
                relation = new SpouseRelationViewModel();
            else if (relationType == new ChildRelationViewModel().GetRelationName())
                relation = new ChildRelationViewModel();
            else
                relation = new RelationViewModel();

            serializer.Populate(jo.CreateReader(), relation);

            return relation;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
