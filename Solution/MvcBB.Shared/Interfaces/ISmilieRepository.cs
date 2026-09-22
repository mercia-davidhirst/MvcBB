using MvcBB.Shared.Models.BBCode;

namespace MvcBB.Shared.Interfaces
{
    public interface ISmilieRepository
    {
        IReadOnlyList<SmilieModel> GetAll();
        SmilieModel? GetById(int id);
        SmilieModel Add(SmilieModel smilie);
        void Update(SmilieModel smilie);
        void Remove(SmilieModel smilie);
    }
}
