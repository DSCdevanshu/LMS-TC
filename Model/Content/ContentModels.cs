using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TCBackend.Model.Content
{
    // =================== SHARED LOOKUP ===================
    // ContentType NULL = category applies to all content types.
    [Table("ContentCategory")]
    public class ContentCategory
    {
        [Key]
        public int CategoryId { get; set; }
        public string? ContentType { get; set; }
        public string CategoryName { get; set; }
        public string Status { get; set; } = "A";
    }

    // =================== CORE CONTENT ROW ===================
    // One row per Announcement | Policy | Post.
    [Table("ContentItem")]
    public class ContentItem
    {
        [Key]
        public int ContentId { get; set; }
        public string ContentType { get; set; }
        public string? Kind { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public int? CategoryId { get; set; }
        public string Status { get; set; } = "Draft";

        // Announcement-specific
        public string? Priority { get; set; }
        public bool IsPinned { get; set; }
        public DateTime? PublishOn { get; set; }
        public DateTime? ExpiresOn { get; set; }

        // Policy-specific
        public int? OwnerDepartmentId { get; set; }
        public int RevisionNo { get; set; } = 1;

        // Audit
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }

        // Navigation
        [ForeignKey(nameof(CategoryId))]
        public ContentCategory? Category { get; set; }
        public ICollection<ContentAttachment> Attachments { get; set; } = new List<ContentAttachment>();
        public ICollection<ContentAudience> Audiences { get; set; } = new List<ContentAudience>();
        public ICollection<ContentEngagement> Engagements { get; set; } = new List<ContentEngagement>();
        public ICollection<ContentComment> Comments { get; set; } = new List<ContentComment>();
    }

    // =================== ATTACHMENTS ===================
    [Table("ContentAttachment")]
    public class ContentAttachment
    {
        [Key]
        public int AttachmentId { get; set; }
        public int ContentId { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string? ContentKind { get; set; }
        public long? FileSizeBytes { get; set; }
        public int? UploadedBy { get; set; }
        public DateTime UploadedOn { get; set; }

        [ForeignKey(nameof(ContentId))]
        public ContentItem? Content { get; set; }
    }

    // =================== AUDIENCE TARGETING ===================
    [Table("ContentAudience")]
    public class ContentAudience
    {
        [Key]
        public int AudienceId { get; set; }
        public int ContentId { get; set; }
        public string TargetType { get; set; }
        public int? TargetId { get; set; }

        [ForeignKey(nameof(ContentId))]
        public ContentItem? Content { get; set; }
    }

    // =================== PER-USER SIGNALS ===================
    // EngagementType: Read (Announcement) | Ack (Policy) | Like (Post).
    [Table("ContentEngagement")]
    public class ContentEngagement
    {
        [Key]
        public int EngagementId { get; set; }
        public int ContentId { get; set; }
        public int UserId { get; set; }
        public string EngagementType { get; set; }
        public string? ReactionType { get; set; }
        public int? RevisionNo { get; set; }
        public DateTime CreatedOn { get; set; }

        [ForeignKey(nameof(ContentId))]
        public ContentItem? Content { get; set; }
    }

    // =================== COMMENTS + REPLIES ===================
    [Table("ContentComment")]
    public class ContentComment
    {
        [Key]
        public int CommentId { get; set; }
        public int ContentId { get; set; }
        public int? ParentCommentId { get; set; }
        public string Body { get; set; }
        public string Status { get; set; } = "Active";
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }

        [ForeignKey(nameof(ContentId))]
        public ContentItem? Content { get; set; }

        [ForeignKey(nameof(ParentCommentId))]
        public ContentComment? ParentComment { get; set; }
        public ICollection<ContentComment> Replies { get; set; } = new List<ContentComment>();
    }
}
