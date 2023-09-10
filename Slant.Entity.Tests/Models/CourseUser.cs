namespace Slant.Entity.Tests.Models
{
    internal class CourseUser
    {
        public int CourseId { get; set; }
        public int UserId { get; set; }
        public string Grade { get; set; } = default!;

        // Navigation properties
        public Course Course { get; set; } = default!;
        public User User { get; set; } = default!;
    }
}