# Phase 2: SEO Audit Scoring, AI Bot Auditor & AEO Engine

## 🎯 Phase Overview
Phase 2 focuses on automated SEO issue scoring, `robots.txt` AI Bot Auditor (detecting GPTBot, ClaudeBot, PerplexityBot access), and AEO (Answer Engine Optimization) JSON-LD schema & `llms.txt` generators.

---

## 🛠️ Planned Technical Features

### 1. Detailed SEO Audit & Issue Scoring Engine (`SeoAuditEngine`)
* Analyzes crawled HTML for:
  * Missing or short `<title>` (< 20 chars).
  * Missing or short `<meta name="description">` (< 50 chars).
  * Missing or multiple `<h1>` headings.
  * Missing OpenGraph (`og:title`, `og:image`) tags.
  * Missing Canonical link (`<link rel="canonical">`).
* Computes weighted overall score (0 - 100).

### 2. AI Bot Auditor (`robots.txt` Checker)
* Tars `https://domain.com/robots.txt`.
* Checks Disallow rules for AI crawlers: `GPTBot`, `ChatGPT-User`, `ClaudeBot`, `PerplexityBot`, `Google-Extended`.

### 3. AEO (Answer Engine Optimization) Generator
* Generates valid JSON-LD schemas (`Organization`, `FAQPage`, `Article`).
* Generates `llms.txt` markdown specification file for LLM indexers.

---

## 🧪 How to Test & Verify Phase 2 (To Be Completed)
*(Detailed test steps will be added upon completion of Phase 2 features)*
