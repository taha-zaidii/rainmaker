# Google Stitch prompts — Rainmaker HRMS recruitment portal

Prepared by Syed Taha, Multinet.

How to use: Stitch works best **one screen at a time**. For every screen, paste
**Block A** (the design system) followed by **one** numbered screen prompt.
Block A is the same every time — that is what keeps the screens consistent.

> First time with Stitch: it generates a UI design from a text prompt, and you
> can then export it (to Figma, or as HTML/CSS). You do **not** need it to
> produce working code — I only need the visual and the markup as a reference,
> and I convert it to Angular myself. If the export options differ from what is
> described in §3, just send screenshots; that is enough to work from.

---

## 1. Block A — paste this before EVERY screen prompt

```
Design a screen for "Rainmaker" — an enterprise HRMS / ERP web application used
by HR teams. Desktop-first, 1440px wide, dense but breathable enterprise layout.

DESIGN SYSTEM (follow exactly, every screen):

Colors
- Primary blue #1A56DB. Hover #1E429F. Tint background #EBF2FF.
- Accent orange #F97316. Tint background #FFF3E8.
- Page background #F7F9FC. Cards/surfaces pure white #FFFFFF.
- Borders and dividers #E5E9F0.
- Text primary #1F2A37, secondary #6B7280, disabled #9CA3AF.
- Success #16A34A, Warning #D97706, Danger #DC2626, Info #0284C7.

CRITICAL COLOR RULE — orange means "AI".
Every element that is AI-generated, AI-suggested or AI-related uses the orange
accent: sparkle icons, "Generate with AI" buttons, AI badges, review banners,
AI score chips. Nothing else on the screen uses orange. Blue is for normal
interactive elements. A user must be able to tell at a glance what came from a
human and what came from the AI.

Typography
- Inter. Page title 24px semibold. Section heading 16px semibold.
- Body 14px regular. Labels 13px medium. Helper/caption 12px regular.

Shape and spacing
- Cards: white, 8px radius, 1px #E5E9F0 border, very soft shadow, 24px padding.
- Inputs and buttons: 6px radius, 40px tall. Inputs have a 1px #E5E9F0 border,
  white fill, and a blue focus ring.
- 4px spacing scale. 24px gap between cards, 16px between form fields.

Layout chrome (on every screen except where stated)
- Left icon sidebar, 88px wide, primary blue #1A56DB background, white icons
  with 11px labels beneath. Items: Dashboard, System Administration, Human
  Resource, Inventory, Procurement, Reports, Foundation Data, Learning Center,
  Visitor Management, Performance Management, Event Management, Recruitment
  Management (this one active/highlighted).
- Top bar, 64px, white: "rainmaker" wordmark left; right side has home,
  tasks, a notification bell with an orange count badge, a dark-mode toggle,
  and a user avatar with the name "Sumaira Butt".
- Page header block: title, one-line description, and a primary action button
  on the right.

Tone: clean, professional, trustworthy, modern SaaS. Not playful, not flat-grey
corporate. Think Linear or Vercel dashboard discipline applied to an HR product.
```

---

## 2. Screen prompts

### Priority A — these test the AI features that are already built

**A1. Recruitment module dashboard**

```
Screen: "Recruitment Dashboard".
A secondary navigation column (240px, white) sits between the icon sidebar and
the content, listing: Create Job Requisition, Job Requisitions, Applications
Management, Interview Scheduling, Candidate Evaluation, Recruitment Settings.

Content:
- Four KPI stat cards in a row: Open Requisitions 12, Applications 26,
  Interviews Scheduled 13, Hired 4. Each has an icon in a soft tinted square,
  a large number, and a small "+3 this week" delta in green.
- A wide card "AI Activity" with an orange sparkle icon in the heading. Inside,
  a timeline list: "Job description generated — Software Developer · 2 min ago",
  "12 resumes parsed · 1 hour ago", "Candidate screening completed — 8
  shortlisted · 3 hours ago". Each row has a small orange AI badge.
- A card "AI Service Status": a green dot, "hrms-ai-service 1.1.0 connected",
  and six small capability pills (Resume Parsing, JD Generation, Screening,
  Ranking, Interview Questions, Scoring).
- A table card "Recent Job Requisitions" with columns: Job Title, Department,
  Vacancies, Applications, Status (pill: Draft grey / Pending Approval amber /
  Published green), Created, Actions (view, edit, delete icon buttons).
```

**A2. AI Recruitment Settings — the most important settings screen**

