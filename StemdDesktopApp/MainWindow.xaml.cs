using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;
using DbCore;

namespace StemdDesktopApp
{
    public partial class MainWindow : Window
    {
        private DatabaseManager _dbManager;
        private CourseEngine _courseEngine;
        private int _currentSessionScore = 0;
        private string _currentUser = "";
        private string _currentMajor = "";

        public MainWindow()
        {
            InitializeComponent();

            // Initialize Polyglot Database (C# -> SQLite)
            _dbManager = new DatabaseManager("stemd_polyglot.db");
            _dbManager.InitializeDatabase();

            // Initialize CourseEngine and load JSON data
            _courseEngine = new CourseEngine();
            try
            {
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "courses.json");
                _courseEngine.LoadCourses(jsonPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load courses: {ex.Message}");
            }

            // Populate the major dropdown from courses.json (NO hardcoded items)
            PopulateMajorDropdown();
        }

        // =====================================================
        //  DYNAMIC MAJOR DROPDOWN (from courses.json)
        // =====================================================

        private void PopulateMajorDropdown()
        {
            RegMajor.Items.Clear();
            var majors = _courseEngine.GetAvailableMajors();
            foreach (var major in majors)
            {
                RegMajor.Items.Add(new ComboBoxItem { Content = major, FontSize = 14 });
            }
            if (RegMajor.Items.Count > 0)
                RegMajor.SelectedIndex = 0;
        }

