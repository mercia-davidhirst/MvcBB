using System.ComponentModel.DataAnnotations;

namespace MvcBB.App.Models;

/// <summary>
/// Data transfer object for creating a new thread
/// </summary>
public class CreateThreadDto
{
    /// <summary>
    /// Title of the new thread
    /// </summary>
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Initial content/post of the thread
    /// </summary>
    [Required(ErrorMessage = "Content is required")]
    [StringLength(10000, MinimumLength = 10, ErrorMessage = "Content must be between 10 and 10000 characters")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// ID of the board where the thread will be created
    /// </summary>
    [Required(ErrorMessage = "Board ID is required")]
    public int BoardId { get; set; }

    /// <summary>
    /// Whether the thread should be pinned to the top of the board
    /// </summary>
    public bool IsSticky { get; set; }
} 