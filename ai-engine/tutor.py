import sys
import argparse
import time
import random

# Simulated per-topic quiz banks
QUESTIONS = {
    "Physics": [
        "What is the formula for calculating kinetic energy? Provide your answer and the SI units.",
        "Explain Newton's First Law of Motion with an everyday example.",
        "If a car accelerates from 0 to 60 mph in 5 seconds, what is its average acceleration?",
        "What is the difference between speed and velocity?",
        "Describe the relationship between mass, gravity, and weight."
    ],
    "Chemistry": [
        "Explain the difference between covalent and ionic bonds.",
        "What is the pH scale and what values indicate an acid?",
        "Describe the process of a titration and why it is useful.",
        "What are the products of an acid-base neutralization reaction?",
        "Define an isotope and give an example."
    ],
    "Computer Science": [
        "What is the difference between a stack and a queue data structure?",
        "Explain the concept of Big O notation and why it is used.",
        "Write a simple Python function to reverse a string.",
        "What is object-oriented programming? Name two of its core principles.",
        "Describe the difference between an interpreter and a compiler."
    ],
    "Machine Learning": [
        "What is the difference between supervised and unsupervised learning?",
        "Explain the concept of overfitting in a machine learning model.",
        "What is a neural network and what are its basic components?",
        "Describe the purpose of a loss function.",
        "What is cross-validation and why is it used?"
    ],
    "Microbiology": [
        "What is the primary difference between prokaryotic and eukaryotic cells?",
        "Describe the steps of the Gram stain procedure and what it indicates.",
        "What are the three main shapes of bacteria?",
        "Explain the replication cycle of a typical virus.",
        "What is the role of plasmids in bacterial antibiotic resistance?"
    ],
    "default": [
        "Explain a core concept from this subject and why it is important.",
        "How would you apply a key principle of this field to a real-world problem?",
        "Describe a major historical discovery in this field.",
        "What is a common misconception about this subject?",
        "Summarize the main topics covered in the first chapter of a typical textbook for this subject."
    ]
}


def generate_quiz(subject):
    """Generates a randomized quiz question for the given subject."""
    
    # Try to find a matching bank
    matched_bank = QUESTIONS["default"]
    for key in QUESTIONS.keys():
        if key.lower() in subject.lower():
            matched_bank = QUESTIONS[key]
            break
            
    # Simulate API latency (anti-cheat)
    time.sleep(1.2)
    
    # Pick a random question
    return random.choice(matched_bank)

def generate_ai_response(subject, query):
    """Simulates an AI tutor response with RAG context."""
    time.sleep(1.0) # simulate Thinking
    
    # Catch basic greetings
    clean_query = query.lower().strip()
    if clean_query in ["hello", "hi", "hey"]:
        return f"Hello! I am your AI Teaching Assistant for {subject}. What would you like to review today?"
    
    if "help" in clean_query:
        return f"I can help you understand topics related to {subject}, summarize concepts, or give you practice problems. Just ask!"
        
    # Generic smart fallback
    return f"Based on the context of '{subject}', regarding your question '{query}': Yes, that's an excellent point to consider. In advanced studies, we often see that this relates deeply to the core foundational principles we've discussed. Keep thinking critically like an engineer!"

def main():
    parser = argparse.ArgumentParser(description="STEMd AI Engine")
    parser.add_argument("--mode", required=True, choices=["tutor", "quiz"], help="Mode of operation")
    parser.add_argument("--subject", required=True, help="Subject context for the AI")
    parser.add_argument("--query", help="User question (if mode=tutor)")
    
    args = parser.parse_args()
    
    if args.mode == "quiz":
        result = generate_quiz(args.subject)
        print(result)
        
    elif args.mode == "tutor":
        # we expect a query
        if not args.query:
            print("Error: --query is required when mode=tutor")
            sys.exit(1)
        result = generate_ai_response(args.subject, args.query)
        print(result)

if __name__ == "__main__":
    main()
