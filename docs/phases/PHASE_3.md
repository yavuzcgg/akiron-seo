# Phase 3: Rank Tracking Engine, Keyword Performance Tracking & Competitor Intelligence

## 🎯 Phase Overview
Phase 3 focuses on automated keyword rank position tracking, historical rank delta calculations (e.g. 🟢 #3 ↑2 / 🔴 #12 ↓4), periodic background execution scheduling via `Cronos`, and interactive UI position cards on the dashboard.

---

## 🛠️ Implemented Technical Features

### 1. Keyword Rank Tracking Engine (`KeywordRankTrackerService.cs`)
* Tracks search positioning (1–100) for target keywords.
* Computes position change deltas (`PositionChange`) between previous and current runs.
* Calculates `NextScheduledRun` using `Cronos` cron expressions (e.g. `0 0 * * *` daily at midnight).

### 2. CQRS Rank Queries & Modular Endpoints (`KeywordEndpoints.cs`)
* `GET /api/v1/websites/{id}/keywords`: Returns list of tracked keywords with position change indicators.
* `POST /api/v1/keywords`: Adds a new keyword to track.
* `POST /api/v1/keywords/{id}/check-rank`: Triggers immediate position check.

### 3. Dashboard Keyword Rank Tracker UI (`KeywordTrackerCard.tsx`)
* Renders tracked keywords table directly under registered websites.
* Interactive position change badges (🟢 **#3 ↑2** / 🔴 **#12 ↓4** / ⚪ **#5 -**).
* **"+ Track"** input & **"⚡ Check Rank"** instant rank trigger buttons.

---

## 🧪 How to Test & Verify Phase 3 Features

1. Open **[http://localhost:3000/dashboard](http://localhost:3000/dashboard)**.
2. In the **Registered Websites** list, locate the **Keyword Rank Tracker** card under your site.
3. Type a keyword (e.g. `"motosiklet kaskı"` or `"motosiklet yedek parça"`) and click **`+ Track`**.
4. The keyword will appear in the table. Click **`⚡ Check Rank`** to trigger a position check.
5. Observe the live rank position badge (e.g. **#3 ↑2**) and updated URL details!