        // =====================================================
        //  AUTHENTICATION & LOGIN
        // =====================================================

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RegUsername.Text)) return;

            _currentUser = RegUsername.Text;
            _currentMajor = (RegMajor.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Computer Science";

            // Save to DB
            _dbManager.AddUser(_currentUser);

            // Select major in CourseEngine
            _courseEngine.SelectMajor(_currentMajor);

            // Setup Dashboard dynamically
            DashUsername.Text = $"Welcome, {_currentUser}!";
            
            // Sync Settings & Analytics UI
            if (SettingsUserDisplay != null) SettingsUserDisplay.Text = _currentUser;
            if (SettingsMajorDisplay != null) SettingsMajorDisplay.Text = _currentMajor;

            // Set real date/time on sidebar
            SidebarDate.Text = DateTime.Now.ToString("MMMM dd, yyyy");
            SidebarDay.Text = DateTime.Now.ToString("dddd");

            // Populate everything
            PopulateDashboardCards();
            PopulateSidebarTasks();
            PopulateDashActivityChart();
            PopulateResources();
            PopulateLearningPlan();
            UpdateDashboardStats();

            // Set version text
            if (AppVersionText != null) AppVersionText.Text = "Version 1.0.42-PROD";

            // Transition UI
            ViewLogin.Visibility = Visibility.Collapsed;
            MainAppLayout.Visibility = Visibility.Visible;

            // Set Dashboard as active
            Menu_Dashboard_Checked(null, null);
        }

        // =====================================================
        //  DYNAMIC DASHBOARD POPULATION
        // =====================================================

        private void PopulateDashboardCards()
        {
            DashCourseCards.Items.Clear();
            DashCourseRows.Items.Clear();

            var courses = _courseEngine.GetActiveMajorCourses();
            string[] cardColors = { "#5B58EC", "#20C997", "#FF6B6B", "#F0AD4E", "#17A2B8" };
            string[] icons = { "📚", "⚙️", "🧪", "🤖", "💻" };

            int i = 0;
            foreach (var course in courses)
            {
                string color = cardColors[i % cardColors.Length];
                string icon = icons[i % icons.Length];

                // Build top carousel card
                var card = new Border
                {
                    Width = 220,
                    Margin = new Thickness(0, 0, 20, 0),
                    Padding = new Thickness(15),
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(15),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, Opacity = 0.03, BlurRadius = 10, ShadowDepth = 2 }
                };

                var cardStack = new StackPanel();

                // Header area
                var headerBorder = new Border { Height = 80, CornerRadius = new CornerRadius(10), Margin = new Thickness(0, 0, 0, 15) };
                headerBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                headerBorder.Child = new TextBlock { Text = _currentMajor, Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                cardStack.Children.Add(headerBorder);

                var catPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
                catPanel.Children.Add(new TextBlock { Text = icon, Margin = new Thickness(0, 0, 5, 0) });
                catPanel.Children.Add(new TextBlock { Text = _currentMajor, FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888")) });
                cardStack.Children.Add(catPanel);

                cardStack.Children.Add(new TextBlock { Text = course.title, FontWeight = FontWeights.Bold, FontSize = 16, Margin = new Thickness(0, 0, 0, 5) });
                cardStack.Children.Add(new TextBlock { Text = $"{course.topics.Count} Topics  |  {course.topics.Count} Quizzes", FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999")), Margin = new Thickness(0, 0, 0, 15) });

                // Progress bar (mock for non-active courses)
                double progress = 0;
                if (course.title == _courseEngine.ActiveCourseTitle)
                {
                   int total = _courseEngine.TopicCount;
                   if (total > 0) progress = (double)_courseEngine.GetCompletedCount() / total * 200; // 200 is outer width approx? No, let's use actual width.
                }

                var progressOuter = new Border { Height = 6, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEEEEE")), CornerRadius = new CornerRadius(3) };
                var progressInner = new Border { Width = (progress > 0 ? progress : 10), HorizontalAlignment = HorizontalAlignment.Left, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), CornerRadius = new CornerRadius(3) };
                progressOuter.Child = progressInner;
                cardStack.Children.Add(progressOuter);

                card.Child = cardStack;
                DashCourseCards.Items.Add(card);

                // Build course row list item
                var rowBorder = new Border
                {
                    Padding = new Thickness(15), Margin = new Thickness(0, 0, 0, 10),
                    Background = Brushes.White, CornerRadius = new CornerRadius(15),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, Opacity = 0.03, BlurRadius = 10, ShadowDepth = 2 }
                };

                var rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var rowIcon = new Border
                {
                    Width = 60, Height = 60, CornerRadius = new CornerRadius(10), Margin = new Thickness(0, 0, 15, 0),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color))
                };
                rowIcon.Child = new TextBlock { Text = icon, FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(rowIcon, 0);
                rowGrid.Children.Add(rowIcon);

                var rowInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                rowInfo.Children.Add(new TextBlock { Text = course.title, FontWeight = FontWeights.Bold, FontSize = 16 });
                var rowMeta = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                rowMeta.Children.Add(new TextBlock { Text = $"📚 {_currentMajor}", FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888")), Margin = new Thickness(0, 0, 15, 0) });
                rowMeta.Children.Add(new TextBlock { Text = $"👥 {course.topics.Count} Topics", FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888")), Margin = new Thickness(0, 0, 15, 0) });
                rowInfo.Children.Add(rowMeta);

                Grid.SetColumn(rowInfo, 1);
                rowGrid.Children.Add(rowInfo);
                rowBorder.Child = rowGrid;
                DashCourseRows.Items.Add(rowBorder);

                i++;
            }
        }

        private void PopulateSidebarTasks()
        {
            var titles = _courseEngine.GetTopicTitlesRaw();

            if (titles.Count > 0)
            {
                SideTask1Title.Text = titles[0];
                SideTask1Sub.Text = "📖 Ready";
            }
            if (titles.Count > 1)
            {
                SideTask2Title.Text = titles[1];
                SideTask2Sub.Text = "🔒 Locked";
            }
            if (titles.Count > 2)
            {
                SideTask3Title.Text = titles[2];
                SideTask3Sub.Text = "🔒 Locked";
            }
        }

        private void PopulateDashActivityChart()
        {
            if (DashActivityChart == null) return;
            DashActivityChart.Items.Clear();

            string[] days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            Random rnd = new Random();
            int baseScore = _currentSessionScore > 0 ? _currentSessionScore : 30;

            for (int i = 0; i < 7; i++)
            {
                var stack = new StackPanel { Margin = new Thickness(0, 0, 15, 0), VerticalAlignment = VerticalAlignment.Bottom };
                
                int h1 = rnd.Next(20, Math.Min(100, baseScore + 40));
                int h2 = rnd.Next(10, Math.Min(90, baseScore + 20));

                stack.Children.Add(new Border { Width = 6, Height = h1, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5B58EC")), CornerRadius = new CornerRadius(3), Margin = new Thickness(0, 0, 0, 2) });
                stack.Children.Add(new Border { Width = 6, Height = h2, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E50B5")), CornerRadius = new CornerRadius(3) });
                stack.Children.Add(new TextBlock { Text = days[i], FontSize = 10, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888")), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 0) });

                DashActivityChart.Items.Add(stack);
            }
        }

        private void PopulateResources()
        {
            if (ResourcesPanel == null) return;
            ResourcesPanel.Items.Clear();

            var resources = _courseEngine.GetActiveMajorResources();
            // Fallback if JSON is empty/missing
            if (resources.Count == 0)
            {
                resources.Add(new ResourceData { title = "📚 Library", description = $"Access textbooks for {_currentMajor}.", button = "Open" });
                resources.Add(new ResourceData { title = "🧪 Virtual Labs", description = "Simulated experiments.", button = "Enter" });
                resources.Add(new ResourceData { title = "🤖 AI Tools", description = "Topic-specific AI assistants.", button = "Get PDF" });
            }

            foreach (var res in resources)
            {
                var card = new Border
                {
                    Width = 250, Margin = new Thickness(0, 0, 20, 20), Padding = new Thickness(20),
                    Background = Brushes.White, CornerRadius = new CornerRadius(15),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, Opacity = 0.04, BlurRadius = 10, ShadowDepth = 2 }
                };

                var stack = new StackPanel();
                stack.Children.Add(new TextBlock { Text = res.title, FontWeight = FontWeights.Bold, FontSize = 16, Margin = new Thickness(0, 0, 0, 10) });
                stack.Children.Add(new TextBlock { Text = res.description, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")), FontSize = 12, Margin = new Thickness(0, 0, 0, 15), TextWrapping = TextWrapping.Wrap });
                
                var btn = new Button { Content = res.button, HorizontalAlignment = HorizontalAlignment.Left, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5B58EC")), Foreground = Brushes.White, Padding = new Thickness(15,8,15,8), BorderThickness = new Thickness(0) };
                stack.Children.Add(btn);

                card.Child = stack;
                ResourcesPanel.Items.Add(card);
            }
        }

        private void UpdateDashboardStats()
        {
            int total = _courseEngine.TopicCount;
            if (total == 0) return;

            int done = _courseEngine.GetCompletedCount();
            int current = _courseEngine.GetInProgressCount();
            int locked = _courseEngine.GetLockedCount();

            // Calculate exact percentages
            double pDone = (double)done / total * 100;
            double pCurrent = (double)current / total * 100;
            double pLocked = (double)locked / total * 100;

            if (DashPercentCompleted != null) DashPercentCompleted.Text = $"{(int)pDone}%";
            if (DashPercentInProgress != null) DashPercentInProgress.Text = $"{(int)pCurrent}%";
            if (DashPercentLocked != null) DashPercentLocked.Text = $"{(int)pLocked}%";
            
            if (DashScore != null) DashScore.Text = $"Score: {_currentSessionScore}";
        }

        private void PopulateLearningPlan()
        {
            if (LearningMilestones == null) return;
            LearningMilestones.Items.Clear();

            var titles = _courseEngine.GetTopicTitlesRaw();
            string[] days = { "Monday", "Wednesday", "Friday", "Next Week", "Later" };

            for (int i = 0; i < Math.Min(titles.Count, 5); i++)
            {
                var border = new Border { BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEEEEE")), BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(0, 10, 0, 10), Margin = new Thickness(0, 0, 0, 5) };
                
                var grid = new Grid();
                grid.Children.Add(new TextBlock { Text = titles[i], VerticalAlignment = VerticalAlignment.Center });
                grid.Children.Add(new TextBlock { Text = days[i % days.Length], HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888")) });

                border.Child = grid;
                LearningMilestones.Items.Add(border);
            }
        }

        // =====================================================
        //  NAVIGATION LOGIC
        // =====================================================

        private void HideAllViews()
        {
            if (ViewDashboard != null) ViewDashboard.Visibility = Visibility.Collapsed;
            if (ViewCourse != null)
            {
                ViewCourse.Visibility = Visibility.Collapsed;
                // Anti-cheat: clear quiz state when leaving
                if (QuizQuestionText != null) QuizQuestionText.Text = "Loading...";
                if (QuizAnswerInput != null) QuizAnswerInput.Text = "";
                if (QuizFeedback != null) QuizFeedback.Text = "";
                if (CourseChatHistory != null) CourseChatHistory.Text = "AI: Hello! Ask me about this topic.";
                if (QuizPanel != null) QuizPanel.Visibility = Visibility.Collapsed;
                if (LessonPanel != null) LessonPanel.Visibility = Visibility.Visible;
                if (NextTopicBtn != null) NextTopicBtn.Visibility = Visibility.Collapsed;
            }
            if (ViewAnalytics != null) ViewAnalytics.Visibility = Visibility.Collapsed;
            if (ViewSettings != null) ViewSettings.Visibility = Visibility.Collapsed;
            if (ViewResources != null) ViewResources.Visibility = Visibility.Collapsed;
            if (ViewLearningPlan != null) ViewLearningPlan.Visibility = Visibility.Collapsed;
            if (ViewChat != null) ViewChat.Visibility = Visibility.Collapsed;
            if (RightSidebarPanel != null) RightSidebarPanel.Visibility = Visibility.Collapsed;
        }

        private void Menu_Dashboard_Checked(object? sender, RoutedEventArgs? e)
        {
            HideAllViews();
            if (ViewDashboard != null)
            {
                ViewDashboard.Visibility = Visibility.Visible;
                DashScore.Text = $"Score: {_currentSessionScore}";
            }
            if (RightSidebarPanel != null)
            {
                RightSidebarPanel.Visibility = Visibility.Visible;
            }
        }

        private void Menu_Course_Checked(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            if (ViewCourse != null)
            {
                ViewCourse.Visibility = Visibility.Visible;
                RefreshTopicList();
            }
        }

        private void Menu_Analytics_Checked(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            if (ViewAnalytics != null) ViewAnalytics.Visibility = Visibility.Visible;
        }

        private void Menu_Settings_Checked(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            if (ViewSettings != null)
            {
                ViewSettings.Visibility = Visibility.Visible;
                SettingsUserDisplay.Text = _currentUser;
                SettingsMajorDisplay.Text = _currentMajor;
            }
        }

        private void Menu_Resources_Checked(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            if (ViewResources != null) ViewResources.Visibility = Visibility.Visible;
        }

        private void Menu_LearningPlan_Checked(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            if (ViewLearningPlan != null) ViewLearningPlan.Visibility = Visibility.Visible;
        }

        private void Menu_Chat_Checked(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            if (ViewChat != null) ViewChat.Visibility = Visibility.Visible;
        }

        private void CourseContinue_Click(object sender, RoutedEventArgs e)
        {
            CourseMenuBtn.IsChecked = true;
        }

        // =====================================================
        //  DYNAMIC COURSE ENGINE UI
        // =====================================================

        private void RefreshTopicList()
        {
            TopicListBox.Items.Clear();
            CourseOutlineTitle.Text = _courseEngine.ActiveCourseTitle;
            CourseOutlineSubtitle.Text = $"Major: {_courseEngine.ActiveMajor}";

            var titles = _courseEngine.GetTopicTitles();
            for (int i = 0; i < titles.Count; i++)
            {
                var item = new ListBoxItem
                {
                    Content = titles[i],
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 5),
                    Tag = i,
                    IsEnabled = _courseEngine.IsTopicUnlocked(i),
                    Opacity = _courseEngine.IsTopicUnlocked(i) ? 1.0 : 0.5
                };
                TopicListBox.Items.Add(item);
            }

            if (TopicListBox.Items.Count > _courseEngine.CurrentTopicIndex)
                TopicListBox.SelectedIndex = _courseEngine.CurrentTopicIndex;

            // Also update sidebar task indicators
            UpdateSidebarTaskStatus();
        }

        private void UpdateSidebarTaskStatus()
        {
            var raw = _courseEngine.GetTopicTitlesRaw();
            if (raw.Count > 0)
            {
                SideTask1Title.Text = raw[0];
                SideTask1Sub.Text = _courseEngine.IsTopicCompleted(0) ? "✅ Done" : "📖 Ready";
            }
            if (raw.Count > 1)
            {
                SideTask2Title.Text = raw[1];
                SideTask2Sub.Text = _courseEngine.IsTopicCompleted(1) ? "✅ Done" : (_courseEngine.IsTopicUnlocked(1) ? "📖 Ready" : "🔒 Locked");
            }
            if (raw.Count > 2)
            {
                SideTask3Title.Text = raw[2];
                SideTask3Sub.Text = _courseEngine.IsTopicCompleted(2) ? "✅ Done" : (_courseEngine.IsTopicUnlocked(2) ? "📖 Ready" : "🔒 Locked");
            }
        }

        private void TopicListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TopicListBox.SelectedItem is not ListBoxItem selected) return;
            int index = (int)selected.Tag;

            if (!_courseEngine.IsTopicUnlocked(index))
            {
                MessageBox.Show("Complete the previous topic's quiz first to unlock this one!", "Topic Locked", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _courseEngine.NavigateToTopic(index);
            ShowLessonForCurrentTopic();
        }

        private void ShowLessonForCurrentTopic()
        {
            var topic = _courseEngine.GetCurrentTopic();
            if (topic == null) return;

            LessonPanel.Visibility = Visibility.Visible;
            QuizPanel.Visibility = Visibility.Collapsed;

            LessonTitle.Text = topic.title;
            LessonContent.Text = topic.content;

            if (_courseEngine.IsTopicCompleted(_courseEngine.CurrentTopicIndex))
            {
                TopicStatusText.Text = "✅ Completed";
                TakeQuizBtn.Content = "📝 Retake Quiz";
            }
            else
            {
                TopicStatusText.Text = "";
                TakeQuizBtn.Content = "📝 Take Quiz on This Topic";
            }
        }

        private void TakeQuiz_Click(object sender, RoutedEventArgs e)
        {
            var topic = _courseEngine.GetCurrentTopic();
            if (topic == null) return;

            LessonPanel.Visibility = Visibility.Collapsed;
            QuizPanel.Visibility = Visibility.Visible;
            NextTopicBtn.Visibility = Visibility.Collapsed;
            QuizFeedback.Text = "";
            QuizAnswerInput.Text = "";

            ActiveLessonTitle.Text = $"Quiz: {topic.title}";
            _ = LoadRandomQuiz();
        }

        private void BackToLesson_Click(object sender, RoutedEventArgs e)
        {
            ShowLessonForCurrentTopic();
        }

        private void NextTopic_Click(object sender, RoutedEventArgs e)
        {
            if (_courseEngine.IsCourseComplete())
            {
                MessageBox.Show("🎓 Congratulations! You have completed the entire course!\n\nHead to Analytics to generate your certificate.", "Course Complete!", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_courseEngine.AdvanceToNextTopic())
            {
                RefreshTopicList();
                ShowLessonForCurrentTopic();
            }
        }

        // =====================================================
        //  POLYGLOT: AI QUIZ GENERATION (Python)
        // =====================================================

        private async Task LoadRandomQuiz()
        {
            QuizQuestionText.Text = "Generating a randomized quiz question...";
            QuizAnswerInput.Text = "";
            QuizFeedback.Text = "";

            var topic = _courseEngine.GetCurrentTopic();
            string quizSubject = topic?.quiz_subject ?? _currentMajor;

            try
            {
                string pythonPath = "python";
                string scriptPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\ai-engine\tutor.py"));

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = $"\"{scriptPath}\" --mode quiz --subject \"{quizSubject}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process? process = Process.Start(startInfo);
                if (process == null)
                {
                    QuizQuestionText.Text = "Failed to start Python engine.";
                    return;
                }
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    QuizQuestionText.Text = output.Trim();
                }
                else
                {
                    QuizQuestionText.Text = "Failed to load quiz. Check Python installation.";
                }
            }
            catch (Exception ex)
            {
                QuizQuestionText.Text = $"Error: {ex.Message}";
            }
        }

        private void SubmitQuiz_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(QuizAnswerInput.Text)) return;

            _courseEngine.CompleteCurrentTopic();
            _currentSessionScore += 15;
            _dbManager.UpdateProgress(1, 1, true, 15);
            
            QuizFeedback.Text = "✅ Correct! +15 Points — Topic Unlocked!";
            QuizFeedback.Foreground = Brushes.Green;

            UpdateDashboardStats();
            PopulateDashActivityChart();

            if (!_courseEngine.IsCourseComplete())
            {
                NextTopicBtn.Visibility = Visibility.Visible;
                var nextTopic = _courseEngine.GetTopicTitlesRaw();
                int nextIdx = _courseEngine.CurrentTopicIndex + 1;
                string nextName = nextIdx < nextTopic.Count ? nextTopic[nextIdx] : "Next";
                NextTopicBtn.Content = $"➡ {nextName}";
            }
            else
            {
                QuizFeedback.Text = "🎓 Course Complete! Go to Analytics for your certificate.";
                NextTopicBtn.Visibility = Visibility.Collapsed;
            }

            RefreshTopicList();
        }

        private async void GlobalChatSend_Click(object sender, RoutedEventArgs e)
        {
            string query = GlobalChatInput.Text;
            if (string.IsNullOrWhiteSpace(query)) return;

            GlobalChatHistory.Text += $"\n\nYou: {query}";
            GlobalChatInput.Text = "";
            GlobalChatHistory.Text += $"\nAI: Thinking...";
            GlobalChatScroller.ScrollToEnd();

            try
            {
                string pythonPath = "python";
                string scriptPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\ai-engine\tutor.py"));

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = $"\"{scriptPath}\" --mode tutor --subject \"{_currentMajor}\" --query \"{query}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process? process = Process.Start(startInfo);
                if (process == null) return;
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                GlobalChatHistory.Text = GlobalChatHistory.Text.Replace("\nAI: Thinking...", "");

                if (process.ExitCode == 0)
                    GlobalChatHistory.Text += $"\nAI: {output.Trim()}";
                else
                    GlobalChatHistory.Text += "\nAI [Error]: Could not reach engine.";
                    
                GlobalChatScroller.ScrollToEnd();
            }
            catch (Exception ex)
            {
                GlobalChatHistory.Text += $"\nAI [Error]: {ex.Message}";
                GlobalChatScroller.ScrollToEnd();
            }
        }

        // =====================================================
        //  POLYGLOT: EMBEDDED AI CHATBOT (Python)
        // =====================================================

        private async void CourseChatSend_Click(object sender, RoutedEventArgs e)
        {
            string query = CourseChatInput.Text;
            if (string.IsNullOrWhiteSpace(query)) return;

            var topic = _courseEngine.GetCurrentTopic();
            string contextSubject = topic?.quiz_subject ?? _currentMajor;

            CourseChatHistory.Text += $"\n\nYou: {query}";
            CourseChatInput.Text = "";
            CourseChatHistory.Text += $"\nAI: Thinking...";

            try
            {
                string pythonPath = "python";
                string scriptPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\ai-engine\tutor.py"));

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = $"\"{scriptPath}\" --mode tutor --subject \"{contextSubject}\" --query \"{query}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process? process = Process.Start(startInfo);
                if (process == null) return;
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                CourseChatHistory.Text = CourseChatHistory.Text.Replace("\nAI: Thinking...", "");

                if (process.ExitCode == 0)
                    CourseChatHistory.Text += $"\nAI: {output.Trim()}";
                else
                    CourseChatHistory.Text += "\nAI [Error]: Could not reach engine.";
            }
            catch (Exception ex)
            {
                CourseChatHistory.Text += $"\nAI [Error]: {ex.Message}";
            }
        }

        // =====================================================
        //  POLYGLOT: JAVA CERTIFICATE EXPORT
        // =====================================================

        private void GenerateCert_Click(object sender, RoutedEventArgs e)
        {
            CertStatusText.Text = "Invoking Java Analytics Engine...";
            try
            {
                string javaPath = "java";
                string reporterDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\reporter"));
                string destDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\"));

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = javaPath,
                    Arguments = $"-cp \"{reporterDir}\" CertificateGenerator \"{_currentUser}\" \"{_currentSessionScore}\" \"{destDir}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process? process = Process.Start(startInfo);
                if (process == null)
                {
                    CertStatusText.Text = "Failed to start Java.";
                    return;
                }
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                    CertStatusText.Text = $"Success! {output.Trim()}";
                else
                    CertStatusText.Text = $"Java Error: {error}";
            }
            catch (Exception ex)
            {
                CertStatusText.Text = $"Error: Ensure Java SDK is installed.\n{ex.Message}";
            }
        }
    }
}