# Resume Job Matching System

A clean-architecture ASP.NET Core Web API that matches resumes to job descriptions using **free, local semantic similarity** — no OpenAI, no paid APIs.

```
┌─────────────────────────────┐         ┌──────────────────────────────────┐
│   ASP.NET Core Web API      │  HTTP   │    Python Flask Microservice      │
│   (.NET 8, port 5000)       │ ──────► │    (sentence-transformers, 5001)  │
│                             │         │                                   │
│  POST /api/resume/upload    │         │  POST /similarity                 │
│  POST /api/match            │         │  • Encodes texts with MiniLM      │
│                             │         │  • Returns cosine similarity 0–100│
│  In-memory EF Core DB       │         └──────────────────────────────────┘
└─────────────────────────────┘
```

---

## Project Structure

```
ResumeMatchingSystem/
│
├── ResumeAPI/                        # ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── ResumeController.cs       # POST /api/resume/upload, GET /api/resume/{id}
│   │   └── MatchController.cs        # POST /api/match
│   │
│   ├── Services/
│   │   ├── ResumeService.cs          # Upload & retrieval business logic
│   │   ├── MatchService.cs           # Orchestrates match pipeline
│   │   ├── SimilarityClient.cs       # HttpClient wrapper for Python service
│   │   └── KeywordExtractor.cs       # Local keyword comparison (no dependencies)
│   │
│   ├── Repositories/
│   │   └── ResumeRepository.cs       # IResumeRepository + EF Core implementation
│   │
│   ├── Models/
│   │   ├── Resume.cs                 # Domain entity
│   │
│   ├── DTOs/
│   │   └── ResumeDtos.cs             # Request/response data transfer objects
│   │
│   ├── Data/
│   │   └── AppDbContext.cs           # EF Core in-memory context
│   │
│   ├── Program.cs                    # DI setup, middleware pipeline
│   ├── appsettings.json              # Config (Python service URL)
│   └── ResumeAPI.csproj
│
├── PythonService/                    # Python Flask microservice
│   ├── app.py                        # Similarity endpoint using sentence-transformers
│   └── requirements.txt
│
├── ResumeAPI.postman_collection.json # Ready-to-import Postman requests
└── README.md
```

---

## Prerequisites

| Tool | Required Version |
|------|-----------------|
| .NET SDK | 8.0+ |
| Python | 3.9+ |
| pip | Any recent version |

---

## Setup & Run

### Step 1 — Start the Python Microservice

```bash
cd PythonService

# Create a virtual environment (recommended)
python -m venv venv
source venv/bin/activate        # Linux/macOS
venv\Scripts\activate           # Windows

# Install dependencies
pip install -r requirements.txt

# Start the Flask server
python app.py
```

> **First run:** The `all-MiniLM-L6-v2` model (~80MB) will download automatically.

Expected output:
```
Loading sentence-transformer model...
Model loaded successfully.
 * Running on http://0.0.0.0:5001
```

---

### Step 2 — Start the ASP.NET Core API

Open a new terminal:

```bash
cd ResumeAPI

# Restore packages
dotnet restore

# Run the API
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

Swagger UI is available at: **http://localhost:5000**

---

## API Reference

### 1. Upload Resume

```
POST http://localhost:5000/api/resume/upload
Content-Type: application/json
```

**Request:**
```json
{
  "candidateName": "Jane Doe",
  "content": "Experienced software engineer with 5 years in C#, ASP.NET Core, REST APIs, PostgreSQL, Docker, Azure, and Agile methodologies."
}
```

**Response (201 Created):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "candidateName": "Jane Doe",
  "uploadedAt": "2024-01-15T10:30:00Z",
  "message": "Resume uploaded successfully."
}
```

---

### 2. Match Resume to Job

```
POST http://localhost:5000/api/match
Content-Type: application/json
```

**Request:**
```json
{
  "resumeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "jobDescription": "We seek a Senior Backend Engineer with ASP.NET Core, C#, PostgreSQL, Docker, and CI/CD experience working in an Agile team."
}
```

**Response (200 OK):**
```json
{
  "matchScore": 76.42,
  "matchedKeywords": ["agile", "azure", "c#", "docker", "postgresql", "rest"],
  "missingKeywords": ["ci/cd", "senior"],
  "matchLabel": "Good Match"
}
```

| Score Range | Label           |
|-------------|-----------------|
| 80–100      | Excellent Match |
| 60–79       | Good Match      |
| 40–59       | Moderate Match  |
| 20–39       | Weak Match      |
| 0–19        | Poor Match      |

---

### 3. Python Service (Direct)

```
POST http://localhost:5001/similarity
Content-Type: application/json

{
  "text1": "Software engineer experienced in Python and machine learning.",
  "text2": "Looking for a Python developer with ML expertise."
}
```

**Response:**
```json
{ "score": 84.71 }
```

---

## Using Postman

1. Open Postman → Import → select `ResumeAPI.postman_collection.json`
2. Run **"1. Upload Resume"** → copy the `id` from the response
3. Paste the `id` into **"2. Match Resume to Job"** → run it

---

## Switch to PostgreSQL

To use a real database instead of in-memory:

1. Add the Npgsql package:
   ```bash
   dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
   ```

2. In `Program.cs`, replace:
   ```csharp
   opt.UseInMemoryDatabase("ResumeDb")
   ```
   with:
   ```csharp
   opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
   ```

3. Add the connection string to `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "Default": "Host=localhost;Port=5432;Database=resumedb;Username=postgres;Password=yourpassword"
   }
   ```

4. Run migrations:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

---

## Architecture Decisions

| Decision | Rationale |
|----------|-----------|
| In-memory EF Core | No DB setup needed; swap to PostgreSQL with 2 lines |
| IHttpClientFactory | Efficient HTTP connection pooling; easy to mock in tests |
| Separate Python service | sentence-transformers runs better in Python; clean separation |
| Repository pattern | Decouples data access from business logic |
| Static KeywordExtractor | No external NLP needed; fast, readable, dependency-free |
