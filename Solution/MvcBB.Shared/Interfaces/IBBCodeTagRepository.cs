using MvcBB.Shared.Models.BBCode;

namespace MvcBB.Shared.Interfaces
{
    public interface IBBCodeTagRepository
    {
        IReadOnlyList<BBCodeTagModel> GetAll();
        BBCodeTagModel? GetById(int id);
        BBCodeTagModel Add(BBCodeTagModel tag);
        void Update(BBCodeTagModel tag);
        void Remove(BBCodeTagModel tag);
    }
}
