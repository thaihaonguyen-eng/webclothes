namespace webclothes.Policies
{
    public static class ReviewSubmissionPolicy
    {
        public static bool CanSubmit(bool hasOrdered, bool hasReviewed)
        {
            return hasOrdered && !hasReviewed;
        }

        public static string? GetValidationError(bool hasOrdered, bool hasReviewed, int rating, string? comment)
        {
            if (!hasOrdered)
            {
                return "Bạn chỉ có thể đánh giá sản phẩm sau khi đã mua và nhận hàng thành công!";
            }

            if (hasReviewed)
            {
                return "Bạn đã đánh giá sản phẩm này rồi.";
            }

            if (rating is < 1 or > 5)
            {
                return "Số sao đánh giá phải từ 1 đến 5.";
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                return "Vui lòng nhập nội dung đánh giá.";
            }

            if (comment.Trim().Length > 1000)
            {
                return "Nội dung đánh giá không được vượt quá 1000 ký tự.";
            }

            return null;
        }
    }
}