```
Screen: "AI Recruitment Settings", subtitle "Configure your AI provider and
feature preferences".

Card 1 — "API Key Configuration":
- A green success banner at the top: "API Key Configured — your key is active
  and ready to use", with a checkmark icon.
- Two-column form:
  - "AI Provider" dropdown, currently showing "MultinetAI" with a small orange
    "In-house" pill next to it. Show the dropdown OPEN, listing: OpenAI,
    Anthropic, Google AI, Custom API, MultinetAI (selected, with the orange
    In-house pill).
  - "Model" text input showing "qwen3.5:27b", greyed/read-only, with helper
    text "Set by the service — not editable".
  - "API Key" password input with an eye toggle, placeholder "Enter new API key
    or leave blank to keep current". Below it: "Current key: UALW••••••••0xIL"
    and a lock icon with "Encrypted at rest. Never shared."
  - "API Endpoint" input showing "https://ai.rainmaker.pk/hrms/api/v1", helper
    text "Base URL only — the backend appends the feature path."
  - "Max Tokens" and "Temperature" inputs, both visibly DISABLED and greyed,
    with helper text "Managed by the in-house service".
  - "Auto Shortlist Threshold" input showing 80, ENABLED, helper text
    "Candidates scoring at or above this are suggested for shortlisting".
- Button row: "Test Connection" (secondary, with a plug icon), "Save Settings"
  (primary blue), "Delete Key" (danger text button, far right).

Card 2 — "Connection Test Result". Show THREE stacked variants of this card so
all states are visible in one design:
  1. SUCCESS — green left border. "Connection verified". Body: "hrms-ai-service
     1.1.0 · schema 1.2.0". Six green capability pills. Timestamp.
  2. INVALID KEY — red left border. "API key rejected". Body: "The AI service
     did not accept this key. Check the key and save again."
  3. SERVICE UNREACHABLE — amber left border. "Could not reach the AI service".
     Body: "The key was not tested. The service may be down or the endpoint may
     be wrong." A "Retry" text button.

Card 3 — "AI Feature Settings": five rows, each with a title, one-line
description, and a toggle switch on the right (all ON, toggles in primary blue):
Auto Resume Screening, Auto Candidate Matching, Generate Interview Questions,
Email Notifications, Auto Resume Parse. A "Save Settings" primary button below.
```

**A3. Create Job Requisition — mode selection**

```
Screen: "Create Job Requisition", subtitle "Create a new job posting with AI
assistance or manually". Secondary nav column as in A1.

- A small grey pill at the top left reading "New Joining", with a "Change" text
  button on the right of that row.
- Section label "Creation Mode", then TWO large selectable cards side by side,
  each about 400px wide and 180px tall:
  - "Manual Creation" — pencil icon, blue tinted icon square, description
    "Fill in every field yourself", a "Start" outline button.
  - "AI-Assisted Creation" — orange sparkle icon, orange tinted icon square, a
    small orange "Recommended" pill in the top-right corner, description
    "Answer 5 questions and the AI drafts the full requisition for you", a
    "Start with AI" button in ORANGE. This card has a subtle orange border and
    looks like the featured option.
```

**A4. AI Job Description generator — input form**

```
Screen: "Create Job Requisition" with AI-Assisted mode active.

A card with a 3px orange left border, heading "AI Job Description Generator"
with an orange sparkle icon, and the line "Fill in the fields below and the AI
will draft a complete job requisition for you to review and edit."

Two-column form:
- Job Title (required) — "Senior Software Engineer"
- Department (required) — dropdown, "Information Technology"
- Designation (required) — dropdown, "System Administrator"
- Experience Required (required) — "3 - 6 years", helper "Free text — '3-5
  years', '5+', 'Fresh' all work"
- Job Category — dropdown, "Dot Net Developer", helper "The AI picks from your
  configured categories so the result always fits your dropdown"
- Key Skills (required) — a full-width textarea, two rows tall, containing
  "JavaScript, Python, .NET, Angular, C#", helper "Comma separated or one per
  line — the AI cleans this up"

Below the form: an amber info strip with an info icon reading "Generation takes
about 15–30 seconds. You can edit everything afterwards."

Footer: a large ORANGE button "Generate Job Description with AI" with a sparkle
icon, and a "Switch to manual" text link beside it.
```

**A5. AI generating — loading state (important, the wait is ~30s)**

```
Screen: the same AI generator card, but in its GENERATING state.

The form fields are dimmed and disabled. Centred in the card:
- An animated orange sparkle / pulsing ring illustration.
- Heading "Drafting your job requisition…"
- Sub-line "This usually takes 15–30 seconds."
- A horizontal 3-step progress indicator with the middle step active:
  "Understanding the role" (done, check) → "Writing the description" (active,
  orange spinner) → "Preparing your draft" (pending, grey).
- An elapsed timer reading "0:12".
- A "Cancel" text button underneath.

Make it feel calm and deliberate, not like a stuck page.
```

