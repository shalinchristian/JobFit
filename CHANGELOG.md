# Changelog

## 2.4.0: QA round 5 fixes

1. **Saved jobs never show a stale label.** `JobTrackerService` now recomputes the label (`ScoreBands.Label`) and the rounded score whenever a job is read, not only when it's saved. A 2.2 database row stored as "72 · Good Match" now reads as "72 · Excellent Match".
2. **The score now counts skill coverage.** The new `ScoreBlend` computes `0.5 × semantic + 0.5 × 100 × √(matched catalog skills ÷ posting catalog skills)`. The API returns `semanticScore` and `skillCoverage` alongside `matchScore`.
   - Bands were re-cut for the blend: Weak now starts at 20 instead of 25. Excellent stays at 72, Good at 55 and Moderate at 40.
   - Real-model results:

     | Pair | Before | After |
     |---|---|---|
     | Strong backend | 81 | 90 |
     | Medium backend | 65 | 58 |
     | Weak backend | 28 Weak | 24 Weak |
     | React | 67 | 68 |
     | Weak resume, React posting | 32 | 25 Weak |
     | Data | 59 | 59 |
     | Strong backend resume, data posting | 45 | 33 Weak |
     | SRE good | 61 | 71 Good |
     | SRE bad | 14 | 7 |
     | **Android near-complete** | 63 Good | **73 Excellent** |
     | Android bad | 34 | 27 Weak |
   - **Quick score recalibrated.** The fallback now replaces only the semantic half (`20 + 65 × √coverage`, least-squares refit against the model) and is blended the same way. It lands within 5 points of the model on all 11 pairs, 2.9 on average.
3. **Catalog changes.**
   - C++ was added (aliases: cpp), so "Written in C++" matches. C, C# and C++ stay separate.
   - "Ruby on Rails" now names both Ruby and Rails, and Rails on a resume implies Ruby.
   - A pre-existing bug is fixed: "Spring Boot" in a posting was read as bare Spring and now stays Spring Boot.
- **Fewer false positives for ambiguous skills:**
  - Context is only searched within the same sentence, so "Series C startup. Python…" no longer gives C.
  - A name joined to a hyphen doesn't count ("Rust-free").
  - Swift, Rust, Dart, Hilt and Dagger at the start of a sentence need tech context in that sentence, so "Swift turnaround" doesn't count.
  - Phrases like "react quickly", "react to" and "Spring boot camp" are not skills.
  - Ruby is matched case-sensitively and not as "Ruby on our team" or "Ruby is…".
- The related-skill link Python → Go was removed.
- Tests: 198 → 230 xUnit tests. pytest: 6.

## 2.3.0: QA round 4 fixes

1. **Labels are always computed on the server.** `JobTrackerService.SaveAsync` ignores any posted label. It rounds the score with `ScoreBands.Round` and derives the label from that rounded score, so 71.5 posted with "Good Match" is saved as 72, "Excellent Match".
2. **Skill names that are also ordinary words.** The new `AmbiguousSkills` matches them case-sensitively on the original text, with context rules:
   - **Go / Golang:** never "to go", "go live", "go-to" or "Go through".
   - **R:** a standalone capital R only; not "R&D" or initials like "John R. Smith".
   - **C:** only near a language or programming word; not "C-level", "Series C", "C#" or "C++".
   - **Swift, Rust, Dart, Hilt, Dagger:** capitalised only.
   - **Express:** "Express.js" always; bare "Express" only near "Node".
   - **Spring:** "Spring Framework/MVC/..." always; bare "Spring" only near Java, Kotlin or Hibernate.
   - **Excel:** capitalised and not "Excel at/in".
   - **Flow:** "Kotlin Flow", "StateFlow" or "SharedFlow", or bare "Flow" near Kotlin or coroutines.
   - **Room:** only near Android, Kotlin or database words.
   - Each has true- and false-positive tests.
3. **Implied skills.** On the resume side, specific tools now count toward their umbrella skill:
   - Jenkins, GitHub Actions, GitLab CI, CircleCI, Azure DevOps and Argo CD → CI/CD
   - ASP.NET, ASP.NET Core and EF Core → .NET
   - EKS, AKS and GKE → Kubernetes plus their cloud
   - Helm → Kubernetes; Spring Boot → Spring; Next.js → React
   - Jetpack Compose → Android and Jetpack; xUnit, JUnit, Jest and similar → Testing
   - React Native does **not** imply React, and an umbrella skill never implies a specific tool.
