using MvcBB.Shared.Models.BBCode;

namespace MvcBB.Shared.Interfaces
{
    /// <summary>
    /// Extended BBCode service interface that adds management capabilities
    /// </summary>
    public interface IBBCodeManagementService : IBBCodeService
    {
        IEnumerable<BBCodeTagModel> GetBBCodeTags();
        BBCodeTagModel GetBBCodeTag(int id);
        void AddBBCodeTag(BBCodeTagModel model);
        void UpdateBBCodeTag(int id, BBCodeTagModel model);
        void DeleteBBCodeTag(int id);
        
        IEnumerable<SmilieModel> GetSmilies();
        SmilieModel GetSmilie(int id);
        void AddSmilie(SmilieModel model);
        void UpdateSmilie(int id, SmilieModel model);
        void DeleteSmilie(int id);
    }
} 