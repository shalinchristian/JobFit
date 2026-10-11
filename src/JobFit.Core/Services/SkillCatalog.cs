namespace JobFit.Services;

/// <summary>
/// Built-in catalog of common job skills. Keys are canonical lowercase names ("asp.net core", "c#", "ci/cd").
/// Aliases let the tokenizer recognise multi-word and punctuated skills before splitting text into words,
/// so "ASP.NET Core 8", "Node.js" or "CI-CD" are never broken into fragments.
/// Related skills are an explicit, curated map (not "same category") and drive the gap plan's "Show a project".
/// </summary>
public static class SkillCatalog
{
    public record Skill(string Key, string Display, string Category, string LearnTip);

    private static readonly Skill[] All =
    {
        // Languages
        new("c#", "C#", "language", "work through the official C# tour and rebuild one small tool in it"),
        new("java", "Java", "language", "build a small Spring Boot REST API end to end"),
        new("python", "Python", "language", "write a small CLI or Flask app and add tests with pytest"),
        new("javascript", "JavaScript", "language", "build a small interactive page with plain JavaScript (DOM, fetch, events)"),
        new("typescript", "TypeScript", "language", "convert a small JavaScript project to TypeScript with strict mode on"),
        new("golang", "Go", "language", "do the Tour of Go and write a small HTTP service"),
        new("rust", "Rust", "language", "read the first half of The Rust Book and write a small CLI"),
        new("kotlin", "Kotlin", "language", "build a small Android or Ktor app in Kotlin"),
        new("swift", "Swift", "language", "build a small SwiftUI app"),
        new("php", "PHP", "language", "build a small CRUD app with Laravel"),
        new("ruby", "Ruby", "language", "build a small Rails app"),
        new("sql", "SQL", "database", "practice joins, GROUP BY and window functions on a sample dataset"),
        // Frameworks
        new(".net", ".NET", "backend", "build a small ASP.NET Core Web API with EF Core"),
        new("asp.net", "ASP.NET", "backend", "build a small ASP.NET Core MVC or Web API project"),
        new("spring", "Spring", "backend", "build a Spring Boot API with one database table and tests"),
        new("django", "Django", "backend", "follow the official Django tutorial and deploy the result"),
        new("flask", "Flask", "backend", "build a small Flask API with two or three endpoints"),
        new("fastapi", "FastAPI", "backend", "build a small FastAPI service with Pydantic models"),
        new("node.js", "Node.js", "backend", "build a small Express or Fastify API"),
        new("express", "Express", "backend", "build a small Express REST API with validation"),
        new("graphql", "GraphQL", "backend", "add a GraphQL endpoint to a small existing API"),
        new("rest api", "REST APIs", "backend", "design a small REST API with proper status codes and docs"),
        new("microservices", "Microservices", "backend", "split a small app into two services that talk over HTTP"),
        new("react", "React", "frontend", "build a small React app with state, effects and one API call"),
        new("angular", "Angular", "frontend", "do the official Angular tutorial (Tour of Heroes)"),
        new("vue", "Vue", "frontend", "build a small Vue 3 app with the Composition API"),
        new("next.js", "Next.js", "frontend", "build a small Next.js site with one dynamic route"),
        new("html", "HTML", "frontend", "build an accessible, semantic one-page site"),
        new("css", "CSS", "frontend", "rebuild a page layout with Flexbox and Grid, responsive down to mobile"),
        new("tailwind", "Tailwind CSS", "frontend", "restyle a small page with Tailwind utility classes"),
        new("figma", "Figma", "design", "recreate one existing app screen in Figma with components"),
        // Cloud / DevOps
        new("aws", "AWS", "cloud", "deploy a small app on AWS (EC2 or Lambda + S3) using the free tier"),
        new("azure", "Azure", "cloud", "deploy a small app to Azure App Service and take the AZ-900 learning path"),
        new("gcp", "Google Cloud", "cloud", "deploy a small container to Cloud Run"),
        new("docker", "Docker", "devops", "containerize one of your projects and run it with docker compose"),
        new("kubernetes", "Kubernetes", "devops", "run a small app on a local cluster (kind or minikube) with a Deployment and Service"),
        new("terraform", "Terraform", "devops", "write Terraform for one small cloud resource and apply it"),
        new("jenkins", "Jenkins", "devops", "set up a Jenkins pipeline that builds and tests a project"),
        new("ci/cd", "CI/CD", "devops", "add a GitHub Actions workflow that builds and tests one of your repos"),
        new("github", "GitHub", "devops", "keep a clean public repo with a README, issues and pull requests"),
        new("git", "Git", "devops", "practice branching, rebasing and resolving merge conflicts"),
        new("linux", "Linux", "devops", "do daily work in a Linux shell for a week; learn processes, permissions and systemd"),
        // Data
        new("postgresql", "PostgreSQL", "database", "move a small project's database to PostgreSQL and add an index"),
        new("mysql", "MySQL", "database", "design a small schema in MySQL and write a few reporting queries"),
        new("mongodb", "MongoDB", "database", "build a small app that stores documents in MongoDB"),
        new("redis", "Redis", "database", "add Redis caching to one slow endpoint of a project"),
        new("pandas", "pandas", "data", "clean and analyze a public dataset in a notebook"),
        new("numpy", "NumPy", "data", "do a short NumPy exercise set (arrays, broadcasting, vectorization)"),
        new("spark", "Apache Spark", "data", "process a public dataset with PySpark locally"),
        new("tableau", "Tableau", "data", "build and publish one dashboard on Tableau Public"),
        new("excel", "Excel", "data", "practice pivot tables and XLOOKUP on a sample dataset"),
        new("tensorflow", "TensorFlow", "ml", "train a small image or text classifier with Keras"),
        new("pytorch", "PyTorch", "ml", "train a small model with PyTorch and write up the results"),
        new("nlp", "NLP", "ml", "build a small text classifier or semantic search demo"),
        new("llms", "LLMs", "ml", "build a small app that calls an LLM API with prompt tests"),
        // Testing / practices
        new("testing", "Testing", "testing", "add unit tests to one of your projects and report coverage"),
        new("xunit", "xUnit", "testing", "add an xUnit test project to a .NET repo"),
        new("jest", "Jest", "testing", "add Jest tests to a JavaScript project"),
        new("selenium", "Selenium", "testing", "write a few browser tests for a small web app"),
        new("playwright", "Playwright", "testing", "write end-to-end tests for a small web app with Playwright"),
        new("agile", "Agile", "practice", "read the Scrum Guide and describe how your last team planned work"),
        new("scrum", "Scrum", "practice", "read the Scrum Guide and describe sprints you have worked in"),
        new("security", "Security", "practice", "work through the OWASP Top 10 and fix one issue in a project"),
        new("accessibility", "Accessibility", "practice", "audit one of your pages with Lighthouse and fix the issues"),
        new("seo", "SEO", "practice", "run a Lighthouse SEO audit on a site and fix the findings"),
        // Soft skills
        new("communication", "Communication", "soft", "add one bullet that shows you explaining or presenting work to others"),
        new("leadership", "Leadership", "soft", "add one bullet where you led a project, mentored someone or owned a decision"),
        new("mentoring", "Mentoring", "soft", "add one bullet about helping or onboarding a teammate"),
        new("stakeholders", "Stakeholder management", "soft", "add one bullet about working with non-engineering stakeholders"),
        // Added after QA
        new("asp.net core", "ASP.NET Core", "backend", "build a small ASP.NET Core Web API with EF Core and Swagger"),
        new("entity framework core", "EF Core", "backend", "add EF Core with migrations to a small ASP.NET Core API"),
        new("spring boot", "Spring Boot", "backend", "build a Spring Boot API with one database table and tests"),
        new("grpc", "gRPC", "backend", "add a small gRPC service next to one of your REST APIs"),
        new("rabbitmq", "RabbitMQ", "messaging", "publish and consume events between two small services with RabbitMQ"),
        new("kafka", "Kafka", "messaging", "run Kafka locally and stream events between two small services"),
        new("sql server", "SQL Server", "database", "move a small project's database to SQL Server and write a stored procedure"),
        new("github actions", "GitHub Actions", "devops", "add a GitHub Actions workflow that builds, tests and packages one of your repos"),
        new("gitlab", "GitLab CI", "devops", "add a GitLab CI pipeline to a small project"),
        new("opentelemetry", "OpenTelemetry", "observability", "add OpenTelemetry tracing to a small API and view the traces locally"),
        new("grafana", "Grafana", "observability", "build a Grafana dashboard for a small app's metrics"),
        new("prometheus", "Prometheus", "observability", "expose metrics from a small app and scrape them with Prometheus"),
        new("microsoft azure devops", "Azure DevOps", "devops", "set up an Azure DevOps pipeline for a small project"),
        new("power bi", "Power BI", "data", "build and publish one Power BI report from a public dataset"),
        new("cypress", "Cypress", "testing", "write end-to-end tests for a small web app with Cypress"),
        new("pytest", "pytest", "testing", "add a pytest suite to a small Python project"),
        new("nunit", "NUnit", "testing", "add an NUnit test project to a .NET repo"),
        new("jira", "Jira", "practice", "describe how you planned and tracked work in Jira or a similar tool"),
        new("photoshop", "Photoshop", "design", "redo one project's assets in Photoshop and add it to your portfolio"),
        new("illustrator", "Illustrator", "design", "create a small icon set in Illustrator for your portfolio"),
        new("machine learning", "Machine learning", "ml", "train and evaluate a small model on a public dataset"),
        new("statistics", "Statistics", "data", "work through descriptive statistics and hypothesis tests on a real dataset"),
        new("a/b testing", "A/B testing", "data", "analyze a public A/B test dataset and write up the result"),
        new("wcag", "WCAG", "practice", "audit a page against WCAG 2.2 AA and fix the issues"),
        new("svelte", "Svelte", "frontend", "build a small Svelte app"),
        new("oracle", "Oracle DB", "database", "practice PL/SQL on a free Oracle instance"),
        new("dynamodb", "DynamoDB", "database", "store and query a small dataset in DynamoDB"),
        new("bicep", "Bicep", "devops", "describe one Azure resource in Bicep and deploy it"),
        new("cloudformation", "CloudFormation", "devops", "describe one AWS resource in CloudFormation and deploy it"),
        // Added in 2.2.0 (QA round 3): broader frontend / data / cloud coverage
        new("redux", "Redux", "frontend", "add Redux Toolkit to a small React app for shared state"),
        new("react query", "React Query", "frontend", "fetch and cache API data in a React app with TanStack Query"),
        new("react native", "React Native", "mobile", "build a small two-screen React Native app with Expo"),
        new("react testing library", "React Testing Library", "testing", "test a few React components with React Testing Library"),
        new("storybook", "Storybook", "frontend", "document a small component library in Storybook"),
        new("webpack", "Webpack", "frontend", "set up a small project with Webpack from scratch (loaders, code splitting)"),
        new("vite", "Vite", "frontend", "migrate a small React app to Vite"),
        new("sass", "Sass/SCSS", "frontend", "restyle a page with Sass variables, mixins and partials"),
        new("responsive design", "Responsive design", "frontend", "make one page work from 320px to desktop with media queries and fluid layouts"),
        new("web performance", "Web performance", "frontend", "profile a page with Lighthouse and improve its Core Web Vitals"),
        new("design systems", "Design systems", "design", "build a small token-based component set and document it"),
        new("vercel", "Vercel", "cloud", "deploy a small Next.js app to Vercel with preview deployments"),
        new("netlify", "Netlify", "cloud", "deploy a static site to Netlify with a form and redirects"),
        new("nestjs", "NestJS", "backend", "build a small NestJS API with one module and tests"),
        new("laravel", "Laravel", "backend", "build a small CRUD app with Laravel"),
        new("rails", "Ruby on Rails", "backend", "build a small Rails app with one resource"),
        new("elasticsearch", "Elasticsearch", "database", "index a public dataset in Elasticsearch and add a search endpoint"),
        new("ansible", "Ansible", "devops", "write an Ansible playbook that sets up a small server"),
        new("helm", "Helm", "devops", "package a small app as a Helm chart and install it on a local cluster"),
        new("argo cd", "Argo CD", "devops", "deploy a small app to a local cluster with Argo CD (GitOps)"),
        new("serverless", "Serverless", "cloud", "build one small function on AWS Lambda or Azure Functions"),
        new("nginx", "Nginx", "devops", "put Nginx in front of a small app as a reverse proxy"),
        new("bash", "Bash", "devops", "automate one repetitive task with a Bash script"),
        new("powershell", "PowerShell", "devops", "automate one Windows or Azure task with PowerShell"),
        new("bigquery", "BigQuery", "data", "query a BigQuery public dataset and save a few analyses"),
        new("snowflake", "Snowflake", "data", "load a CSV into a Snowflake trial account and write a few queries"),
        new("redshift", "Redshift", "data", "load and query a small dataset in Redshift Serverless"),
        new("dbt", "dbt", "data", "build a small dbt project with a few models and tests on a sample warehouse"),
        new("airflow", "Airflow", "data", "write a small Airflow DAG that loads and transforms a dataset daily"),
        new("etl", "ETL", "data", "build a small extract-transform-load script from an API into a database"),
        new("looker", "Looker", "data", "build a small Looker Studio dashboard on a public dataset"),
        new("r", "R", "data", "do one analysis in R with the tidyverse and ggplot2"),
        new("data visualization", "Data visualization", "data", "redesign one chart-heavy report for clarity and add it to your portfolio"),
        new("forecasting", "Forecasting", "data", "build a simple demand forecast on a public sales dataset"),
        new("presentation", "Presenting", "soft", "add one bullet about presenting findings or demos to stakeholders or leadership"),
        new("problem solving", "Problem solving", "soft", "add one bullet that shows a hard problem you diagnosed and solved"),
        // Added in 2.3.0 (QA round 4): Android / mobile
        new("android", "Android", "mobile", "build and publish a small Android app with two screens and one API call"),
        new("jetpack", "Android Jetpack", "mobile", "use ViewModel, Navigation and Room together in a small Android app"),
        new("jetpack compose", "Jetpack Compose", "mobile", "rebuild one XML screen of an Android app in Jetpack Compose"),
        new("xml views", "XML views", "mobile", "build one Android screen with XML layouts and RecyclerView"),
        new("mvvm", "MVVM", "mobile", "refactor one screen to MVVM with a ViewModel and a repository"),
        new("coroutines", "Kotlin Coroutines", "mobile", "move one network call to coroutines with viewModelScope"),
        new("flow", "Kotlin Flow", "mobile", "expose screen state as a StateFlow and collect it in the UI"),
        new("room", "Room", "mobile", "cache API results locally with Room and a DAO"),
        new("retrofit", "Retrofit", "mobile", "call a public REST API from Android with Retrofit and Moshi/Kotlinx serialization"),
        new("hilt", "Hilt", "mobile", "add Hilt dependency injection to a small Android app"),
        new("dagger", "Dagger", "mobile", "wire a small app's dependencies with Dagger 2 components"),
        new("junit", "JUnit", "testing", "write JUnit tests for a few ViewModel or service classes"),
        new("mockito", "Mockito", "testing", "mock a repository with Mockito in a few unit tests"),
        new("espresso", "Espresso", "testing", "write two Espresso UI tests for an Android screen"),
        new("firebase", "Firebase", "mobile", "add Firebase Crashlytics and Analytics to a small app"),
        new("gradle", "Gradle", "devops", "split a small project into Gradle modules with a version catalog"),
        new("google play", "Google Play", "mobile", "publish a small app to a Google Play internal testing track"),
        new("kotlin multiplatform", "Kotlin Multiplatform", "mobile", "share one data layer between Android and iOS with Kotlin Multiplatform"),
        new("ios", "iOS", "mobile", "build a small SwiftUI app with one list and one detail screen"),
        new("flutter", "Flutter", "mobile", "build a small two-screen Flutter app"),
        new("dart", "Dart", "language", "do the Dart language tour and build a small CLI or Flutter app"),
        new("c", "C", "language", "write a small C program that parses a file, and run it under Valgrind"),
        new("c++", "C++", "language", "build a small CLI in modern C++ (C++17, CMake) and add unit tests"),
        // SRE / platform
        new("eks", "EKS", "cloud", "run a small app on an EKS cluster (eksctl) and tear it down"),
        new("aks", "AKS", "cloud", "run a small app on an AKS cluster and tear it down"),
        new("gke", "GKE", "cloud", "run a small app on a GKE Autopilot cluster"),
        new("circleci", "CircleCI", "devops", "add a CircleCI pipeline that builds and tests one repo"),
        new("sre", "SRE", "devops", "define SLOs for one of your services and alert on its error budget"),
        new("pagerduty", "PagerDuty", "observability", "route alerts from Prometheus/Grafana to a PagerDuty free account"),
        new("slos", "SLOs/SLIs", "observability", "define two SLIs and an SLO with an error budget for a small service"),
        new("incident response", "Incident response", "practice", "write a blameless postmortem for a real or simulated outage"),
        new("on-call", "On-call", "practice", "add one bullet about your on-call rotation and what you improved"),
        new("istio", "Istio", "devops", "add Istio to a local cluster and try traffic splitting"),
        new("elk", "ELK stack", "observability", "ship a small app's logs to Elasticsearch and explore them in Kibana"),
        new("load balancing", "Load balancing", "devops", "put two app instances behind Nginx or a cloud load balancer"),
        new("networking", "Networking", "devops", "debug one real DNS or TCP issue with dig, curl and tcpdump and write it up"),
        new("chaos engineering", "Chaos engineering", "devops", "kill pods on a test cluster with Chaos Mesh and fix what breaks"),
    };

