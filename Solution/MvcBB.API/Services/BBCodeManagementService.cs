using MvcBB.Shared.Interfaces;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.API.Services
{
    /// <summary>
    /// Server-side implementation of IBBCodeManagementService: parsing/validation
    /// is delegated to the stateless ICoreBBCodeService, while tag/smilie
    /// persistence goes through the repository layer (InMemory/SQL/PostgreSql).
    /// </summary>
    public class BBCodeManagementService : IBBCodeManagementService
    {
        private readonly ICoreBBCodeService _coreBBCodeService;
        private readonly IBBCodeTagRepository _tagRepository;
        private readonly ISmilieRepository _smilieRepository;

        public BBCodeManagementService(
            ICoreBBCodeService coreBBCodeService,
            IBBCodeTagRepository tagRepository,
            ISmilieRepository smilieRepository)
        {
            _coreBBCodeService = coreBBCodeService;
            _tagRepository = tagRepository;
            _smilieRepository = smilieRepository;
        }

        public string ParseBBCode(string input) => _coreBBCodeService.ParseBBCode(input);

        public string StripBBCode(string input) => _coreBBCodeService.StripBBCode(input);

        public bool ValidateBBCode(string input) => _coreBBCodeService.ValidateBBCode(input);

        public IEnumerable<BBCodeTagModel> GetBBCodeTags() => _tagRepository.GetAll();

        public BBCodeTagModel GetBBCodeTag(int id) => _tagRepository.GetById(id);

        public void AddBBCodeTag(BBCodeTagModel model)
        {
            // Add() returns a new object with the generated Id populated rather
            // than necessarily mutating `model` in place (InMemoryBBCodeTagRepository
            // happens to do the latter, but SqlBBCodeTagRepository/PostgreSqlBBCodeTagRepository
            // construct a fresh object from the insert result) - copy it back so
            // callers relying on this void-returning method still see model.Id set.
            var created = _tagRepository.Add(model);
            model.Id = created.Id;
        }

        public void UpdateBBCodeTag(int id, BBCodeTagModel model)
        {
            model.Id = id;
            _tagRepository.Update(model);
        }

        public void DeleteBBCodeTag(int id)
        {
            var existing = _tagRepository.GetById(id);
            if (existing != null)
                _tagRepository.Remove(existing);
        }

        public IEnumerable<SmilieModel> GetSmilies() => _smilieRepository.GetAll();

        public SmilieModel GetSmilie(int id) => _smilieRepository.GetById(id);

        public void AddSmilie(SmilieModel model)
        {
            // See AddBBCodeTag for why the repository's return value needs to
            // be copied back rather than discarded.
            var created = _smilieRepository.Add(model);
            model.Id = created.Id;
        }

        public void UpdateSmilie(int id, SmilieModel model)
        {
            model.Id = id;
            _smilieRepository.Update(model);
        }

        public void DeleteSmilie(int id)
        {
            var existing = _smilieRepository.GetById(id);
            if (existing != null)
                _smilieRepository.Remove(existing);
        }
    }
}
