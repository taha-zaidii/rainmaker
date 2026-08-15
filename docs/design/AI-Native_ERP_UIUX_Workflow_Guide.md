# From Inspiration to Enterprise ERP UI
### A senior UI/UX engineer's workflow — using Figma + Claude MCP

You've done the right first step: gathered references, studied ERP competitors, and looked closely at Apple's design system. Most people jump straight from "I saw something cool on Pinterest" to Figma and start dragging rectangles. That's how you get a pretty screen with no system behind it. Here's the actual path.

---

## Phase 1 — Turn inspiration into a design *direction* (not a mood board)

A folder of screenshots isn't a direction — it's raw material. Before opening Figma, do this synthesis work:

**1. Sort your references into categories, not "cool stuff."**
Go through your Pinterest board and ERP screenshots and bucket them:
- Navigation patterns (sidebar vs. command palette vs. top nav)
- Data table treatments (row density, sorting, inline actions)
- Color & typography systems
- Micro-interactions (hover states, transitions, loading states)
- Empty/error state handling

You'll notice most of your "inspiration" clusters around 2-3 real patterns. That's your direction emerging.

**2. Write a one-page Design Principles doc.**
This is the single highest-leverage thing a senior designer does that juniors skip. 3-5 principles, specific to *this* product. For an AI-native ERP, something like:
- **Clarity over decoration** — every AI suggestion, every data point must be scannable in under 2 seconds
- **Density with breathing room** — enterprise users need lots of data on screen, but never cramped
- **Trust through predictability** — AI actions are always visible, explainable, and reversible
- **Consistency is the feature** — 40 modules should feel like one product, not 40

Every design decision later gets checked against this doc. It's your tie-breaker.

**3. Be honest about what Apple's system actually transfers.**
Apple's redesign (the "Liquid Glass" language from WWDC 2025) is genuinely excellent — but it's a *consumer hardware* system, optimized for marketing moments and large touch targets on phones/watches. Before you borrow it wholesale for an ERP, separate what transfers from what doesn't:

- **Transfers well:** restraint, systemic rigor (every component obeys the same rules), depth through subtle elevation/shadow rather than heavy borders, motion that clarifies state changes rather than decorates
- **Doesn't transfer well:** heavy glass/blur/translucency effects — these actively hurt legibility on dense data tables and long forms, which is 90% of an ERP's surface area. Glassmorphism looks stunning on a hero screen and becomes a readability problem on a 40-row table with 12 columns.

The far more relevant references for an *enterprise, data-dense* product are design systems built for exactly this problem: **IBM Carbon**, **Salesforce Lightning Design System**, and **Atlassian Design System**. Add these to your research pass — they've already solved density, permissions UI, bulk actions, and complex forms at scale. Apple teaches you restraint and polish; Carbon/Lightning teach you how to keep that polish when you have 200 rows and a sidebar of filters.

---

## Phase 2 — Build the foundation before you design a single screen

This is the step most people skip, and it's why AI-generated code from Figma often comes out messy. Define **design tokens** first:

| Token type | What to define |
|---|---|
| Color | Semantic names (`color-bg-surface`, `color-text-critical`), not raw hex. Full light + dark mode pairs. |
| Typography | A scale (e.g. 12/14/16/20/24/32px), one or two font families max |
| Spacing | 8pt grid (4, 8, 12, 16, 24, 32, 48...) — non-negotiable for ERP density consistency |
| Radius | 2-3 values max (e.g. 4px inputs, 8px cards) |
| Elevation | 3-4 shadow levels for layering (dropdown, modal, popover) |
| Motion | Standard easing curve + 2-3 duration tokens (150ms micro, 250ms standard, 400ms page-level) |

Set these up as **Figma Variables/Styles**, not just visual choices. This matters a lot more than it sounds — when you get to the Claude MCP workflow in Phase 5, Claude reads your Figma variables directly. Named tokens (`color-bg-surface`) generate clean, semantic code. Unnamed hex values generate code Claude has to guess names for, which is where AI-generated UI starts looking generic.

---

## Phase 3 — Build the component library (atomic → composite)

Work bottom-up:

1. **Atoms:** buttons, inputs, checkboxes, badges, icons, avatars
2. **Molecules:** form fields (label + input + error text), table cells, filter chips, search bars
3. **Organisms:** data table, side nav, command palette, record detail panel, bulk-action bar
4. **ERP-specific components you'll need that generic UI kits don't have:**
   - Status/permission pills (role-based visual language)
   - Audit trail / activity log component
   - Empty, loading, and error states for *every* data view (not an afterthought)
   - Bulk selection + action bar
   - AI-specific: a copilot/chat panel, inline AI suggestion chips, and an "agent action" confirmation pattern (since your AI can take actions, not just answer questions — users need to see what it did and undo it)

