namespace MvcBB.Shared.Interfaces
{
    public interface ICoreBBCodeService
    {
        string ParseBBCode(string input);
        string StripBBCode(string input);
        bool ValidateBBCode(string input);
    }
} 