using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace StemdDesktopApp
{
    // --- Data Models --- //

    public class TopicData
    {
        public string title { get; set; } = "";
        public string content { get; set; } = "";
        public string quiz_subject { get; set; } = "";
    }

    public class ResourceData
    {
        public string title { get; set; } = "";
        public string description { get; set; } = "";
        public string button { get; set; } = "";
    }

    public class CourseData
    {
        public string title { get; set; } = "";
        public List<TopicData> topics { get; set; } = new();
    }

    public class MajorData
    {
        public List<ResourceData> resources { get; set; } = new();
        public List<CourseData> courses { get; set; } = new();
    }

    // --- Course Engine --- //

    public class CourseEngine
    {
        private Dictionary<string, MajorData> _allCourses = new();
        private CourseData? _activeCourse;
        private int _currentTopicIndex = 0;
        private HashSet<int> _completedTopics = new();

        public string ActiveMajor { get; private set; } = "";
        public string ActiveCourseTitle => _activeCourse?.title ?? "No Course Loaded";
        public int TopicCount => _activeCourse?.topics.Count ?? 0;
        public int CurrentTopicIndex => _currentTopicIndex;

        /// <summary>
        /// Load courses.json from the app directory.
        /// </summary>
        public void LoadCourses(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"Course data not found at: {jsonPath}");

            string json = File.ReadAllText(jsonPath);
            _allCourses = JsonSerializer.Deserialize<Dictionary<string, MajorData>>(json)
                          ?? new Dictionary<string, MajorData>();
        }

        /// <summary>
        /// Get all available majors from the loaded JSON.
        /// </summary>
        public List<string> GetAvailableMajors()
        {
            return new List<string>(_allCourses.Keys);
        }

        public List<CourseData> GetActiveMajorCourses()
        {
            if (_allCourses.ContainsKey(ActiveMajor))
                return _allCourses[ActiveMajor].courses;
            return new List<CourseData>();
        }

        public List<ResourceData> GetActiveMajorResources()
        {
            if (_allCourses.ContainsKey(ActiveMajor))
                return _allCourses[ActiveMajor].resources;
            return new List<ResourceData>();
        }

        public int GetCompletedCount() => _completedTopics.Count;
        
        public int GetInProgressCount() 
        {
            // The current active topic is always the one "In Progress"
            return 1; 
        }

        public int GetLockedCount()
        {
            int total = TopicCount;
            int done = GetCompletedCount();
            int current = GetInProgressCount();
            return Math.Max(0, total - done - current);
        }

        /// <summary>
        /// Select a major and activate its first course.
        /// </summary>
        public bool SelectMajor(string major)
        {
            // Try exact match first
            if (_allCourses.ContainsKey(major))
            {
                ActiveMajor = major;
                _activeCourse = _allCourses[major].courses[0];
                _currentTopicIndex = 0;
                _completedTopics.Clear();
                return true;
            }

            // Try partial match (e.g. " - Computer Science" -> "Computer Science")
            foreach (var key in _allCourses.Keys)
            {
                if (major.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    key.Contains(major, StringComparison.OrdinalIgnoreCase))
                {
                    ActiveMajor = key;
                    _activeCourse = _allCourses[key].courses[0];
                    _currentTopicIndex = 0;
                    _completedTopics.Clear();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get titles of all topics in the active course.
        /// </summary>
        public List<string> GetTopicTitles()
        {
            if (_activeCourse == null) return new List<string>();
            var titles = new List<string>();
            for (int i = 0; i < _activeCourse.topics.Count; i++)
            {
                string prefix = IsTopicUnlocked(i) ? (IsTopicCompleted(i) ? "✅ " : "📖 ") : "🔒 ";
                titles.Add($"{prefix}{i + 1}. {_activeCourse.topics[i].title}");
            }
            return titles;
        }

        /// <summary>
        /// Get a list of raw topic titles without any prefix emojis or numbers.
        /// </summary>
        public List<string> GetTopicTitlesRaw()
        {
            if (_activeCourse == null) return new List<string>();
            var titles = new List<string>();
            foreach (var topic in _activeCourse.topics)
            {
                titles.Add(topic.title);
            }
            return titles;
        }

        /// <summary>
        /// Check if a topic is unlocked (topic 0 is always unlocked, others need previous pass).
        /// </summary>
        public bool IsTopicUnlocked(int index)
        {
            if (index == 0) return true;
            return _completedTopics.Contains(index - 1);
        }

        /// <summary>
        /// Check if a topic has been completed.
        /// </summary>
        public bool IsTopicCompleted(int index)
        {
            return _completedTopics.Contains(index);
        }

        /// <summary>
        /// Navigate to a specific topic (if unlocked).
        /// </summary>
        public bool NavigateToTopic(int index)
        {
            if (_activeCourse == null) return false;
            if (index < 0 || index >= _activeCourse.topics.Count) return false;
            if (!IsTopicUnlocked(index)) return false;

            _currentTopicIndex = index;
            return true;
        }

        /// <summary>
        /// Get the current topic's data.
        /// </summary>
        public TopicData? GetCurrentTopic()
        {
            if (_activeCourse == null) return null;
            if (_currentTopicIndex < 0 || _currentTopicIndex >= _activeCourse.topics.Count) return null;
            return _activeCourse.topics[_currentTopicIndex];
        }

        /// <summary>
        /// Mark the current topic as completed (quiz passed).
        /// </summary>
        public void CompleteCurrentTopic()
        {
            _completedTopics.Add(_currentTopicIndex);
        }

        /// <summary>
        /// Check if all topics in the course are completed.
        /// </summary>
        public bool IsCourseComplete()
        {
            if (_activeCourse == null) return false;
            return _completedTopics.Count >= _activeCourse.topics.Count;
        }

        /// <summary>
        /// Advance to the next topic (if available and unlocked).
        /// </summary>
        public bool AdvanceToNextTopic()
        {
            if (_activeCourse == null) return false;
            int next = _currentTopicIndex + 1;
            if (next >= _activeCourse.topics.Count) return false;
            if (!IsTopicUnlocked(next)) return false;
            _currentTopicIndex = next;
            return true;
        }
    }
}
