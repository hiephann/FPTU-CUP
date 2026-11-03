namespace fptu_cup.Common
{
    /// <summary>
    /// Giờ đá (KickoffTime) lưu theo giờ địa phương Việt Nam (UTC+7, không có giờ mùa hè),
    /// nên mọi phép so sánh với "bây giờ" phải dùng giờ Việt Nam, không phụ thuộc múi giờ của máy chủ.
    /// </summary>
    public static class VietnamTime
    {
        /// <summary>Hạn nộp đội hình: trước giờ bóng lăn 1 ngày (BR23).</summary>
        public const int LineupDeadlineHours = 24;

        public static DateTime Now => DateTime.UtcNow.AddHours(7);

        public static DateTime LineupDeadline(DateTime kickoff) => kickoff.AddHours(-LineupDeadlineHours);
    }
}