    /// <summary>Extra spellings that map to a key (the key itself and its display name always match).</summary>
    private static readonly Dictionary<string, string[]> Aliases = new()
    {
        ["c#"] = new[] { "csharp", "c sharp" },
        ["c++"] = new[] { "cpp", "c plus plus" },
        [".net"] = new[] { "dotnet", ".net core", ".net framework", "net core" },
        ["asp.net core"] = new[] { "aspnet core", "aspnetcore" },
        ["asp.net"] = new[] { "aspnet", "asp.net mvc", "asp.net web api" },
        ["entity framework core"] = new[] { "entity framework", "ef core", "efcore" },
        ["node.js"] = new[] { "nodejs", "node" },
        ["next.js"] = new[] { "nextjs" },
        ["vue"] = new[] { "vue.js", "vuejs" },
        ["react"] = new[] { "react.js", "reactjs" },
        ["react native"] = new[] { "react-native" },
        ["redux"] = new[] { "redux toolkit" },
        ["react query"] = new[] { "tanstack query", "react-query" },
        ["react testing library"] = new[] { "testing library" },
        ["responsive design"] = new[] { "responsive", "responsive web design", "mobile-first" },
        ["web performance"] = new[] { "core web vitals", "performance optimization", "web vitals" },
        ["design systems"] = new[] { "design system" },
        ["argo cd"] = new[] { "argocd" },
        ["serverless"] = new[] { "aws lambda", "lambda", "azure functions" },
        ["bash"] = new[] { "shell scripting" },
        ["etl"] = new[] { "elt", "etl pipelines", "data pipelines" },
        ["looker"] = new[] { "looker studio" },
        ["data visualization"] = new[] { "data visualisation", "visualization", "visualisation" },
        ["forecasting"] = new[] { "forecast", "forecasts" },
        ["excel"] = new[] { "pivot tables", "microsoft excel", "ms excel" },
        ["presentation"] = new[] { "present findings", "presenting", "presentations" },
        ["problem solving"] = new[] { "problem-solving" },
        ["rails"] = new[] { "ruby on rails" },
        ["angular"] = new[] { "angularjs" },
        ["javascript"] = new[] { "js", "es6" },
        ["golang"] = new[] { "go lang" },
        ["rest api"] = new[] { "rest apis", "restful", "restful api", "restful apis" },
        ["ci/cd"] = new[] { "ci-cd", "cicd", "ci cd", "continuous integration", "continuous delivery", "continuous deployment" },
        ["github actions"] = new[] { "gh actions" },
        ["gitlab"] = new[] { "gitlab ci" },
        ["kubernetes"] = new[] { "k8s" },
        ["eks"] = new[] { "amazon eks" },
        ["aks"] = new[] { "azure kubernetes service" },
        ["gke"] = new[] { "google kubernetes engine" },
        ["jetpack compose"] = new[] { "compose ui" },
        ["xml views"] = new[] { "xml layouts", "android views" },
        ["mvvm"] = new[] { "model-view-viewmodel" },
        ["coroutines"] = new[] { "kotlin coroutines", "coroutine" },
        ["firebase"] = new[] { "crashlytics", "firebase crashlytics" },
        ["google play"] = new[] { "google play console", "play console", "play store", "google play store" },
        ["kotlin multiplatform"] = new[] { "kmp", "kotlin multi-platform" },
        ["sre"] = new[] { "site reliability engineering" },
        ["slos"] = new[] { "slo", "slis", "sli", "error budgets", "error budget", "service level objectives" },
        ["incident response"] = new[] { "incident management", "postmortems", "postmortem", "blameless postmortems", "root cause analysis" },
        ["on-call"] = new[] { "on call", "oncall", "on-call rotation" },
        ["istio"] = new[] { "service mesh" },
        ["elk"] = new[] { "elk stack", "kibana", "logstash" },
        ["load balancing"] = new[] { "load balancers", "load balancer" },
        ["networking"] = new[] { "tcp/ip", "dns" },
        ["circleci"] = new[] { "circle ci" },
        ["postgresql"] = new[] { "postgres", "psql" },
        ["sql server"] = new[] { "mssql", "ms sql", "t-sql", "tsql" },
        ["mongodb"] = new[] { "mongo" },
        ["gcp"] = new[] { "google cloud", "google cloud platform" },
        ["aws"] = new[] { "amazon web services" },
        ["azure"] = new[] { "microsoft azure" },
        ["microsoft azure devops"] = new[] { "azure devops" },
        ["tailwind"] = new[] { "tailwindcss", "tailwind css" },
        ["css"] = new[] { "css3" },
        ["sass"] = new[] { "scss", "sass/scss" },
        ["html"] = new[] { "html5" },
        ["llms"] = new[] { "llm", "large language models" },
        ["machine learning"] = new[] { "ml" },
        ["nlp"] = new[] { "natural language processing" },
        ["a/b testing"] = new[] { "a/b tests", "ab testing" },
        ["opentelemetry"] = new[] { "open telemetry", "otel" },
        ["power bi"] = new[] { "powerbi" },
        ["photoshop"] = new[] { "adobe photoshop" },
        ["illustrator"] = new[] { "adobe illustrator" },
        ["testing"] = new[] { "unit testing", "unit tests", "integration tests", "integration testing", "automated tests", "test automation" },
        ["stakeholders"] = new[] { "stakeholder", "stakeholder management" },
        ["mentoring"] = new[] { "mentor", "mentored", "mentorship", "coaching" },
        ["communication"] = new[] { "communication skills", "communicate" },
        ["leadership"] = new[] { "led", "team lead" },
        ["microservices"] = new[] { "microservice", "micro-services" },
        ["accessibility"] = new[] { "a11y", "accessible" },
        ["statistics"] = new[] { "statistical", "statistical analysis" },
        ["spark"] = new[] { "apache spark", "pyspark" },
    };

