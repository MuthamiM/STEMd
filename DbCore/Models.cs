using System;

namespace DbCore
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public int TotalScore { get; set; }
    }

    public class Subject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Progress
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SubjectId { get; set; }
        public bool IsCompleted { get; set; }
    }
}