**A6. THE KEY SCREEN — AI draft loaded into the 4-step wizard**

```
Screen: "Create Job Requisition" — Step 1 of 4, populated with an AI draft.

At the top of the content area, a full-width ORANGE-tinted banner (#FFF3E8,
orange left border) with a sparkle icon:
"AI-generated draft — please review before saving. Every field is editable."
On the right of the banner: a "Regenerate" outline button and a dismiss X.

Below it, a 4-step horizontal stepper: 1 Basic Info (active, blue filled),
2 Requirements, 3 Compensation, 4 Publishing.

Then the "Basic Information" card. Fields filled with AI values, and EVERY
AI-filled field has a tiny orange sparkle icon inside the right edge of the
input to mark it as AI-suggested:
- Job Title: "Software Developer" (orange sparkle)
- Job Summary: a 3-line textarea with generated prose (orange sparkle)
- Department: "Information Technology" (orange sparkle)
- Designation: "System Administrator" (orange sparkle)
- Vacancies: "1" (orange sparkle)
- Job Category: "Dot Net Developer" (orange sparkle)

CRITICAL — two fields are deliberately EMPTY and styled differently:
- Employment Type — empty dropdown, a dashed grey border instead of solid, and
  helper text in grey: "For you to complete — the AI does not decide this"
- Grade — same empty dashed treatment, same helper text

Add a small collapsed summary strip below the card: "3 fields left for you to
complete" with a chevron, in grey — NOT an error, just information.

Footer: "Back" outline button left, "Continue to Requirements" primary blue
button right.

Note for the designer: the empty dashed fields are intentional and must NOT look
like validation errors. They are fields the AI is not permitted to fill.
```

**A7. Wizard step 2 — Requirements (AI-filled lists)**

```
Screen: same wizard, Step 2 of 4 "Requirements" active in the stepper. The
orange "AI-generated draft" banner is still at the top.

Card "Requirements":
- "Experience Required": two small number inputs, Minimum "3" and Maximum "6",
  each with an orange sparkle. Below them a tiny orange caption: "Taken from
  what you entered".
- "Key Responsibilities": an editable list of 5 bullet rows. Each row is a text
  input with a drag handle on the left and a delete X on the right, and an
  orange sparkle. An "+ Add responsibility" text button below.
- "Requirements": the same editable-list pattern, 4 rows.
- "Qualifications": the same pattern, 2 rows.
- "Skills": a chip/tag input showing removable chips — JavaScript, Python,
  .NET, Angular, C# — each chip with a small x, plus an "+ Add skill" chip. The
  chips are in blue tint, and the group has one orange sparkle in the corner.

There is deliberately NO age field anywhere on this screen.

Footer: "Back" and "Continue to Compensation".
```

### Priority B — next features (design now, wire later)

**B1. Resume upload and AI parse review**

```
Screen: "Upload Resume", subtitle "Upload a CV and the AI will extract the
candidate profile for your review".

Left half: a large dashed-border drop zone, blue tint on hover, with an upload
icon, "Drag a CV here or click to browse", and "PDF, DOCX, PNG or JPG · up to
5 MB". Below it, a compact uploaded-file row with a file icon, name
"ayesha-khan-cv.pdf", size, and a progress bar at 100%.

Right half: a card "Extracted Profile" with an orange sparkle and an orange
"AI-extracted — please verify" pill.
Fields: Name, Email, Phone, Location, Headline, Summary, Skills (chips),
Experience (two entries with company/role/duration), Education.

CRITICAL — some fields are flagged for review. Those fields have an AMBER left
edge and a small amber warning icon with a tooltip "Needs review — extracted by
pattern matching, not verified". Flag exactly: Phone, Location, and Skills.
The rest look normal.

At the bottom: "3 of 11 fields need review" in amber, then "Discard" and
"Accept and Save Candidate" (primary) buttons.
```

**B2. Applications Management with AI ranking and screening**