    /// <summary>
    /// Curated "if you know A you can credibly point at it for B" groups. Directed pairs are listed as
    /// (have -> missing). Groups are symmetric. Anything not listed falls back to "Learn".
    /// </summary>
    private static readonly string[][] RelatedGroups =
    {
        new[] { "postgresql", "mysql", "sql server", "oracle" },
        new[] { "react", "vue", "angular", "svelte" },
        new[] { "aws", "azure", "gcp" },
        new[] { "jenkins", "github actions", "gitlab", "ci/cd", "microsoft azure devops" },
        new[] { "terraform", "bicep", "cloudformation" },
        new[] { "rabbitmq", "kafka" },
        new[] { "xunit", "nunit", "jest", "pytest", "testing" },
        new[] { "playwright", "selenium", "cypress" },
        new[] { "django", "flask", "fastapi" },
        new[] { "node.js", "express" },
        new[] { "spring", "spring boot" },
        new[] { "asp.net core", "asp.net", ".net" },
        new[] { "javascript", "typescript" },
        new[] { "java", "kotlin" },
        new[] { "tensorflow", "pytorch" },
        new[] { "tableau", "power bi" },
        new[] { "grafana", "prometheus", "opentelemetry" },
        new[] { "scrum", "agile" },
        new[] { "mongodb", "dynamodb" },
        new[] { "photoshop", "illustrator", "figma" },
        new[] { "leadership", "mentoring" },
        new[] { "hilt", "dagger" },
        new[] { "eks", "aks", "gke" },
        new[] { "incident response", "on-call" },
        new[] { "elk", "elasticsearch" },
        new[] { "junit", "xunit", "nunit" },
        new[] { "flutter", "react native" },
        new[] { "redux", "react query" },
        new[] { "webpack", "vite" },
        new[] { "sass", "css", "tailwind" },
        new[] { "snowflake", "bigquery", "redshift" },
        new[] { "tableau", "power bi", "looker" },
        new[] { "python", "r" },
        new[] { "vercel", "netlify" },
        new[] { "communication", "presentation", "stakeholders" },
    };

