# CyberBot

This is a WPF (Windows Presentation Foundation) Cybersecurity Awareness Chatbot built for PROG6221 POE Part 2. The application features a graphical user interface with dynamic responses, sentiment detection, memory/recall functionality, and text-to-speech capabilities.

 Features Implemented

 1. GUI Design and Implementation (WPF)
- Dark cybersecurity-themed interface with professional colour scheme
- ASCII art logo displayed in header
- Message bubbles with different colours for user and bot
- Quick topic buttons for easy navigation
- Status indicators for voice and memory
- User info bar showing name, current topic, and detected mood

 2. Generic Collections
- `Dictionary<string, List<string>>` for topic-response mapping
- `List<string>` for random response selection
- `Dictionary<string, string>` for single responses
- `List<string>` for user interests and conversation history

 3. Delegates
- Custom delegates for response handling and error processing
- Extensible architecture for future enhancements

 4. Keyword Recognition
Recognises 6 cybersecurity topics:
- **Password** - password safety and management
- **Phishing** - email scams and fraudulent links
- **Malware** - viruses, ransomware, and protection
- **Browsing** - safe internet browsing habits
- **Social Engineering** - manipulation tactics and defense
- **Privacy** - online privacy and data protection

 5. Random Responses
Each topic has 3+ unique responses randomly selected to keep interactions varied and engaging.

 6. Conversation Flow
- Handles follow-up requests: "tell me more", "explain more", "give me another tip"
- Tracks current topic context
- Cycles through responses without repetition

 7. Memory and Recall
- Stores user's name and recalls it later
- Remembers user's interests in cybersecurity topics
- Personalises responses based on stored memory
- Can answer: "What's my name?" and "What am I interested in?"

 8. Sentiment Detection
Detects and adjusts responses for:
- **Worried** - Provides comfort + immediate actionable tip
- **Curious** - Encouraging, detailed response
- **Frustrated** - Simplified, supportive response
- **Happy** - Positive reinforcement
- **Urgent** - Immediate help + escalation advice

 9. Error Handling
- Graceful handling of unknown inputs
- Default response: "I'm not sure I understand. Can you try rephrasing?"
- No crashes on unexpected input
- Voice synthesis fails silently if no voices installed

 10. Code Optimisation
- Full Object-Oriented Programming structure
- Separate classes for Models, Services, and Data
- Methods and classes with good organisation practices
- Ready for Part 3 expansion

 Technologies Used

- **.NET 8.0** - Framework
- **WPF** - Windows Presentation Foundation for GUI
- **C#** - Programming language
- **System.Speech** - Text-to-speech functionality
- **XAML** - GUI layout and styling

 Project Structure

```
CybersecurityChatbot/
├── CybersecurityChatbot.csproj    # Project configuration
├── MainWindow.xaml                 # GUI layout (XAML)
├── MainWindow.xaml.cs              # Main application logic
├── README.md                       # This file
├── voice_greeting.wav              # Voice greeting audio
└── ascii_art.png                   # ASCII art image
```

 How to Run

 Prerequisites
- Windows OS
- .NET 8.0 SDK or later
- Visual Studio 2022 or Visual Studio Code

 Steps
1. Clone or download the repository
2. Open `CybersecurityChatbot.csproj` in Visual Studio
3. Build the solution: `Ctrl+Shift+B`
4. Run: `F5`

 Sample Conversation

```
Bot: Welcome to the Cybersecurity Awareness Bot! What is your name?
User: Thabo
Bot: Nice to meet you, Thabo! I am your Cybersecurity Awareness Assistant.
      You can ask me about password safety, phishing, malware, safe browsing,
      or social engineering. What would you like to know?

User: I'm worried about online scams
Bot: I understand your concern. PHISHING AWARENESS: Check sender email
      addresses carefully, scammers use slight misspellings. Hover over links
      before clicking to see the actual URL. Remember, staying informed is
      your best defense. You have got this!

User: Tell me more
Bot: SPOTTING PHISHING EMAILS: Look for poor grammar and spelling mistakes.
      Generic greetings like Dear Customer instead of your name...

User: What's my name?
Bot: Your name is Thabo, of course! I remember.

User: bye
Bot: Stay safe online, Thabo! Remember: think before you click!
```

 Author

- **Student:** [Your Name]
- **Course:** PROG6221
- **Institution:** The Independent Institute of Education
- **Year:** 2026

 License

This project is for educational purposes only.
'''

base_dir = "/mnt/agents/output/CybersecurityChatbotWPF_Fixed"
with open(f"{base_dir}/README.md", "w") as f:
    f.write(readme_content)

    https://youtu.be/dAJxp-_0f_o
    YOUTUBE VIDEO!!

