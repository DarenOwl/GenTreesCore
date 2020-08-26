
namespace GenTreesCore.Services
{
    public class ModelEntityPair<TEntity, TModel>
    {
        public TEntity Entity { get; }
        public TModel Model { get; }

        public ModelEntityPair(TEntity entity, TModel model)
        {
            Entity = entity;
            Model = model;
        }
    }
}
