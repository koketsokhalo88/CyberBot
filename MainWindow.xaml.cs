using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CybersecurityChatbot
{
    // ==================== MAIN WINDOW ====================
    public partial class MainWindow : Window
    {
        // Services
        private AudioService _audioService;
        private ResponseRepository _responseRepo;
        private MemoryManager _memoryManager;
        private SentimentAnalyzer _sentimentAnalyzer;
        private ResponseEngine _responseEngine;
        private ConversationContext _context;
        private Random _random;

        // State
        private string _userName = "";
        private string _currentTopic = "";
        private bool _awaitingName = true;
        private bool _voiceEnabled = true;
        private bool _isProcessing = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeServices();
            DisplayAsciiArt();
            CheckVoiceStatus();
        }

        private void InitializeServices()
        {
            _random = new Random();
            _audioService = new AudioService();
            _responseRepo = new ResponseRepository();
            _memoryManager = new MemoryManager();
            _sentimentAnalyzer = new SentimentAnalyzer();
            _context = new ConversationContext();
            _responseEngine = new ResponseEngine(_responseRepo, _memoryManager, _sentimentAnalyzer, _context, _random);
        }

        private void DisplayAsciiArt()
        {
            string asciiArt = @"
    +=======================================+
    |     CYBERSECURITY BOT                 |
    |                                       |
    |        C Y B E R S E C U R I T Y      |
    |                                       |
    |       Protecting SA Online            |
    +=======================================+";
            LogoTextBlock.Text = asciiArt;
        }

        private void CheckVoiceStatus()
        {
            if (!_audioService.IsVoiceAvailable)
            {
                VoiceStatus.Text = "Voice: Unavailable";
                VoiceStatus.Foreground = Brushes.OrangeRed;
                _voiceEnabled = false;
                VoiceToggleButton.Content = "Voice OFF";
                VoiceToggleButton.Background = new SolidColorBrush(Color.FromRgb(239, 83, 80));
            }
        }

        // ==================== EVENT HANDLERS ====================

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessUserInput();
        }

        private void UserInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ProcessUserInput();
        }

        private void QuickTopicButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                UserInputTextBox.Text = button.Tag.ToString();
                ProcessUserInput();
            }
        }

        private void VoiceToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _voiceEnabled = !_voiceEnabled;
            if (_voiceEnabled)
            {
                VoiceToggleButton.Content = "Voice ON";
                VoiceToggleButton.Background = new SolidColorBrush(Color.FromRgb(102, 187, 106));
            }
            else
            {
                VoiceToggleButton.Content = "Voice OFF";
                VoiceToggleButton.Background = new SolidColorBrush(Color.FromRgb(239, 83, 80));
            }
        }

        private void ClearMemoryButton_Click(object sender, RoutedEventArgs e)
        {
            _memoryManager.ClearMemory();
            _userName = "";
            _currentTopic = "";
            _awaitingName = true;
            _context.Reset();
            UpdateUserInfo();
            UpdateTopicDisplay();
            AddBotMessage("Memory cleared! All user data has been reset. What is your name?");
        }

        // ==================== CORE LOGIC ====================

        private async void ProcessUserInput()
        {
            if (_isProcessing) return;

            string input = UserInputTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(input)) return;

            _isProcessing = true;
            UserInputTextBox.IsEnabled = false;
            SendButton.IsEnabled = false;

            AddUserMessage(input);
            UserInputTextBox.Clear();
            ShowTypingIndicator();
            await Task.Delay(600);

            try
            {
                string response;

                if (_awaitingName)
                {
                    _userName = input;
                    _memoryManager.StoreUserName(input);
                    _awaitingName = false;
                    response = "Nice to meet you, " + input + "! I am your Cybersecurity Awareness Assistant. You can ask me about password safety, phishing, malware, safe browsing, or social engineering. What would you like to know?";
                    UpdateUserInfo();
                }
                else
                {
                    Sentiment sentiment = _sentimentAnalyzer.Analyze(input);
                    UpdateSentimentDisplay(sentiment);

                    string lowerInput = input.ToLower();

                    if (lowerInput.Contains("my name") || lowerInput.Contains("who am i"))
                    {
                        string rememberedName = _memoryManager.GetUserName();
                        response = string.IsNullOrEmpty(rememberedName)
                            ? "I do not know your name yet. What should I call you?"
                            : "Your name is " + rememberedName + ", of course! I remember.";
                    }
                    else if (lowerInput.Contains("my interest") || lowerInput.Contains("what do i like"))
                    {
                        string interest = _memoryManager.GetUserInterest();
                        response = string.IsNullOrEmpty(interest)
                            ? "I do not know your interests yet. Tell me what cybersecurity topic interests you!"
                            : "You are interested in " + interest + "! I remembered that. Great choice for staying safe online!";
                    }
                    else if (IsFollowUpRequest(lowerInput))
                    {
                        if (!string.IsNullOrEmpty(_currentTopic))
                        {
                            response = _responseEngine.GetFollowUpResponse(_currentTopic, _userName);
                        }
                        else
                        {
                            response = "What topic would you like me to tell you more about? Try: password, phishing, malware, browsing, or social engineering.";
                        }
                    }
                    else if (IsGoodbye(lowerInput))
                    {
                        response = GetGoodbyeResponse(_userName);
                    }
                    else
                    {
                        response = _responseEngine.GenerateResponse(input, _userName);

                        string detectedTopic = _responseEngine.GetDetectedTopic(input);
                        if (!string.IsNullOrEmpty(detectedTopic))
                        {
                            _currentTopic = detectedTopic;
                            _context.SetTopic(detectedTopic);
                            UpdateTopicDisplay();

                            if (sentiment == Sentiment.Curious)
                            {
                                _memoryManager.StoreUserInterest(detectedTopic);
                            }
                        }
                    }

                    // Apply sentiment prefix/suffix
                    if (sentiment != Sentiment.Neutral)
                    {
                        string prefix = _sentimentAnalyzer.GetSentimentPrefix(sentiment);
                        string suffix = _sentimentAnalyzer.GetSentimentSuffix(sentiment);
                        response = prefix + response + suffix;
                    }
                }

                HideTypingIndicator();
                AddBotMessage(response);

                if (_voiceEnabled && _audioService.IsVoiceAvailable)
                {
                    await Task.Run(() => _audioService.Speak(response));
                }
            }
            catch (Exception ex)
            {
                HideTypingIndicator();
                AddBotMessage("I am not sure I understand. Can you try rephrasing? Try asking about: password, phishing, malware, browsing, or social engineering.");
            }
            finally
            {
                _isProcessing = false;
                UserInputTextBox.IsEnabled = true;
                SendButton.IsEnabled = true;
                UserInputTextBox.Focus();
            }
        }

        // ==================== HELPER METHODS ====================

        private bool IsFollowUpRequest(string input)
        {
            return input.Contains("tell me more") ||
                   input.Contains("explain more") ||
                   input.Contains("give me another") ||
                   input.Contains("another tip") ||
                   input.Contains("more info") ||
                   input.Contains("more details") ||
                   input.Contains("what else") ||
                   input.Contains("anything else");
        }

        private bool IsGoodbye(string input)
        {
            return input.Contains("bye") || input.Contains("goodbye") ||
                   input.Contains("exit") || input.Contains("quit") ||
                   input.Contains("see you") || input.Contains("farewell");
        }

        private string GetGoodbyeResponse(string userName)
        {
            string name = !string.IsNullOrEmpty(userName) ? userName : "friend";
            string[] goodbyes = new[]
            {
                "Stay safe online, " + name + "! Remember: think before you click!",
                "Goodbye " + name + "! Keep your passwords strong and your software updated!",
                "See you later, " + name + "! Do not let the scammers win!"
            };
            return goodbyes[_random.Next(goodbyes.Length)];
        }

        // ==================== UI METHODS ====================

        private void AddUserMessage(string message)
        {
            Border bubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                CornerRadius = new CornerRadius(15, 15, 0, 15),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(50, 5, 5, 5),
                Opacity = 0.9,
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap
                }
            };
            ChatStackPanel.Children.Add(bubble);
            ScrollToBottom();
        }

        private void AddBotMessage(string message)
        {
            Border bubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                CornerRadius = new CornerRadius(15, 15, 15, 0),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(5, 5, 50, 5),
                Opacity = 0.9,
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap
                }
            };
            ChatStackPanel.Children.Add(bubble);
            ScrollToBottom();
        }

        private void ShowTypingIndicator()
        {
            Border typingBubble = new Border
            {
                Name = "TypingIndicator",
                Background = new SolidColorBrush(Color.FromRgb(27, 38, 59)),
                CornerRadius = new CornerRadius(15, 15, 15, 0),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(5, 5, 50, 5),
                Child = new TextBlock
                {
                    Text = "Bot is typing...",
                    Foreground = new SolidColorBrush(Color.FromRgb(224, 225, 221)),
                    FontSize = 14
                }
            };
            ChatStackPanel.Children.Add(typingBubble);
            ScrollToBottom();
        }

        private void HideTypingIndicator()
        {
            for (int i = ChatStackPanel.Children.Count - 1; i >= 0; i--)
            {
                if (ChatStackPanel.Children[i] is Border border && border.Name == "TypingIndicator")
                {
                    ChatStackPanel.Children.RemoveAt(i);
                    break;
                }
            }
        }

        private void ScrollToBottom()
        {
            ChatScrollViewer.ScrollToEnd();
        }

        private void UpdateUserInfo()
        {
            if (!string.IsNullOrEmpty(_userName))
                UserInfoText.Text = "User: " + _userName;
        }

        private void UpdateTopicDisplay()
        {
            CurrentTopicText.Text = !string.IsNullOrEmpty(_currentTopic)
                ? "Topic: " + _currentTopic
                : "Topic: None";
        }

        private void UpdateSentimentDisplay(Sentiment sentiment)
        {
            string text = sentiment switch
            {
                Sentiment.Worried => "Mood: Worried",
                Sentiment.Curious => "Mood: Curious",
                Sentiment.Frustrated => "Mood: Frustrated",
                Sentiment.Happy => "Mood: Happy",
                Sentiment.Urgent => "Mood: Urgent",
                _ => "Mood: Neutral"
            };

            Brush color = sentiment switch
            {
                Sentiment.Worried => new SolidColorBrush(Color.FromRgb(255, 167, 38)),
                Sentiment.Frustrated => new SolidColorBrush(Color.FromRgb(239, 83, 80)),
                Sentiment.Curious => new SolidColorBrush(Color.FromRgb(0, 188, 212)),
                _ => new SolidColorBrush(Color.FromRgb(102, 187, 106))
            };

            SentimentText.Text = text;
            SentimentText.Foreground = color;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _audioService?.Dispose();
            base.OnClosing(e);
        }
    }

    // ==================== AUDIO SERVICE ====================
    public class AudioService : IDisposable
    {
        private SpeechSynthesizer _synth;
        private bool _hasVoices;
        private bool _disposed;

        public bool IsVoiceAvailable => _hasVoices;

        public AudioService()
        {
            try
            {
                _synth = new SpeechSynthesizer();
                var voices = _synth.GetInstalledVoices();
                _hasVoices = voices.Count > 0;

                if (_hasVoices)
                {
                    _synth.SetOutputToDefaultAudioDevice();
                    _synth.Volume = 100;
                    _synth.Rate = 0;
                    try { _synth.SelectVoiceByHints(VoiceGender.Neutral, VoiceAge.Adult); }
                    catch
                    {
                        try { _synth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult); }
                        catch { if (voices.Count > 0) _synth.SelectVoice(voices.First().VoiceInfo.Name); }
                    }
                }
            }
            catch { _hasVoices = false; }
        }

        public void Speak(string text)
        {
            if (!_hasVoices || _disposed || string.IsNullOrWhiteSpace(text)) return;
            try
            {
                string clean = text.Replace("\n", ". ").Replace("•", ", ");
                _synth.Speak(clean);
            }
            catch { }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try { _synth?.SpeakAsyncCancelAll(); } catch { }
                _synth?.Dispose();
                _disposed = true;
            }
        }
    }

    // ==================== SENTIMENT ENUM ====================
    public enum Sentiment
    {
        Neutral,
        Worried,
        Curious,
        Frustrated,
        Happy,
        Urgent
    }

    // ==================== SENTIMENT ANALYZER ====================
    public class SentimentAnalyzer
    {
        private Dictionary<string, List<string>> _keywords;

        public SentimentAnalyzer()
        {
            _keywords = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["worried"] = new List<string> { "worried", "scared", "afraid", "fear", "anxious", "nervous", "concerned", "terrified", "panic", "stressed", "overwhelmed", "not safe", "dangerous", "help me", "need help", "emergency" },
                ["curious"] = new List<string> { "curious", "interested", "want to know", "tell me more", "how does", "how do", "what is", "explain", "learn", "teach me", "more info", "details", "how to", "why is", "wondering" },
                ["frustrated"] = new List<string> { "frustrated", "annoyed", "angry", "mad", "confused", "do not understand", "not clear", "complicated", "difficult", "hard", "stupid", "useless", "not working", "hate", "do not get it" },
                ["happy"] = new List<string> { "happy", "glad", "pleased", "thank you", "thanks", "grateful", "appreciate", "great", "awesome", "excellent", "good", "helpful", "love it", "perfect", "wonderful", "amazing" },
                ["urgent"] = new List<string> { "urgent", "immediately", "right now", "asap", "quickly", "hurry", "fast", "emergency", "critical", "important", "need now", "must", "have to", "quick" }
            };
        }

        public Sentiment Analyze(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return Sentiment.Neutral;

            string lower = input.ToLower();
            var scores = new Dictionary<Sentiment, int>
            {
                [Sentiment.Worried] = 0,
                [Sentiment.Curious] = 0,
                [Sentiment.Frustrated] = 0,
                [Sentiment.Happy] = 0,
                [Sentiment.Urgent] = 0
            };

            foreach (var group in _keywords)
            {
                Sentiment s = (Sentiment)Enum.Parse(typeof(Sentiment), group.Key, true);
                foreach (string keyword in group.Value)
                    if (lower.Contains(keyword)) scores[s]++;
            }

            Sentiment best = Sentiment.Neutral;
            int max = 0;
            foreach (var score in scores)
            {
                if (score.Value > max) { max = score.Value; best = score.Key; }
            }
            return max > 0 ? best : Sentiment.Neutral;
        }

        public string GetSentimentPrefix(Sentiment sentiment)
        {
            return sentiment switch
            {
                Sentiment.Worried => "I understand your concern. ",
                Sentiment.Curious => "Great question! ",
                Sentiment.Frustrated => "I hear your frustration. Let me simplify this. ",
                Sentiment.Happy => "Wonderful! ",
                Sentiment.Urgent => "I understand this is urgent. ",
                _ => ""
            };
        }

        public string GetSentimentSuffix(Sentiment sentiment)
        {
            return sentiment switch
            {
                Sentiment.Worried => " Remember, staying informed is your best defense. You have got this!",
                Sentiment.Curious => " Keep that curiosity alive, it is your best protection online!",
                Sentiment.Frustrated => " Take it one step at a time. Cybersecurity can seem complex, but you are learning well!",
                Sentiment.Happy => " I am glad I could help! Stay safe out there!",
                Sentiment.Urgent => " If you suspect an active threat, contact your IT department immediately.",
                _ => ""
            };
        }
    }

    // ==================== MEMORY MANAGER ====================
    public class MemoryManager
    {
        private Dictionary<string, string> _memory;
        private List<string> _interests;

        public MemoryManager()
        {
            _memory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _interests = new List<string>();
        }

        public void StoreUserName(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
                _memory["name"] = name.Trim();
        }

        public string GetUserName()
        {
            return _memory.ContainsKey("name") ? _memory["name"] : string.Empty;
        }

        public void StoreUserInterest(string topic)
        {
            if (!string.IsNullOrWhiteSpace(topic))
            {
                string clean = topic.Trim().ToLower();
                _memory["latest_interest"] = clean;
                if (!_interests.Contains(clean)) _interests.Add(clean);
            }
        }

        public string GetUserInterest()
        {
            return _memory.ContainsKey("latest_interest") ? _memory["latest_interest"] : string.Empty;
        }

        public void ClearMemory()
        {
            _memory.Clear();
            _interests.Clear();
        }
    }

    // ==================== CONVERSATION CONTEXT ====================
    public class ConversationContext
    {
        public string CurrentTopic { get; set; } = "";
        public string PreviousTopic { get; set; } = "";
        public int FollowUpCount { get; set; } = 0;

        public void SetTopic(string topic)
        {
            if (!string.IsNullOrEmpty(CurrentTopic)) PreviousTopic = CurrentTopic;
            CurrentTopic = topic;
            FollowUpCount = 0;
        }

        public void IncrementFollowUp() { FollowUpCount++; }

        public void Reset()
        {
            CurrentTopic = "";
            PreviousTopic = "";
            FollowUpCount = 0;
        }
    }

    // ==================== RESPONSE REPOSITORY ====================
    public class ResponseRepository
    {
        private Dictionary<string, List<string>> _topicResponses;
        private Dictionary<string, string> _singleResponses;
        private Random _random;

        public ResponseRepository()
        {
            _random = new Random();
            InitializeResponses();
        }

        private void InitializeResponses()
        {
            _topicResponses = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            _singleResponses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            _topicResponses["password"] = new List<string>
            {
                "PASSWORD SAFETY: Use at least 12 characters with a mix of uppercase, lowercase, numbers, and symbols. Never reuse passwords across different accounts. Use a password manager to generate and store strong passwords. Enable two-factor authentication (2FA) wherever possible. Avoid using personal information like birthdays or pet names.",
                "STRONG PASSWORD TIPS: Create passphrases with 3-4 random words combined like BlueMonkey42Coffee. Change passwords immediately if you suspect a breach. Do not share passwords via email or messaging apps. Use different passwords for banking, email, and social media. Check if your password has been leaked using haveibeenpwned.com.",
                "PASSWORD BEST PRACTICES: Update critical passwords every 3-6 months. Use biometric authentication when available like fingerprint or face recognition. Be wary of password reset emails and verify they are legitimate. Never save passwords in browsers on shared computers. Consider using hardware security keys for maximum protection."
            };
            _topicResponses["passwords"] = _topicResponses["password"];

            _topicResponses["phishing"] = new List<string>
            {
                "PHISHING AWARENESS: Check sender email addresses carefully, scammers use slight misspellings. Hover over links before clicking to see the actual URL. Be suspicious of urgent language like Act now or Your account will be closed. Never download unexpected attachments, even from known contacts. Verify requests for personal information by contacting the organization directly.",
                "SPOTTING PHISHING EMAILS: Look for poor grammar and spelling mistakes. Generic greetings like Dear Customer instead of your name. Requests for passwords, PINs, or banking details. Legitimate organizations never ask for these via email. Threats or unrealistic promises like winning lotteries you did not enter. Check for HTTPS and valid certificates on websites asking for information.",
                "PHISHING PROTECTION: Report phishing emails to your IT department or email provider. Use email filters and anti-phishing browser extensions. Keep your email client and browser updated. Be extra cautious during tax season and holiday shopping periods. If unsure, call the company using a number from their official website, not from the email."
            };
            _topicResponses["email"] = _topicResponses["phishing"];
            _topicResponses["scam"] = _topicResponses["phishing"];
            _topicResponses["scams"] = _topicResponses["phishing"];

            _topicResponses["malware"] = new List<string>
            {
                "MALWARE PROTECTION: Install reputable antivirus software and keep it updated. Enable automatic updates for your operating system and all applications. Never download software from untrusted or pirated sources. Regularly backup your data to an external drive or cloud service. Be cautious of USB drives from unknown sources, they can spread malware.",
                "VIRUS AND RANSOMWARE DEFENSE: Use anti-malware tools with real-time protection. Scan all downloads before opening them. Disable macros in Office documents by default. Be wary of free software that asks for excessive permissions. If infected, disconnect from the internet immediately to prevent spread.",
                "MALWARE RECOGNITION: Signs of infection include slow performance, frequent crashes, and pop-up ads. Unexpected browser homepage changes or new toolbars. Unknown programs running in Task Manager. Friends receiving spam from your accounts. If you suspect malware, boot in Safe Mode and run a full system scan."
            };
            _topicResponses["virus"] = _topicResponses["malware"];
            _topicResponses["ransomware"] = _topicResponses["malware"];
            _topicResponses["antivirus"] = _topicResponses["malware"];

            _topicResponses["browsing"] = new List<string>
            {
                "SAFE BROWSING: Always look for https and the padlock icon before entering sensitive data. Avoid using public WiFi for banking, shopping, or accessing sensitive accounts. Keep your browser and all plugins updated. Do not click pop-ups saying you won a prize or your computer is infected. Use private or incognito mode for sensitive searches on shared computers.",
                "BROWSER SECURITY: Clear cookies and cache regularly. Use ad-blockers and script-blockers to prevent malicious ads. Enable Do Not Track requests in browser settings. Be cautious of browser extensions, only install from official stores. Use separate browser profiles for work and personal browsing.",
                "ONLINE SHOPPING SAFETY: Only shop on websites with secure payment processing. Use credit cards or secure payment services like PayPal instead of debit cards. Check for customer reviews and verify the website legitimacy. Be wary of deals that seem too good to be true. Save order confirmations and check bank statements regularly."
            };
            _topicResponses["internet"] = _topicResponses["browsing"];
            _topicResponses["browser"] = _topicResponses["browsing"];
            _topicResponses["wifi"] = _topicResponses["browsing"];

            _topicResponses["social engineering"] = new List<string>
            {
                "SOCIAL ENGINEERING: Be skeptical of unexpected phone calls, emails, or messages asking for information. Verify identities before sharing sensitive data, call back using official numbers. Do not let urgency or pressure tactics rush you into decisions. Legitimate IT support will NEVER ask for your password via email or phone. Be cautious of too friendly strangers asking about your work or personal life.",
                "COMMON SOCIAL ENGINEERING TACTICS: Pretexting is creating a fabricated scenario to gain information. Baiting is offering something enticing like a free download or USB drive to install malware. Quid pro quo is offering a service in exchange for information. Tailgating is following someone into a restricted area. Impersonation is pretending to be authority figures, IT staff, or coworkers.",
                "PROTECTING AGAINST SOCIAL ENGINEERING: Verify unexpected requests through a separate communication channel. Do not give out personal information on social media. Be careful what you reveal in fun quizzes and surveys. Train yourself to recognize manipulation tactics. When in doubt, verify with your supervisor or IT security team."
            };
            _topicResponses["social"] = _topicResponses["social engineering"];
            _topicResponses["engineering"] = _topicResponses["social engineering"];
            _topicResponses["manipulation"] = _topicResponses["social engineering"];

            _topicResponses["privacy"] = new List<string>
            {
                "PRIVACY PROTECTION: Review privacy settings on all social media accounts regularly. Limit the personal information you share online. Use privacy-focused browsers and search engines when possible. Be aware of what apps can access on your phone like camera, contacts, and location. Read privacy policies before agreeing to them, especially for free apps.",
                "DATA PRIVACY TIPS: Use a VPN when browsing on public networks. Turn off location services when not needed. Regularly audit app permissions on your devices. Use encrypted messaging apps for sensitive conversations. Be cautious of free services, they often monetize your data.",
                "ONLINE PRIVACY BEST PRACTICES: Use separate email addresses for different purposes like work, personal, and shopping. Enable two-factor authentication on all accounts. Be mindful of what photos and information you post publicly. Regularly search for yourself online to see what information is available. Consider using privacy-focused alternatives to mainstream services."
            };

            _singleResponses["purpose"] = "I am the Cybersecurity Awareness Bot, designed to educate South African citizens about online threats. I can help you with password safety, phishing awareness, malware protection, safe browsing, and social engineering defense.";
            _singleResponses["what can i ask"] = "You can ask me about password safety and management, phishing and email scams, malware and virus protection, safe browsing habits, social engineering tactics, and online privacy. Just type a keyword or ask a question!";
            _singleResponses["help"] = "AVAILABLE COMMANDS: Type a topic like password, phishing, malware, browsing, social engineering, or privacy. Ask follow-ups like tell me more or explain more. Check memory by asking what is my name or what am I interested in. Say hello or goodbye. I will remember your name and interests to personalize our conversation!";
            _singleResponses["how are you"] = "I am just a bot, but I am fully operational and ready to help you with cybersecurity! How are you doing today?";
            _singleResponses["default"] = "I am not sure I understand. Can you try rephrasing? Try asking about password, phishing, malware, browsing, social engineering, or privacy. Or type help for options.";
        }

        public string GetRandomResponse(string topic)
        {
            if (_topicResponses.ContainsKey(topic) && _topicResponses[topic].Count > 0)
                return _topicResponses[topic][_random.Next(_topicResponses[topic].Count)];
            if (_singleResponses.ContainsKey(topic))
                return _singleResponses[topic];
            return _singleResponses["default"];
        }

        public List<string> GetAllResponsesForTopic(string topic)
        {
            if (_topicResponses.ContainsKey(topic))
                return new List<string>(_topicResponses[topic]);
            return new List<string>();
        }

        public string GetSingleResponse(string key)
        {
            return _singleResponses.ContainsKey(key) ? _singleResponses[key] : _singleResponses["default"];
        }

        public string GetDefaultResponse() { return _singleResponses["default"]; }
        public string GetHelpResponse() { return _singleResponses["help"]; }

        public List<string> GetAllKeywords()
        {
            var keys = new List<string>();
            keys.AddRange(_topicResponses.Keys);
            keys.AddRange(_singleResponses.Keys);
            return keys;
        }

        public bool HasKeyword(string keyword)
        {
            return _topicResponses.ContainsKey(keyword) || _singleResponses.ContainsKey(keyword);
        }
    }

    // ==================== RESPONSE ENGINE ====================
    public class ResponseEngine
    {
        private ResponseRepository _repo;
        private MemoryManager _memory;
        private SentimentAnalyzer _sentiment;
        private ConversationContext _context;
        private Random _random;

        public ResponseEngine(ResponseRepository repo, MemoryManager memory, SentimentAnalyzer sentiment, ConversationContext context, Random random)
        {
            _repo = repo;
            _memory = memory;
            _sentiment = sentiment;
            _context = context;
            _random = random;
        }

        public string GenerateResponse(string input, string userName)
        {
            string lower = input.ToLower().Trim();

            // Check greetings
            if (IsGreeting(lower))
                return GetGreetingResponse(userName);

            // Check help
            if (IsHelpRequest(lower))
                return _repo.GetHelpResponse();

            // Check purpose
            if (lower.Contains("purpose") || lower.Contains("what do you do") || lower.Contains("who are you"))
                return _repo.GetSingleResponse("purpose");

            // Check keywords
            foreach (string keyword in _repo.GetAllKeywords())
            {
                if (lower.Contains(keyword) && keyword != "default" && keyword != "help" && keyword != "purpose")
                {
                    _context.SetTopic(keyword);
                    return _repo.GetRandomResponse(keyword);
                }
            }

            return _repo.GetDefaultResponse();
        }

        public string GetFollowUpResponse(string topic, string userName)
        {
            _context.IncrementFollowUp();
            var responses = _repo.GetAllResponsesForTopic(topic);
            if (responses.Count > 0)
            {
                int index = _context.FollowUpCount % responses.Count;
                return responses[index];
            }
            return _repo.GetRandomResponse(topic);
        }

        public string GetDetectedTopic(string input)
        {
            string lower = input.ToLower();
            foreach (string topic in _repo.GetAllKeywords())
            {
                if (lower.Contains(topic) && topic != "default" && topic != "help" && topic != "purpose")
                    return topic;
            }
            return string.Empty;
        }

        private bool IsGreeting(string input)
        {
            string[] greetings = { "hello", "hi", "hey", "greetings", "good morning", "good afternoon", "good evening", "howdy" };
            return greetings.Any(g => input.Contains(g));
        }

        private bool IsHelpRequest(string input)
        {
            string[] helpTerms = { "help", "what can you do", "what do you do", "options", "menu", "commands" };
            return helpTerms.Any(h => input.Contains(h));
        }

        private string GetGreetingResponse(string userName)
        {
            string name = !string.IsNullOrEmpty(userName) ? userName : "there";
            string[] greetings = new[]
            {
                "Hello " + name + "! Ready to learn about cybersecurity?",
                "Hi " + name + "! I am here to help you stay safe online.",
                "Hey " + name + "! What cybersecurity topic would you like to explore today?"
            };
            return greetings[_random.Next(greetings.Length)];
        }
    }
}
