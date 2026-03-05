using MvcBB.Shared.Interfaces;

namespace MvcBB.App.Interfaces
{
    /// <summary>
    /// MVC application-specific BBCode service interface.
    /// Inherits all BBCode functionality from IBBCodeManagementService.
    /// This interface exists to provide a clear dependency for MVC components
    /// and to allow for future MVC-specific extensions if needed.
    /// </summary>
    public interface IMvcBBCodeService : IBBCodeManagementService
    {
        /// <summary>
        /// Gets a dictionary of available smilies where the key is the smilie code
        /// and the value is the HTML representation
        /// </summary>
        Dictionary<string, string> GetAvailableSmilies();
    }
} 