4. **Android catalog.** Added Android, Android Jetpack, Jetpack Compose, XML views, MVVM, Coroutines, Kotlin Flow, Room, Retrofit, Hilt, Dagger, JUnit, Mockito, Espresso, Firebase/Crashlytics, Gradle, Google Play, Kotlin Multiplatform, iOS, Flutter, Dart and C. Related pairs include Java→Kotlin, XML views→Compose, Hilt↔Dagger and Coroutines→Flow. QA's Android resume now gets 18 matched chips and scores 63 Good.
- **SRE catalog:** EKS/AKS/GKE, PagerDuty, SLOs/SLIs (error budgets), incident response (postmortems, RCA), on-call, Istio (service mesh), ELK, load balancing, networking (TCP/IP, DNS), chaos engineering, SRE and CircleCI.
- **Advice fits the platform:** in mobile postings, the Accessibility, Web performance and Testing tips use Android tools such as Accessibility Scanner and Profiler instead of Lighthouse.
- **Phrases that aren't skills:** "for networking" and "networking events" no longer match Networking.
- **One 413 message:** body-size, field-length and Kestrel 413s all say "Request is too large. API bodies can be up to 10 MB and each text field up to 1,000,000 characters." Field-length errors also name the field.
- Tests: 116 → 198 xUnit tests. pytest: 6.

## 2.2.0: QA round 3 fixes

1. **Only catalog skills.** Chips, gap-plan lines and interview questions now come only from `SkillCatalog`. Words outside the catalog are no longer shown, though they still count toward the fallback score. "Reword" is used only for curated soft skills.
   - **Bigger catalog:** about 100 → about 130 skills. Frontend adds Redux, React Query, React Native, React Testing Library, Storybook, Webpack, Vite, **Sass/SCSS** (now separate from CSS), responsive design, web performance/Core Web Vitals, design systems, Vercel and Netlify. Data adds BigQuery, Snowflake, Redshift, dbt, Airflow, ETL, Looker, R, data visualization and forecasting. Cloud/DevOps adds Helm, Argo CD, Ansible, serverless, Nginx, Bash and PowerShell, and there are more backend frameworks. Soft skills add presenting and problem solving.
   - **Related-skill map:** more curated pairs, e.g. SQL → Snowflake/BigQuery, Tableau ↔ Power BI ↔ Looker, Jest → React Testing Library, Redux ↔ React Query.
   - "Senior leadership" and "leadership team" are no longer read as the Leadership skill.
2. **Interview questions.** "The posting also mentions X" skips every matched skill. Soft skills get behavioral questions ("Tell me about a situation where communication really mattered…") and never "a project where you used Communication".
3. **Round once.** `ScoreBands.Round` (whole number, half away from zero) is applied once in `MatchService` and the tracker. Label, band, colour, the result ring and `Tracker/Index` all use the rounded value. Edge cases are tested (71.6/71.5/71.4, 54.5/54.4, 39.5, 24.5/24.4, below 0, above 100).
4. **Empty file.** An empty uploaded file now gets `"<file>" is empty…` instead of the generic missing-fields message.
5. **Smaller fixes.**
   - **Wording:** the Good-band detailed summary says "good overall fit".
   - **API limits:** bodies over 10 MB get a JSON 413, checked from Content-Length before reading and enforced for chunked bodies too. Any string field over 1,000,000 characters also gets a 413.
   - **Fallback score:** recalibrated to `15 + 70·sqrt(0.8·skills + 0.2·words)`. On QA's set it gives strong 81, medium 51, weak 27, compared with the model's 81, 65 and 28.
   - **NLP requirements:** these are now version ranges, and a fresh **Python 3.13** venv installs and passes pytest. The Dockerfile installs **CPU-only torch** from `download.pytorch.org/whl/cpu` first.
- Tests: 91 → 116 xUnit tests. pytest: 6.

## 2.1.0: QA round 2 fixes

