
namespace FocusTimer.Models
{
    public class FocusTask
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string Notes { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Duration { get; set; }
        public bool IsCompleted { get; set; }
    }
} 