    private static readonly (string Have, string Missing)[] RelatedDirected =
    {
        ("docker", "kubernetes"), ("kubernetes", "docker"),
        ("sql", "postgresql"), ("sql", "mysql"), ("sql", "sql server"),
        ("postgresql", "sql"), ("mysql", "sql"), ("sql server", "sql"),
        ("git", "github"), ("github", "git"), ("github actions", "github"),
        ("entity framework core", "sql"),
        ("pandas", "numpy"), ("numpy", "pandas"),
        ("css", "tailwind"), ("tailwind", "css"),
        ("react", "next.js"), ("next.js", "react"),
        ("react", "react native"),
        ("java", "kotlin"), ("xml views", "jetpack compose"), ("android", "jetpack compose"), ("android", "jetpack"),
        ("kotlin", "coroutines"), ("coroutines", "flow"), ("rxjava", "coroutines"), ("sqlite", "room"), ("mysql", "room"),
        ("rest api", "retrofit"), ("junit", "mockito"), ("jest", "mockito"), ("selenium", "espresso"), ("playwright", "espresso"),
        ("swift", "ios"), ("ios", "android"), ("kotlin", "kotlin multiplatform"), ("dart", "flutter"),
        ("kubernetes", "eks"), ("kubernetes", "aks"), ("kubernetes", "gke"), ("kubernetes", "istio"),
        ("prometheus", "pagerduty"), ("grafana", "pagerduty"), ("prometheus", "slos"), ("grafana", "slos"),
        ("nginx", "load balancing"), ("linux", "networking"), ("incident response", "chaos engineering"),
        ("c", "c++"), ("c++", "c"), ("c++", "rust"), ("c#", "golang"), ("java", "golang"), ("c", "rust"), ("golang", "rust"), ("jest", "react testing library"),
        ("sql", "snowflake"), ("sql", "bigquery"), ("sql", "redshift"), ("mysql", "snowflake"), ("postgresql", "snowflake"),
        ("airflow", "etl"), ("dbt", "etl"), ("python", "etl"), ("pandas", "etl"),
        ("tableau", "data visualization"), ("power bi", "data visualization"), ("looker", "data visualization"),
        ("statistics", "a/b testing"), ("statistics", "forecasting"), ("python", "pandas"),
        ("docker", "helm"), ("kubernetes", "helm"), ("terraform", "ansible"), ("bash", "linux"), ("linux", "bash"),
    };

