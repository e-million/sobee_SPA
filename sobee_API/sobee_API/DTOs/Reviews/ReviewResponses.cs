namespace sobee_API.DTOs.Reviews
{
    public sealed class ReviewListResponseDto
    {
        public int ProductId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int Count { get; set; }
        public ReviewSummaryDto Summary { get; set; } = new();
        public List<ReviewResponseDto> Reviews { get; set; } = new();
    }

    public sealed class ReviewSummaryDto
    {
        public int Total { get; set; }
        public decimal Average { get; set; }
        public int[] Counts { get; set; } = new int[5];
    }

    public sealed class ReviewResponseDto
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? ReviewText { get; set; }
        public DateTime Created { get; set; }
        public string? UserId { get; set; }
        public string? SessionId { get; set; }
        public List<ReviewReplyDto> Replies { get; set; } = new();
    }

    public sealed class ReviewReplyDto
    {
        public int ReplyId { get; set; }
        public int ReviewId { get; set; }
        public string? Content { get; set; }
        public DateTime Created { get; set; }
        public string? UserId { get; set; }
    }

    public sealed class ReviewCreatedResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? ReviewText { get; set; }
        public DateTime Created { get; set; }
        public string? UserId { get; set; }
        public string? SessionId { get; set; }
    }

    public sealed class ReplyCreatedResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public int ReplyId { get; set; }
        public int ReviewId { get; set; }
        public string? Content { get; set; }
        public DateTime Created { get; set; }
        public string? UserId { get; set; }
    }

    public sealed class ReviewDeletedResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public int ReviewId { get; set; }
    }

    public sealed class ReplyDeletedResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public int ReplyId { get; set; }
    }
}
