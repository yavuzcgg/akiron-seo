# Phase 4: GEO Engine (Generative Engine Optimization) & Prompt Intelligence

## 🎯 Phase Overview
Phase 4 focuses on Generative Engine Optimization (GEO), measuring and optimizing how your brand appears inside AI Search Engines (ChatGPT, Perplexity, Claude, Gemini, SearchGPT).

---

## 🛠️ Implemented Technical Features

### 1. GEO Engine & AI Share of Voice (`GeoEngineService.cs`)
* Simulates or queries LLM prompt templates (e.g. *"Türkiye'deki en iyi X kategorisindeki tedarikçiler nelerdir?"*).
* Checks if the target brand/domain is cited, mentioned, or recommended by AI models.
* Computes **AI Share of Voice (0 - 100%)** & sentiment classification (`Positive`, `Neutral`, `NotMentioned`).
* Persists analysis records to `GeoAnalysis` entity in database.

### 2. Live Prompt Intelligence Evaluator (`GeoEndpoints.cs`)
* `GET /api/v1/websites/{id}/geo-analysis`: Returns AI Share of Voice & citation status across ChatGPT, Perplexity, Claude, and Gemini.
* `POST /api/v1/websites/{id}/analyze-prompt`: Evaluates custom user prompts live against AI models.

### 3. Dashboard GEO Intelligence Component (`GeoIntelligenceCard.tsx`)
* AI Share of Voice score badge (e.g. 75%).
* AI Search Engine Citation Matrix (🟢 Cited & Recommended / 🔴 Not Cited).
* Sample AI Response Snippets & Citation URLs.
* **"⚡ Analyze Prompt"** custom prompt testing tool.

---

## 🧪 How to Test & Verify Phase 4 Features

1. Open **[http://localhost:3000/dashboard](http://localhost:3000/dashboard)**.
2. In the **Registered Websites** list, locate the **🤖 AI Share of Voice & Citation Intelligence** card under your site.
3. Observe the **AI Share of Voice** score badge (e.g. `75%`).
4. Review the **AI Search Engine Citation Status** matrix for ChatGPT, Perplexity, Claude, and Gemini.
5. In the **Prompt Intelligence Tester** input, type a custom query (e.g. *"Motosiklet yedek parça nereden alınır?"*) and click **`⚡ Analyze Prompt`**.
6. Observe live AI citation results and actionable GEO optimization steps!