Use Auto Layout on everything and **name your layers and components semantically** (`ProductTable/Row/StatusCell`, not `Frame 47 / Rectangle 12`). This isn't just tidiness — when you connect Claude to Figma via MCP, layer names *are* the context the model reads. A well-named component tree is the difference between Claude generating clean, reusable code and Claude guessing at your intent from geometry alone.

---

## Phase 4 — Design the core screens

Pick 4-5 anchor screens and design those fully before touching anything else:

1. **Dashboard** — the AI-native differentiator lives here (proactive insights, not just static widgets)
2. **List/table view** — this is where 80% of enterprise usage happens; nail density + filtering + bulk actions first
3. **Record detail view** — single-entity deep dive
4. **Create/edit form flow** — long forms are where enterprise UX usually breaks; use progressive disclosure
5. **AI interaction surface** — chat/copilot panel, suggestion cards, and the confirmation pattern for agent actions

For each: wireframe the information hierarchy in low-fidelity first (boxes and labels only), *then* apply your design system. Skipping straight to high-fidelity is how people end up polishing the wrong layout.

---

## Phase 5 — Set up the Figma MCP + Claude workflow (the "crazy good flow")

This is real and it works well when your Figma file is clean (which is exactly what Phases 2-4 set you up for). Here's the actual setup:

**Install & connect (Claude Code):**
```
claude plugin install figma@claude-plugins-official
```
Restart Claude Code, then run `/plugin`, go to the Installed tab, select the Figma server, and authenticate — it opens a browser window to grant access. <cite index="15-1">This installs the Figma plugin along with MCP server settings and Agent Skills for common workflows</cite>. Alternatively you can add the remote server directly:
```
claude mcp add --transport http figma https://mcp.figma.com/mcp
```

**The actual loop once connected:**
1. In Figma, select the frame/screen you want built, copy its link
2. In Claude Code, paste the link with a specific instruction — e.g. *"build this screen in React and Tailwind, reuse our existing components"* rather than a vague "build this page"
3. <cite index="10-1">Claude reads the frame's layer structure, colors, fonts, spacing, and auto-layout settings through the MCP server, and generates code that matches it</cite>
4. If you've set up Code Connect (mapping Figma components to your actual codebase components), <cite index="10-1">Claude reuses your real components instead of rebuilding them from scratch</cite>
5. Review, iterate, refine the prompt — don't accept the first pass blindly

**It's bidirectional.** Once you've built something in Claude Code, <cite index="14-1">you can send it back into Figma as editable layers using a prompt and a link to the frame</cite> — useful when you prototype fast in code and want to bring it back to the canvas for a designer (or yourself, wearing your design hat) to refine visually.

**One honest warning from people who've done this at scale:** <cite index="9-1">Claude Code is not a magical handoff replacement — it's an execution agent working with whatever structure you give it. If your Figma file is chaotic and your repo has no clear conventions, MCP just gives Claude better access to the chaos.</cite> This is exactly why Phases 2-4 aren't optional — they're what makes this workflow actually fast instead of a mess of AI-guessed styling.

---

## Phase 6 — Critique like a senior, not like the creator

You designed it, so you're the worst-positioned person to spot its flaws. Build in a real critique step:

- **Run Nielsen's 10 usability heuristics** against your core screens (visibility of system status, error prevention, recognition over recall, etc.) — especially relevant for AI actions: is it always visible what the AI just did?
- **Accessibility pass:** contrast ratios (4.5:1 minimum for text), full keyboard navigation, screen-reader labels on icon-only buttons. This is not optional for enterprise software — it's often a procurement requirement.
- **Use Claude itself as a critique partner.** Screenshot a screen and ask for a heuristic review, or ask it to play a specific persona ("a finance manager who's never used AI tools before — where would they get stuck?"). It's a fast, cheap first pass before real user feedback.
- **Get it in front of an actual user** — even one person from Multinet's HR team clicking through Rainmaker screens will surface things no amount of self-review will.

---

## Phase 7 — How to actually become expert-level by the end of this

Doing the above once makes you competent at this project. Becoming genuinely expert takes deliberate practice alongside it:

- **Study systems built for exactly your problem**, not just Apple: IBM Carbon, Salesforce Lightning, and Atlassian Design System are all open, documented, and built specifically for dense enterprise data — read their design principles pages, not just the component screenshots
- **Read:** *Refactoring UI* (Adam Wathan/Steve Schoger — fast, practical, visual), *About Face* (Alan Cooper — the closest thing to a bible for complex enterprise software UX)
- **Rebuild one existing enterprise screen per week** from scratch in your own system, purely as practice — Salesforce's object list view, Linear's issue table, Notion's database view are all excellent density/hierarchy studies
- **Keep a running "why" log** — every time you make a design decision, write one sentence on why. This is what turns "I picked this because it looked nice" into actual design reasoning, which is the real skill gap between junior and senior.

---

**Where you are right now:** research done, direction forming. Next concrete action is Phase 1, step 2 — write that one-page principles doc. Everything downstream moves faster once that exists.