1. **One set of score cut-offs.** `ScoreBands` in Core (72 / 55 / 40 / 25) now drives the label, the applicant summary, the recruiter recommendation and summary, and the UI tones. They're tuned to MiniLM's real range (QA's strong, medium and weak resumes scored about 77, 62 and 28, which now land as Excellent, Good and Weak).
2. **Large uploads.** The browser checks file size and type before submitting. The server limit is raised to 64 MB for the whole request, with a 10 MB per-file check that returns "… is too large. Files can be up to 10 MB". Anything over 64 MB redirects back to the form with the same message (JSON 413 on `/api`), so the connection is no longer reset.
3. **Junk keywords.**
   - **Filler:** stop words now include job-ad filler, resume headers, months and common cities.
   - **Whole skills:** catalog-aware phrase matching runs before splitting into words, with about 100 skills and aliases, so *ASP.NET Core 8*, *EF Core*, *Node.js*, *CI-CD* and *GitHub Actions* stay whole.
   - **Plurals and tenses:** a small stemmer handles them (apis → api, mentored → mentoring).
   - **Company, title and city:** these words are filtered in the **service layer**, so the API gets the same results (`MatchTextAsync(resume, job, ignoreTerms)`; `/api/match/text` accepts `company` and `title`).
   - **Caps:** at most 6 non-catalog words per list, and at most 2 generic "Reword" lines in the gap plan.
4. **Importance order.** Matched and missing lists are ordered with catalog skills first, then by frequency in the posting, then by first position, instead of A–Z.
5. **Curated related skills.** `SkillCatalog.AreRelated` uses an explicit map instead of "same category". Resume-only skills (`MatchResponseDto.ResumeSkills`, which is also stored per job) count too. For example, ASP.NET MVC on the resume means "Show a project" for ASP.NET Core. Anything else becomes Learn.
6. **Interview questions** come only from catalog skills.
7. **Smaller fixes.**
   - **Plurals:** labels read "1 skill missing", "1 job checked".
   - **File types:** the upload hint, the `accept` attribute and the server's error all come from one list (PDF, DOCX, TXT or MD). .csv, .log and .json are no longer accepted.
   - **NLP Docker image:** it runs gunicorn, uses an `HF_HOME=/models` cache volume, and has an optional `PRELOAD_MODEL` build arg.
   - **Compose:** there's a healthcheck on the Python service, and the app uses `depends_on: condition: service_healthy`.
- Tests: 46 → 91 xUnit tests.
- Breaking: the `Jobs` table gained a `ResumeSkills` column. A `jobfit.db` created by 2.0.0 must be deleted, because the schema uses `EnsureCreated` and there are no migrations.

## 2.0.0: ResumeMatchingSystem becomes JobFit

ResumeMatchingSystem was turned into **JobFit**, a simple tool for job seekers. The project was flipped from "rank a candidate for a recruiter" to "am I ready to apply?", so it doesn't overlap with Resqora, the resume builder. The tested matching core was reused, not rewritten.

### Renamed
- The solution and projects are now `JobFit.sln`, `JobFit.Api`, `JobFit.Core`, `JobFit.Infrastructure` and `JobFit.Tests`. The namespaces moved from `ResumeAPI.*` to `JobFit.*`.
- Branding, the Swagger title, the `/health` service name, Docker Compose services (`jobfit`, `nlp`) and the Postman collection were all renamed.

### Added
- **Gap plan** (`GapPlanBuilder`, `SkillCatalog`): one deterministic action (Learn, Show a project or Reword) for each of the top 8 missing skills.
- **Tracker**: the `JobApplication` entity with status Saved/Applied/Interview/Offer/Rejected, plus `JobRepository` and `JobTrackerService`. It's stored in **EF Core SQLite** (`jobfit.db`, created on startup with `EnsureCreated`), and InMemory is still available for tests.
- **API**: `GET/POST /api/jobs`, `GET/DELETE /api/jobs/{id}`, `PATCH|PUT /api/jobs/{id}/status`, `POST /api/match/text`. Enums are serialized as strings.
- **Python fallback** (`FallbackScorer`): if the similarity service is unreachable, times out or returns nothing, `MatchService` uses a weighted keyword-overlap score and sets `usedFallback = true`, and the UI shows a "Quick score" notice.
- `MatchInsightsBuilder.BuildApplicantSummary`, a verdict written for the job seeker.
- A new frontend in plain CSS/JS with no build step and no CDN: the Check page, Result page (score ring, skill chips, gap plan, interview prep, Save to tracker), Tracker list (status counts, inline status change, delete) and Job detail. It's responsive.
- `DemoSeeder` (`--Demo:Seed=true`) with 4 fictional jobs, and sample files in `docs/sample/`.
- Tests went from 14 to **46** xUnit tests. New ones cover the gap plan, applicant summary, fallback scorer, fallback paths in MatchService, tracker service and repository, keyword filler and short skills, and WebApplicationFactory API tests.
- README screenshots in `docs/screenshots/`.

### Changed
- `MatchService`: when Python is down it **no longer throws `InvalidOperationException` / returns 503**. It falls back instead. The old test `MatchAsync_PythonServiceDown_ThrowsInvalidOperation` was replaced with `MatchAsync_PythonServiceDown_FallsBackToKeywordScore`. Scores are clamped to 0–100. There's a new `MatchTextAsync(resumeText, jobDescription)` that works without storing the resume.
- `KeywordExtractor`: it ignores common job-posting filler ("experience", "team", "looking", ...), keeps short known skills such as `C#`, splits on more punctuation, and lists known skills first.
- The web UI no longer stores uploaded resumes. Only saved jobs are persisted.
- The Dockerfile publishes `JobFit.Api.dll` and keeps the SQLite DB in `/data` (a compose volume).
- `UseHttpsRedirection` and the catch-all fallback route were removed. The fallback route was hiding API 404s.

### Removed
- The old single-page Bootstrap view `Views/Home/Index.cshtml` (recruiter "hiring recommendation" UI), `ResumeMatchPageViewModel`, `docs/images/` and `MIGRATION.md` (replaced by this file).

## 1.1.0: Restructure (ResumeMatchingSystem)
- Split into Api / Core / Infrastructure. Python app split into config/similarity/routes/app. 14 xUnit and 6 pytest tests. Docker Compose builds from the repo root.