    private static readonly Dictionary<string, Skill> ByKey =
        All.GroupBy(s => s.Key).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

    /// <summary>Every spelling (key, display, alias) -> canonical skill.</summary>
    private static readonly Dictionary<string, Skill> ByAnyName = BuildNameIndex();

    /// <summary>All spellings, longest first, for phrase matching.</summary>
    public static IReadOnlyList<(string Phrase, string Key)> Phrases { get; } =
        ByAnyName.Where(kv => !AmbiguousSkills.CaseSensitivePhrases.Contains(kv.Key))
            .Select(kv => (Phrase: kv.Key, Key: kv.Value.Key)).OrderByDescending(p => p.Phrase.Length).ThenBy(p => p.Phrase, StringComparer.Ordinal).ToList();

    private static readonly HashSet<(string, string)> RelatedPairs = BuildRelated();

    private static Dictionary<string, Skill> BuildNameIndex()
    {
        var map = new Dictionary<string, Skill>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in All)
        {
            map.TryAdd(s.Key, s);
            map.TryAdd(s.Display.ToLowerInvariant(), s);
        }
        foreach (var (key, names) in Aliases)
            foreach (var n in names)
                map.TryAdd(n, ByKey[key]);
        return map;
    }

    private static HashSet<(string, string)> BuildRelated()
    {
        var set = new HashSet<(string, string)>();
        foreach (var g in RelatedGroups)
            foreach (var a in g)
                foreach (var b in g)
                    if (a != b) set.Add((a, b));
        foreach (var p in RelatedDirected) set.Add(p);
        return set;
    }

    public static bool IsSkill(string keyword) => Find(keyword) is not null;

    /// <summary>Finds a skill by key, display name or alias (case-insensitive).</summary>
    public static Skill? Find(string keyword) =>
        string.IsNullOrWhiteSpace(keyword) ? null : ByAnyName.TryGetValue(keyword.Trim(), out var s) ? s : null;

    public static string Display(string keyword) => Find(keyword)?.Display ?? keyword;

    /// <summary>True when having <paramref name="have"/> is credible evidence toward <paramref name="missing"/>.</summary>
    public static bool AreRelated(string have, string missing)
    {
        var a = Find(have)?.Key;
        var b = Find(missing)?.Key;
        return a is not null && b is not null && a != b && RelatedPairs.Contains((a, b));
    }

    /// <summary>Known skills first (in input order), then other keywords (in input order).</summary>
    public static List<string> RankSkillsFirst(IEnumerable<string> keywords)
    {
        var list = keywords.ToList();
        return list.Where(IsSkill).Concat(list.Where(k => !IsSkill(k))).ToList();
    }

    /// <summary>
    /// Specific tools that are evidence for an umbrella skill: a resume listing Jenkins satisfies "CI/CD",
    /// EF Core satisfies ".NET", EKS satisfies "Kubernetes" and "AWS". One level, applied to the resume side only.
    /// (React Native deliberately does NOT imply React, Next.js does.)
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> Implied = new Dictionary<string, string[]>
    {
        ["jenkins"] = new[] { "ci/cd" },
        ["github actions"] = new[] { "ci/cd" },
        ["gitlab"] = new[] { "ci/cd" },
        ["circleci"] = new[] { "ci/cd" },
        ["microsoft azure devops"] = new[] { "ci/cd" },
        ["argo cd"] = new[] { "ci/cd" },
        ["asp.net"] = new[] { ".net" },
        ["asp.net core"] = new[] { ".net" },
        ["entity framework core"] = new[] { ".net" },
        ["eks"] = new[] { "kubernetes", "aws" },
        ["aks"] = new[] { "kubernetes", "azure" },
        ["gke"] = new[] { "kubernetes", "gcp" },
        ["spring boot"] = new[] { "spring" },
        ["rails"] = new[] { "ruby" },
        ["next.js"] = new[] { "react" },
        ["jetpack compose"] = new[] { "android", "jetpack" },
        ["kotlin multiplatform"] = new[] { "kotlin" },
        ["xunit"] = new[] { "testing" }, ["nunit"] = new[] { "testing" }, ["junit"] = new[] { "testing" },
        ["jest"] = new[] { "testing" }, ["pytest"] = new[] { "testing" }, ["espresso"] = new[] { "testing" },
        ["helm"] = new[] { "kubernetes" },
    };

    /// <summary>The keys plus every umbrella skill they imply.</summary>
    public static HashSet<string> WithImplied(IEnumerable<string> keys)
    {
        var set = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
        foreach (var k in set.ToList())
            if (Implied.TryGetValue(k, out var more))
                set.UnionWith(more);
        return set;
    }
}
