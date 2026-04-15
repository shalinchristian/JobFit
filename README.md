# 📄 Resume Matching System

![Build Status](https://img.shields.io/badge/build-passing-brightgreen)
![Python Version](https://img.shields.io/badge/python-3.8%2B-blue)
![License](https://img.shields.io/badge/license-MIT-green)

---

## 🚀 Overview
The **Resume Matching System** is an intelligent application designed to automate the process of matching resumes with job descriptions. It uses **Natural Language Processing (NLP)** and machine learning to analyze and score resumes against job requirements.

This tool helps recruiters **save time**, **reduce manual screening effort**, and **improve hiring accuracy**.

---

## ✨ Features

- 🔍 **Keyword Extraction**  
  Automatically identifies key skills, qualifications, and requirements from resumes and job descriptions.

- 📊 **Resume Ranking System**  
  Scores and ranks resumes based on similarity and relevance.

- 📈 **Matching Visualization**  
  Provides clear visual insights into how well a resume matches the job description.

- 💡 **Improvement Suggestions**  
  Offers actionable recommendations to enhance resume quality.

---

## 🛠️ Tech Stack

| Category     | Technologies Used              |
|--------------|------------------------------|
| **Backend**  | Python, Flask                |
| **ML**       | Scikit-learn                 |
| **NLP**      | NLTK                         |
| **Frontend** | HTML, CSS, JavaScript        |
| **Deployment** | Docker                    |

---

## 📸 Screenshots

### 🏠 Homepage
![Homepage](images/homepage.png)

### 📊 Matching Result
![Matching Result](images/match_click.png)

---

## ⚙️ Installation & Setup

### 🚀 Quick Start with Docker (Recommended)

1. **Clone the repository**  
   ```bash
   git clone https://github.com/shalinchristian/ResumeMatchingSystem.git
   cd ResumeMatchingSystem
   ```

2. **Start with Docker Compose**  
   ```bash
   docker-compose up --build
   ```

3. **Open in browser**  
   ```
   http://localhost:5000
   ```

That's it! 🎉 Both the API and Python service will start automatically.

---

## 🧠 How It Works

1. **Upload** a resume (PDF/DOCX)  
2. **Paste** the job description  
3. **Click Match**

### 🔄 Processing Steps:
- Text preprocessing  
- Feature extraction  
- Similarity scoring  

### 📊 Output:
- Match score  
- Ranking  
- Suggestions  

---

## 📝 Local Development (Advanced)

If you prefer to run without Docker:

1. Install Python 3.8+ and required dependencies
2. Create a virtual environment: `python -m venv venv`
3. Activate it and install requirements: `pip install -r requirements.txt`
4. Run services separately on different ports

For detailed setup, check individual service READMEs.

---

## 🤝 Contribution

Contributions are welcome!

1. Fork the repository  
2. Create a new branch  
3. Commit your changes  
4. Submit a Pull Request  

> For major changes, please open an issue first.

---

## 📜 License

This project is licensed under the MIT License.  
See the `LICENSE` file for details.

---

## 📬 Contact

**Shalin Christian**  
📧 shalinchristian71@gmail.com