```
Screen: "Applications Management".
Four KPI cards: Total Applications 26, Scheduled 13, Evaluations 13, Hired 0.
Tab row: All Applications (active), Scheduled Interviews, Evaluations, Hired.
Filter row: search box, All Statuses dropdown, All Jobs dropdown, Clear button.

Main table, columns: Rank, Candidate (name + code + phone stacked), Job Title,
Department (blue tint pill), AI Match (see below), Status pill, Applied date,
Actions.

The "Rank" column shows a circular blue badge with the number.
The "AI Match" column shows a score chip: 92 in green, 78 in amber, 45 in red,
each with a tiny orange sparkle to mark it as AI-derived.
The Actions column has four icon buttons; the first is an ORANGE sparkle button
with a tooltip "AI Screen".

Include a right-hand slide-over drawer, open, titled "AI Screening Result" with
an orange sparkle:
- A large circular score gauge reading 92 / 100.
- A green "Recommended for shortlist" pill, and beside it a grey caption
  "Threshold 80 · advisory only".
- "Matched Skills": green chips — C#, .NET, Angular, SQL Server.
- "Missing Skills": grey outline chips — Azure DevOps, Kubernetes.
- "Why this score": three rows, each with a green check or amber gap icon, a
  one-line reason, and an indented italic quote of the evidence from the CV.
- Footer buttons: "Override" (outline) and "Accept Suggestion" (primary).
```

**B3. Interview question generator**

```
Screen: "Generate Interview Questions", with an orange sparkle in the title.
Top card: read-only context — Job Title "Senior .NET Developer", Candidate
"Ayesha Khan", plus a "Questions per category" number input showing 5 and three
category checkboxes (Technical, Behavioural, Role-specific — all checked), and
an orange "Generate Questions" button.

Below: three columns, one per category, each a card with a coloured heading.
Each question is a row containing: the question text, a smaller grey line
beneath labelled "What to listen for:", a tiny pill showing either "from JD"
(blue) or "from CV" (orange), and on hover a copy icon and an edit icon.
A "Add to interview pack" button in each column footer.
```

**B4. Candidate evaluation — rubric scoring**

```
Screen: "Candidate Evaluation" for one candidate.
Header card: candidate name, role applied for, interview round, panel members
as small avatar chips.

Card "AI Scoring Assist" with an orange sparkle AND a prominent amber
"PROVISIONAL" badge in the heading. Directly under the heading, an amber info
strip: "The scoring rubric is awaiting HR sign-off. These scores are advisory
and cannot be used as the evaluation of record."

Inside: four criteria rows. Each row has the criterion name, a 0–10 segmented
score bar with the AI score marked in orange, the numeric score, and a
collapsible "Why" line with the AI's justification.
Criteria: Technical Skill 8/10, Relevant Experience 7/10, Communication 6/10,
Domain Depth 6/10. An overall "72 / 100" circle on the right.

Below it, a clearly separate white card "Panel Evaluation (final)" in BLUE with
empty scoring inputs for the human panel, a comments textarea, and a
"Submit Evaluation" primary button — visually distinct from the AI card so it
is obvious which one is the decision of record.
```

---

## 3. What to send me, and how

For each screen, put it in its own folder under `Frontend/design/stitch/`:

```
Frontend/design/stitch/
  A1-recruitment-dashboard/
    screenshot.png          ← required
    index.html              ← if Stitch lets you export HTML/CSS
  A2-ai-settings/
    screenshot.png
    index.html
  ...
```

**The screenshot is the required part.** The HTML export is a bonus — it gives
me exact spacing and colour values so the Angular build matches your design
instead of approximating it. If Stitch offers "Copy to Figma" instead, a
screenshot is still fine.

Do **not** worry about: making it interactive, real data, responsiveness, or
matching Angular in any way. It is a reference, not code I will run.

Naming matters only so I can map each design to the right route — keep the
`A1-`, `A2-` prefixes.

---

## 4. What happens after you paste them in

1. I install and configure **Tailwind CSS v4** in the Angular app (it is not
   set up yet) and encode the design system above as theme tokens, so the
   colours and spacing are named rather than hard-coded everywhere.
2. I build each screen as an **Angular 19 standalone component** with Tailwind
   utility classes — no UI library needed for these layouts, and skipping one
   keeps the bundle small and the styling entirely under our control.
3. I wire the real API: `SaveApiKeySettings`, `TestApiKey`,
   `GenerateJobDescription` are live on `http://localhost:5019` and already
   verified end to end.
4. The JD wizard binds directly to the response shape the backend already
   returns: `draft.basicInfo` / `.requirements` / `.compensation` /
   `.publishing` map 1:1 onto the four wizard steps, `reviewRequired` drives
   the orange banner, and `fieldsForHumanToComplete` drives the dashed empty
   fields in A6.

Start with **A2** and **A4/A5/A6** — those are the screens that exercise the
features that already work end to end, so they can be wired up the same day.
